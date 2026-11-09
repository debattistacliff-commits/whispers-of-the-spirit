# Local Maltese / Italian / English transcription

The apps record audio themselves. This optional companion turns a selected raw
WAV recording into text on your computer, using a multilingual Whisper model.
It does not generate spoken replies or translate radio stations.

## Windows app on this computer

With Python 3.10+ installed, open a terminal in this folder:

```powershell
python -m pip install -r requirements.txt
python speech_server.py --model small
```

Or double-click `StartSpeechServer.cmd` after installing the dependencies.
Keep the server running. The first start may download model weights; later runs
use the cache. In Ghostbox's Record tab, use `http://127.0.0.1:18765`.
For a slower, larger model, try `--model medium`. Do not use English-only `.en`
models for Maltese or Italian. Maltese recognition remains experimental.

## Android on your trusted LAN

Run the service on your computer:

```powershell
python speech_server.py --host 0.0.0.0 --port 18765 --token YOUR_LONG_RANDOM_TOKEN
```

Use a token of at least 16 characters. In the Android app, enter the computer's
LAN IP, for example `http://192.168.1.20:18765`, and the same token. The phone and
computer must be reachable on the same network, including through the computer's
firewall. A localhost address on Android points to the phone, not the computer.
LAN HTTP traffic is unencrypted; use this only on your trusted network. The
server requires a token for non-localhost binding and does not enable browser CORS.

## What is sent and saved

Only pressing **Transcribe selected recording** uploads that raw WAV to this
configured computer. The service holds it in memory, returns text and does not
save incoming files or transcripts. It does not call a cloud speech API. The app
saves the transcript alongside its original WAV locally. Only Export invokes
your platform's share/save interface.

English, Italian and Maltese (`en`, `it`, `mt`) are passed as recognition language
hints, not requested translations. Original sound is unchanged. The service uses
voice activity detection and rejects some low-quality segments. Those checks
cannot eliminate false words in noise or validate the meaning/source of speech.
Silence returns an empty transcript. Short radio fragments may produce no text.

Maximum: mono 48 kHz 16-bit PCM WAV, 120 seconds, one transcription at a time.
`GET /health` reports readiness. `POST /transcribe?language=mt` accepts a WAV body.
For LAN mode, supply `Authorization: Bearer YOUR_LONG_RANDOM_TOKEN`.

Tests: `python -m unittest -v`. Unit tests use a fake recognizer to verify the
protocol; the development validation additionally ran the real small model on
silence and synthetic English/Italian speech. Maltese speech accuracy has not
been validated on native speakers or live radio recordings.
