"""Local-only-by-default multilingual WAV transcription. No chat/voice generation."""
import argparse
import io
import json
import secrets
import threading
import wave
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from urllib.parse import parse_qs, urlsplit

LANGUAGES = {"mt": "Maltese", "it": "Italian", "en": "English"}
MAX_BYTES = 12_000_044  # Up to 120 seconds of the app's mono 48 kHz PCM.
WARNING = "Machine transcription can invent words from noise. Verify against the original recording. No response is generated."


def validate_wave(data):
    try:
        with wave.open(io.BytesIO(data), "rb") as audio:
            if audio.getnchannels() != 1 or audio.getsampwidth() != 2 or audio.getframerate() != 48000 or audio.getcomptype() != "NONE":
                raise ValueError("Expected mono 48 kHz signed 16-bit PCM WAV from Ghostbox.")
            if not 0 < audio.getnframes() <= 48000 * 120:
                raise ValueError("Recording must contain audio and be at most 120 seconds.")
            if len(audio.readframes(audio.getnframes())) != audio.getnframes() * 2:
                raise ValueError("WAV data is incomplete.")
    except (wave.Error, EOFError) as error:
        raise ValueError("Invalid WAV recording.") from error


def transcribe_audio(model, data, language):
    validate_wave(data)
    if language not in LANGUAGES:
        raise ValueError("Choose mt (Maltese), it (Italian), or en (English).")
    segments, _ = model.transcribe(
        io.BytesIO(data), language=language, task="transcribe", beam_size=5,
        vad_filter=True, vad_parameters={"min_silence_duration_ms": 500},
        condition_on_previous_text=False, temperature=0.0,
    )
    # These heuristics reduce, but cannot eliminate, recognizer hallucinations.
    accepted = [s for s in segments if s.no_speech_prob < 0.6 and s.avg_logprob >= -1.0 and s.compression_ratio <= 2.4]
    text = "\n".join(f"[{s.start:05.1f}–{s.end:05.1f}s] {s.text.strip()}" for s in accepted)
    warning = WARNING + (" Maltese recognition is experimental and has not been validated on your radio recordings." if language == "mt" else "")
    return {"text": text, "language": language, "warning": warning}


def detect_speech(data):
    validate_wave(data)
    from faster_whisper.audio import decode_audio
    from faster_whisper.vad import get_speech_timestamps, VadOptions
    audio = decode_audio(io.BytesIO(data), sampling_rate=16000)
    regions = get_speech_timestamps(audio, VadOptions(threshold=0.6, min_speech_duration_ms=250, min_silence_duration_ms=350, speech_pad_ms=100))
    segments = [{"start": round(r["start"] / 16000, 3), "end": round(r["end"] / 16000, 3)} for r in regions]
    return {"duration": len(audio) / 16000, "speech_seconds": sum(s["end"] - s["start"] for s in segments),
            "segments": segments, "warning": "Possible speech activity only. Noise, music and radio interference can cause false detections; faint or chopped speech can be missed. No speaker or source identity is determined. Verify against the raw audio."}


def handler_factory(model, token=""):
    model_lock = threading.Lock()

    class Handler(BaseHTTPRequestHandler):
        def setup(self):
            super().setup()
            self.connection.settimeout(30)

        def reply(self, status, payload):
            body = json.dumps(payload, ensure_ascii=False).encode("utf-8")
            self.send_response(status)
            self.send_header("Content-Type", "application/json; charset=utf-8")
            self.send_header("Content-Length", str(len(body)))
            self.end_headers()
            self.wfile.write(body)

        def authorized(self):
            return not token or secrets.compare_digest(self.headers.get("Authorization", ""), "Bearer " + token)

        def do_GET(self):
            if not self.authorized():
                self.reply(401, {"error": "A matching bearer token is required."})
            elif self.path == "/health":
                self.reply(200, {"status": "ready", "languages": LANGUAGES, "cloud": False})
            else:
                self.reply(404, {"error": "Not found."})

        def do_POST(self):
            if not self.authorized():
                self.reply(401, {"error": "A matching bearer token is required."})
                return
            route = urlsplit(self.path)
            if route.path not in ("/transcribe", "/detect-speech"):
                self.reply(404, {"error": "Not found."})
                return
            try:
                length = int(self.headers.get("Content-Length", "0"))
                if not 44 < length <= MAX_BYTES:
                    raise ValueError("Send a WAV recording of 1–120 seconds, at most 12 MB.")
                language = parse_qs(route.query).get("language", ["mt"])[0]
                if language not in LANGUAGES:
                    raise ValueError("Language must be mt, it, or en.")
                data = self.rfile.read(length)
                validate_wave(data)
                if not model_lock.acquire(blocking=False):
                    self.reply(503, {"error": "Another recording is being transcribed. Try again shortly."})
                    return
                try:
                    result = detect_speech(data) if route.path == "/detect-speech" else transcribe_audio(model, data, language)
                finally:
                    model_lock.release()
                self.reply(200, result)
            except ValueError as error:
                self.reply(400, {"error": str(error)})
            except (BrokenPipeError, ConnectionResetError):
                pass
            except Exception:
                self.reply(500, {"error": "Transcription failed. Check the speech-server console and model installation."})

        def log_message(self, format, *args):
            # Do not log query text, bearer tokens, recordings, or transcripts.
            pass

    return Handler


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("--host", default="127.0.0.1", help="Use 0.0.0.0 to serve your Android phone on a trusted LAN")
    p.add_argument("--port", type=int, default=18765)
    p.add_argument("--model", default="small", help="Multilingual faster-whisper model or local model directory; never use .en models")
    p.add_argument("--token", default="", help="Required (at least 16 characters) when listening beyond localhost")
    p.add_argument("--download-root", help="Optional model cache directory")
    args = p.parse_args()
    if args.host not in ("127.0.0.1", "localhost") and len(args.token) < 16:
        p.error("LAN access requires --token with at least 16 characters; enter the same token in the app.")
    if args.model.endswith(".en"):
        p.error("Choose a multilingual model for Maltese and Italian.")
    from faster_whisper import WhisperModel
    print(f"Loading multilingual model {args.model!r} on CPU; first use may download model files.", flush=True)
    model = WhisperModel(args.model, device="cpu", compute_type="int8", download_root=args.download_root)
    server = ThreadingHTTPServer((args.host, args.port), handler_factory(model, args.token))
    server.daemon_threads = True
    print(f"Ready: http://{args.host}:{args.port} — Maltese / Italian / English. Audio stays on this computer.", flush=True)
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass
    finally:
        server.server_close()


if __name__ == "__main__":
    main()
