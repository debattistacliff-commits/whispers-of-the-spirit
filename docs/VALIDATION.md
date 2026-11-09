# Verification

Verified locally on Windows on 1 November 2026:

- Shared scanner checks pass: input units/validation, forward/backward wrap,
  bounce reflection, random starts/directions, single-point scans, bounded grids.
- A local TCP server verifies mode/tuning commands, fragmented acknowledgements,
  failed commands, command timeouts and cancellation.
- DSP checks pass: dry pass-through, the 250 ms echo, and complete volume mute.
- Analyzer checks pass: a 1 kHz sine is located within one FFT bin with the correct
  RMS level, unclipped input stays unclipped, and silence has a finite floor.
- Windows app was launched and visually inspected. Demo playback, tab navigation,
  live spectrum/levels and Stop were exercised. The synthetic 440 Hz tone was
  measured near 445 Hz, consistent with the FFT resolution.
- Windows Release publish is self-contained for x64.
- Android Release APK compiles; its v2/v3 signatures verify successfully. Package
  metadata confirms Android API 24 minimum, API 36 target, arm64/x64 architectures.
  No Android device
  was attached, so on-phone startup, playback and layout remain unverified.

No working Gqrx/SDR was connected for live reception tests. The Y-619 speaker's
USB connection was not used. The app does not implement control of that speaker's
built-in tuner. Audio analysis is waveform measurement; optional transcription is
separate and explicitly machine-generated.

## Version 1.1 additions

- WAV and recording checks pass: correct RIFF header/length on finalization,
  original samples preserved byte-for-byte, processed output stored separately,
  idempotent stop.
- Speech-server unit tests pass for all three language codes, invalid/truncated
  WAVs, segment filtering, token authorization and real HTTP request handling.
- The actual cached multilingual `small` model loaded successfully on CPU.
  Silence produced no transcript in Maltese, Italian or English. Synthetic
  English and Italian speech fixtures were transcribed correctly in the tested
  phrases. These are integration fixtures, not live radio or native-speaker
  accuracy evaluations. Spoken Maltese accuracy remains untested.
- Windows and Android microphone/recording UI compile. Physical microphone
  recording and Android permission/playback behavior require device testing.

## Version 1.2 microphone diagnosis

A Windows microphone test successfully saved a 23.34-second WAV and invoked
playback and Maltese transcription. Its average was approximately -71.4 dBFS;
the speech engine returned no clear speech. The app had used the Kinect input
(device zero), whereas the user identified the NexiGo webcam microphone as their
intended source. Added a Windows microphone picker, default-device mapping and
a live microphone RMS meter with a quiet-input warning. This is a failed speech
recognition test to diagnose, not evidence of working Maltese accuracy.

## Version 1.3 online radio

The radio requirement was clarified to mean live internet stations, without SDR
hardware. The Windows decoder checks opened current Calypso Radio AAC and Rai
Radio 1 MP3 streams. They received 193,836 and 192,962 bytes of mono 48 kHz PCM,
with RMS around -19.6 and -19.3 dBFS in those short samples. This was actual
network audio. Content and levels change over time.
The same decoder source is linked into the app and the live check project.
`Ghostbox.RadioChecks` is an explicit live-network check, not an offline unit test.
Android still needs on-device runtime/playback testing.

The actual Windows app was also observed playing Radju Maria Malta in online
mode, reporting received live audio and changing stations with automatic random
switching enabled. Directory checks returned five Maltese-tagged streams and
non-empty Italian/English lists. No Gqrx process or SDR was used in these checks.
The internet player uses the same effects, analysis and recording PCM path.

Android's stable decoder produced 16 KB alignment warnings. The official
VideoLAN 3.7.0-beta native package was inspected: arm64/x64 ELF LOAD segments
are aligned to 16 KB. The build was updated to that prerelease package. This
addresses the observed packaging issue, not a substitute for on-phone testing.
The final APK passed v2/v3 signature verification and the Android SDK zipalign
check with 16 KB native-library alignment enabled.

