# Research notes — 1 November 2026

## Real remote antenna sweeper (version 1.4)

The remote-frequency option uses the documented protocol behavior in the
[official kiwiclient project](https://github.com/jks-prv/kiwiclient): authentication,
demodulator/frequency commands, sample-rate handshake, uncompressed mono audio,
and keepalive. The app's C# implementation is independent; it does not embed that
Python client. Received PCM is converted from big-endian samples and resampled
to 48 kHz for the existing recording/effects path.

Receiver discovery uses the [AetherSDR directory mirror and documented schema](https://github.com/aethersdr/AetherSDR/blob/main/docs/kiwi-json-schema.md),
with a 30-minute cache. Only positive published external-API allowances are offered;
disabled/unknown policy, offline/flagged entries and full receivers are excluded.
The app does not scrape the KiwiSDR origin directory or disguise itself as a browser.

A live public receiver, Ireland North West 5, delivered 192,528 bytes of decoded
antenna audio while the app test sent eight 531–576 kHz tuning commands. Typical
0–30 MHz KiwiSDRs support AM/shortwave and HF narrow FM; they do not receive
broadcast FM 88–108 MHz or DAB. The UI exposes this limit instead of claiming those
bands work. The current client does not decode DAB.

## Internet radio (version 1.3)

The clarified requirement is online broadcaster streams without SDR hardware.
[Radio Browser's API](https://docs.radio-browser.info/) supplies current station
URLs and language/country metadata. The app requests Malta/Maltese, Italian or
English stations, supports a direct stream URL, and decodes MP3/AAC/HLS through
[LibVLCSharp](https://docs.videolan.me/libvlcsharp/docs/home.html). It uses decoded
48 kHz mono PCM for effects, measurements and raw/processed recordings.

Live Calypso Radio and Rai Radio 1 streams were opened and decoded during
validation. Approximately two seconds of non-silent PCM arrived from each.
No prerecorded voice source was involved. This establishes internet-stream
reception, not RF reception. Internet mode displays station names rather than
claiming measured AM/FM frequencies. Buffering affects station switching.
Directory language metadata cannot guarantee the language of each programme.

## P-SB7: what is documented

The [GhostStop SB7 listing and directions](https://www.ghoststop.com/spirit-box-sb7/)
describe AM/FM reception, forward/reverse sweeping and adjustable dwell times.
The listing names 100–350 ms rates and separately advertises 50 ms in newer
revisions. It describes proprietary synthetic noise between tuning steps and a
50 kHz flutter feature. These are product operating claims, not a released
algorithm or source-code listing. Model revisions differ.

Searches for P-SB7 source code, firmware releases and control APIs did not locate
a verified public release. This does not prove that no such material exists.
The manufacturer-authored [P-SB7 user manual filed for certification](https://fccid.io/2AVMZ-P-SB7/User-Manual/User-manual-8105758.pdf)
is available to read; it is documentation, not an open-source firmware package.
The Pro model is a separate device and should not be treated as interchangeable.

Our app implements its own documented tuning grid and timing through Gqrx. It
does not contain P-SB7 firmware and does not claim to remotely control a P-SB7.
Unlike the synthetic-noise feature described in the listing, our live stream
does not add generated noise. Optional effects modify only received audio.
The explicitly labeled demo remains a separate test source and cannot be saved
as a radio take through the Record tab.

## Real signal path

The [official Gqrx remote specification](https://github.com/gqrx-sdr/gqrx/blob/master/resources/remote-control.txt)
documents TCP frequency and demodulator commands and acknowledgements.
[Gqrx's UDP audio documentation](https://www.gqrx.dk/doc/streaming-audio-over-udp)
specifies mono 48 kHz signed 16-bit little-endian audio. This is our input format.
The app verifies the UDP source against the configured Gqrx host, records the
unmodified incoming PCM, and separately records the effects output. That proves
what the app received; independent hardware checks are still needed to establish
the RF source. No live Gqrx/SDR is available in this development session.

## Languages and transcription limitations

[Whisper's language table](https://github.com/openai/whisper/blob/main/whisper/tokenizer.py)
includes Maltese (`mt`), Italian (`it`) and English (`en`). Language support is
not a guarantee of usable accuracy, especially for Maltese and fragmented radio.
Its [model card](https://github.com/openai/whisper/blob/main/model-card.md)
warns that it can produce words not spoken in the input and has uneven accuracy
across languages. Noise and effects are therefore not evidence of recognized
speech. Our UI marks Maltese experimental and transcripts machine-generated.

The local service uses [faster-whisper](https://github.com/SYSTRAN/faster-whisper),
voice-activity filtering, no previous-segment text conditioning, and conservative
segment rejection heuristics. These reduce but do not eliminate false transcripts.
Only raw recordings are offered for transcription. Original WAVs remain available
for replay and export. Nothing translates the broadcast or generates an answer.
The spoken question is not passed to the recognizer as a text prompt, to avoid
priming the transcript. Radio takes include a UTC tuning-command CSV: these are
acknowledged requested frequencies, with approximate network timing, not an RF
measurement or sample-accurate proof of the broadcast source.
Recording a sound after a question does not establish that it is a reply.
