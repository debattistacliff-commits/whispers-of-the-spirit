# Phase 1 — inspection and implementation plan

The existing application is .NET 10 MAUI with a C# interface, shared DSP/protocol core, Windows NAudio, LibVLCSharp for broadcaster streams, and a separate local Python faster-whisper speech server. Entry points are MauiProgram, App.CreateWindow and MainPage. No web frontend or paid API is required.

## Existing capabilities

| Area | Existing implementation and limits |
|---|---|
| Receiver | Public API-enabled KiwiSDR discovery, real WebSocket antenna audio, frequency tuning, AM/USB/LSB/CW/NFM, band presets, bounds validation, forward/backward/bounce/random. Gqrx TCP control and UDP audio retained. Internet stream fragments are a distinct source. Public availability is variable. |
| Receiver controls | Source picker, find receivers, receiver picker, band/mode/pattern pickers, range/step/dwell entries, Gqrx connection/timeout/passband/UDP controls, quiet mode, start and stop. Some source-specific controls are hidden until that source is selected. |
| Simulation | Explicit demo produces synthetic tones/noise; it is not reception and cannot be recorded as received radio. No generated replies. |
| Recording | Manual-stop microphone, device picker, level meter, optional mute while speaking and timed receiver capture after a question. Raw and effects WAVs are separate. Conversation WAV joins original question/reception clips with embedded metadata. Save/export preserve originals. |
| Analysis | Pre-effect FFT, RMS/peak/clipping, waveform and rolling spectrogram; listening markers and seek from marker. Heat map is audio amplitude, not physical heat/location. |
| Speech | Optional local/LAN transcription mt/it/en. Maltese accuracy remains experimental. Source includes a new saved-clip voice-activity endpoint/button; newest source and published Windows build are 2.0, while the last completed distributable release is 1.9. Endpoint/package verification was interrupted by approval-review usage limits. |
| Existing effects | Voice-band filter, simple noise gate/band filter, sweeping low-pass, echo fixed at 250 ms, reverb, saturation, chop, volume. Live adjustments work. Effects preferences are stored; they are not full named investigation sessions. |
| Playback/review | Original WAV replay at 0.25–1x tape speed, saved recording list, markers, original export and transcript export. No pitch preservation, reverse, sample-range workspace, arbitrary import, named sessions or A/B processed preview yet. |

## Isolation and backup

`Backup-before-redesign.zip` captures current source/resources, published runtime, existing distributable archives, original recordings and user settings. Reproducible bin/obj and disposable cache are excluded and documented. A SHA-256 manifest is inside. Active recordings can continue after the snapshot. Nothing is moved or deleted.

Development lives in `work/Whispers-Dev`, using application ID `uk.whispers.development` and its own recording/settings directory. No production executable, preferences, source file or recording will be replaced. Published development builds go only into this delivery directory. Production approval remains a final separate step.

## Plan presented before implementation

1. Rebuild the desktop dashboard as three persistent columns with a centered title, dark translucent blue panels, real central waveform, red circular record control, and five tool tabs. Preserve receiver methods and source validation.
2. Add reusable effect controls with explicit switches, numeric values, sliders, reset and tooltips. All processing/creative effects start bypassed in this isolated app. Separate creative effects from cleanup and voice tools.
3. Use the already-installed free FFmpeg engine for offline review transformations: profile-based FFT denoise, hum/hiss/click filtering, cutoffs, EQ/compression/limiting, normalization, pitch-preserving and tape speed, reverse. Use argument arrays, local files and cancellable background processes; never shell-interpolate file paths. Keep originals immutable. Record processing recipes with exports.
4. Extend Review with audio import, time selection/loop, waveform/spectrogram zoom, annotated markers, Original/Processed comparison, selected/full exports and named session save/restore. Run expensive processing off the UI thread. Disable conflicting actions and show progress/errors.
5. Test DSP with measured fixtures plus copies of real noisy radio clips, verify source hashes and session roundtrips, inspect the desktop UI and every new control, then deliver a clearly labelled development preview with test/limitation reports. No deployment to the working app without approval.

Voice attenuation on mono radio cannot reliably isolate a speaker; expose only a clearly labelled speech-band attenuation EQ, with no source-separation claim. Stereo imports can be converted to mono/stereo for export, but original channels remain preserved in the imported source. Effects are preview/export operations and require an explicit processed export choice. A live receiver continues independently during review processing.

## Acceptance boundaries

Do not call simulated fixtures hardware tests. Public receiver outages and unavailable audio devices must be reported. A waveform must use samples, not decoration. No control is complete until its actual processing and bypass are checked. A visual preview is not deployment approval.