## Version 1.4 remote RF and VOX dashboard

- Live remote-antenna test passed on an API-enabled public receiver: 192,528 bytes
  of audio and eight tuning commands spanning 531–576 kHz in AM.
- Shared tests pass for network PCM endianness/sample-rate conversion, a 1 kHz
  resampling fixture, and volume mute with reverb/distortion active.
- The interface now follows the supplied VOX reference with adaptive panels,
  a session clock, source/recorder badges and separate review controls.
- Ask/listen sequencing and playback muting were implemented; physical microphone
  recording alongside live remote reception still needs a user/device test.
- Broadcast FM and DAB cannot be claimed from the tested HF receiver.

The redesigned Windows app was launched and visually inspected in its three-column
VOX layout. It connected directly to Ireland North West 5, displayed changing AM
frequencies and an RF LIVE badge, and continued sweeping while recording.
The recording check saved 15.0199 seconds in both raw and processed mono WAVs;
the two audio versions differed as expected, and the tuning CSV contained 62 lines.
Android 1.4 compiled and passed signature and 16 KB zip-alignment checks. Physical
Android playback and adaptive phone layout remain to be verified on-device.

Version 1.5: Windows build passed with zero warnings/errors. Automated conversation checks confirm unchanged PCM and clip order, correct RIFF length, embedded markers, playback boundaries excluding metadata, replacement without duplicate audio, and preservation of existing files after invalid input. Native export dialog and Android recording/share behavior still require user testing.

Version 1.6: known-tone checks verify signed waveform amplitude and FFT frequency/RMS, and silence produces a flat waveform and empty spectrogram. Existing audio preservation and protocol checks pass. Manual marker offsets use PCM byte counts rather than wall-clock recording duration. Android physical-device display and recording tests remain outstanding.
Version 1.6 packaging: Windows publish and Android publish succeeded. APK signature and 16 KB ZIP alignment verified. Windows UI displayed the waveform and scrolling spectrogram during replay of a previously captured receiver WAV. Idle marker guard verified; marker seeking and Android runtime require further user testing.

Version 1.7: checks confirm correct duration at 1×, 0.5× and 0.25× across streamed chunk boundaries, expected tone-frequency change, unchanged input PCM, and sample-exact normal playback including its final sample. Microphone cancellation now has no automatic timeout. Windows/Android hardware playback and extended microphone recording require user testing.
Version 1.7 packaging: Windows and Android publishes succeeded. APK signature and 16 KB alignment checks passed. Windows UI verified the slider value changing to 0.50× and receiver-WAV replay entering playback. No new microphone capture was made during validation.

Version 1.8 filter checks pass: strong 1 kHz speech-band tone retained, 40 Hz rumble and 12 kHz hiss tones attenuated, quiet background attenuated, filter-off exact bypass. Existing protocol, recording preservation and slow-motion checks all pass. Real-world intelligibility and Android runtime still require listening tests.
Version 1.8 Windows publish succeeded; the Effects screen was visually checked with the Noise filter slider at Off and existing effect values retained.
Version 1.8 Android publish succeeded. APK signature and 16 KB ZIP alignment verified. Android runtime and real-world filter listening still require user testing.

Version 1.9: all automated checks passed. A mixed test containing a 1 kHz tone, 40 Hz rumble and 12 kHz hiss showed an 11.5 dB improvement in tone-to-noise ratio at full filter strength after settling. This is synthetic test evidence, not a claim about intelligibility of real radio speech. Windows UI verified Reduce noise on enables the strength slider at its retained 35% setting. Windows publish succeeded. Android hardware listening remains outstanding.
Version 1.9 Android publish, APK signature and 16 KB ZIP alignment checks passed. Windows switch-off UI check confirmed the retained 35% slider becomes disabled. Android runtime remains untested.
