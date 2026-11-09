# Phase 5 — validation and acceptance status

This is a development preview, not an approved production upgrade. The working VOX installation, production project and existing user data were not replaced.

| Check | Result |
|---|---|
| Windows development build/publish | Passed using cached dependencies; a NuGet source warning remains because package-network access is restricted. |
| Core DSP, frequency sequence and WAV preservation checks | Passed with test files in the workspace. Socket-based mock Gqrx checks were excluded with the explicit --dsp-only option after the environment denied socket access. |
| Investigation engine checks | Passed; raw test logs included in Tests/. Synthetic fixtures are not hardware or subjective speech-quality tests. |
| Live public receiver discovery | Passed in the development app: Ireland North West 5, 0.001–30 MHz capability display. |
| Real RF reception/sweep | Passed in the app: changing frequency, 441 hops, nonzero received audio, responsive central waveform and spectrogram. |
| Radio record and stop | Passed: about 15 seconds saved; raw WAV and conversation WAV present, no processed WAV with opt-in off. Stop returned receiver to idle. |
| Tabs and live echo bypass | Passed during reception; hop counter continued as Echo was enabled and bypassed. |
| Import/render/review | Native WAV import, original waveform/spectrogram, captured-take loading and Apply Preview passed. Preview showed RMS/peak and 0.000% clipping. Playback activated pause control. |
| MP3/FLAC import, speed, reverse, mono/stereo, filters | Automated engine checks passed. Pitch/duration checks use known tones, not human speech labeling. |
| Markers/session | GUI saved notes and recipe; reopen reported original hash verified. Automated JSON roundtrip passed. |
| Full set of visible controls | Not yet exhaustively exercised manually. No claim that every button/dropdown has completed acceptance. |
| Microphone and speaker listening | Fresh microphone test was not performed. Physical audio quality, long playback/loop, hearing-based A/B comparison and device-loss cases remain to be checked. |
| Save/export dialogs | Fixed Windows exports to use Save As instead of Share. GUI verified Save As opens and Escape cancels cleanly with original retained. Positive destination-save acceptance remains pending. No files were sent. |
| Gqrx hardware and all public receivers | Not tested in this preview. Protocol/source code retained; public receiver availability varies. |
| Android | Not redesigned, published or deployed in this desktop phase. Existing Android deliverables are untouched. |
| Deployment | Not performed. Requires your approval after acceptance work. |

Early Windows native picker targeting failed in the automation helper; import was successfully completed via its keyboard shortcut. Early test file replacement in the sandbox's redirected temp folder was denied; the same preservation checks passed using the authorized workspace. The previous turn's approval-review usage failure was not retried or bypassed; independent local development and normal app UI tests proceeded.

Pending: visual approval, full UI/control matrix, export dialogs, microphone/device tests, extended listening, remaining hardware paths and small desktop/DPI layouts. Do not replace the working app on the basis of this report alone.

