# VOX Ghost Box — Android and Windows

An app for sweeping a Gqrx radio receiver and shaping its streamed audio.
The Python CLI remains available in the separate `gqrx-ghostbox` folder.

## VOX dashboard and real frequency sweeper — version 1.4

The interface follows the supplied VOX reference: receiver controls, a large live
source display with session time and recording status, and Effects/Analysis/Review.
Windows uses three columns on wide windows. Narrow windows and Android use
Receiver/Live/Tools tabs. Playback and Stop stay accessible at the bottom.

**Remote SDR frequency sweeper** is now the default source. Press **Find public SDR
receivers**, choose a receiver, set an AM/shortwave range, step and dwell, then
press **Start RF sweep**. A connection goes directly to a real antenna receiver
over WebSocket; no USB radio or helper server is needed for reception.
The public directory only offers receivers advertising external-API access and
an available channel. Directory results are cached for at least 30 minutes.
Receivers can still become busy or impose session limits; choose another receiver
when the app reports a refusal. It does not bypass passwords or API restrictions.

Available demodulators are AM, USB, LSB, CW and narrow FM within the selected
receiver's range. Typical KiwiSDRs cover up to 30 MHz. **88–108 MHz broadcast FM
and DAB are not supplied by those HF receivers.** Gqrx mode remains available for
a suitable VHF receiver. DAB decoding is not implemented. Frequency displays are
requested tuning commands, not independent RF-frequency measurements.

Remote sweep directions: forward, backward, bounce, random. Dwell is 100–60,000 ms
and starts after audio arrives for the first frequency. Network and tuner latency
mean received fragments may lag commands. No generated noise replaces reception.

**Speak and listen:** while reception is running, press **Speak • record question**.
Playback can mute while the microphone records. Press it again; with automatic
listening enabled, the app then records the actual receiver audio for the chosen
duration. The microphone question, raw received sound, processed sound and tuning
log are separate files. This does not transmit your voice over radio.

Effects now include reverb and soft distortion alongside echo, chop, speech-band
focus, sweeping filter and volume. Generated demo sound is an explicitly selected
test source. There is no prerecorded voice bank or AI-generated reply.

## Online radio — version 1.3

**No radio hardware is needed for internet radio.** Select Internet station fragments as the source.
Choose Malta/Maltese, Italian or English, press **Find live stations**, and then
**Play online radio**. The app receives actual broadcaster streams directly
using LibVLC; a computer helper is not required for playback on Android.

Enable automatic switching to cycle forward, backward, bounce or random through
the loaded stations. Station listening time is 1–120 seconds, counted after audio
arrives. Disable automatic switching to listen to the selected station. You can
also paste a direct HTTP/HTTPS MP3/AAC/HLS audio stream URL.

Internet streams buffer, so switching is slower than an FM tuner and gaps are
possible. Failed streams are reported and skipped in automatic mode. If all
streams fail, playback stops. There is no replacement audio. Use the Effects
tab's Chop slider for a fast rhythmic sound without pretending to retune RF.
The station name is displayed; this mode does not measure AM/FM frequencies.

Directory metadata is community maintained by Radio Browser. Malta results
prefer stations tagged Maltese/Malti; programmes can still include other
languages and music. A language choice cannot guarantee current speech language.
Streams can change or be unavailable. Listening sends requests to the directory
and the broadcaster's stream host. The app does not download a voice bank or
generate answers. Recording, effects, spectrum and transcription work on the
actual decoded stream audio.

**Optional Gqrx SDR mode** still needs hardware. Turn off online mode to use it.
The Y-619's built-in tuner is not supported; it can be used as an output speaker.

## Speak, record and transcribe (version 1.2)

The **Record** tab provides Maltese (experimental), Italian and English recognition
choices. These do not force a broadcaster to speak that language.

1. On Windows, select the microphone you actually use
   (the default is the Windows preferred input). Press **Record my question** and speak into the device's
   microphone. Press it again to stop; there is no 60-second automatic cutoff. Android
   requests microphone permission when you first use this button.
   Live reception can continue while you speak; enable playback muting in Effects.
   The live microphone meter should move while you speak. A very quiet reading
   warns you to check the selected input before relying on a transcript.
2. Start live online radio or Gqrx audio reception. Return to **Record** and press **Record radio
   audio**. It saves the incoming sound for 1–120 seconds, or until stopped.
3. Raw reception and processed sound are saved in separate WAV files. The question
   is another file. No audio or generated answer is substituted for missing input.
   Demo sound cannot be recorded as a radio take.
4. Choose a saved recording. **Replay original** lets you hear it (stop scanning
   first). **Export** opens the platform's share/save UI with a ZIP of recordings,
   source notes and saved transcripts.
5. Start the included local **SpeechServer** on your computer, choose a language,
   then press **Transcribe selected recording**. See
   [SpeechServer setup](SpeechServer/README.md). No speech API key is needed.

The transcription engine runs on the computer, not inside the Android APK.
Android needs its LAN address and token. Windows uses localhost by default.
The WAV is sent only when Transcribe is pressed. Maltese is not yet validated
with native-speaker recordings; Italian/English were checked using synthetic
test speech. Every transcript is a machine estimate and must be checked by ear.
Recording after a question does not prove that the sound was a reply.

Recordings stay in the app's private `Recordings` directory until exported;
uninstalling the app can remove them. Foreground microphone capture stops when
the app backgrounds or closes. Your recorded question is not transmitted by RF.
See [research and P-SB7 references](RESEARCH.md).

## Try it now without a radio

On the **Radio** tab, enable **Demo sound**, then press **Start scanning**.
This plays explicitly labeled synthetic test tones and noise, not radio reception
or recorded voices. Use **Effects** to adjust the sound and **Analyze** to see it.
Press **Stop** before turning demo off and connecting to Gqrx.

The **Analyze** tab measures incoming audio before effects: RMS and peak dBFS,
strongest frequency (about 23 Hz FFT resolution), percentage of samples near full
scale, and a 32-band logarithmic spectrum from 50 Hz to 16 kHz. Measurements update
about eight times a second. They describe the waveform; they do not recognize
words, isolate speakers, or determine what caused a sound. Quiet mode suppresses
these live updates too.

## Install

**Android:** copy `GqrxGhostbox-Android.apk` to your phone and open it. Android may
ask you to allow installation from the app you used to open the APK. This is a
locally built, debug-key-signed APK for sideloading, not a Play Store release.
Keep this signing key if you build updates that should install over this version.
Android 7.0 or later with a 64-bit ARM or x86 processor is required. Keep Ghostbox open while scanning.
The Android build uses the official VideoLAN 3.7.0-beta native decoder to provide
16 KB native-page alignment. It is a prerelease dependency. This APK has not been
tested on a physical Android phone.

**Windows x64:** extract `GqrxGhostbox-Windows-x64.zip` completely, then run
`Ghostbox.App.exe` inside the extracted folder. Keep all the accompanying files.
The build includes .NET and Windows App SDK dependencies. It is not signed with
a commercial code-signing certificate. Windows 10 1809 or later is required.

## Connect the radio

1. Start Gqrx on your radio computer, select a working SDR device, and start DSP.
2. Enable **Remote control via TCP**. Usually its port is **7356**. For a phone or
   another computer, add that device's IP to Gqrx's allowed clients.
3. Enter the Gqrx computer's host/IP and TCP port in Ghostbox. On a phone,
   `127.0.0.1` means the phone itself: use the radio computer's LAN IP instead.
4. Pick a preset or enter a range, step, dwell, demodulator and pattern. Tap
   **Start scanning**. Tap **Stop** to end the session.

Gqrx and an SDR are required; the phone is not a standalone AM/FM tuner. Your SDR
must support the chosen frequencies (many basic RTL-SDRs cannot receive AM broadcast
without direct sampling or an upconverter). The medium-wave preset uses 9 kHz
spacing; change this to 10 kHz where appropriate.

## Hear audio and use effects

Tuning uses TCP. Audio uses a separate UDP stream that must be enabled in Gqrx:

1. In Gqrx **Audio settings → Network**, set the destination to the phone or
   Windows app computer's LAN IP and port **7355** (or your chosen UDP port).
2. Select **mono**, **48 kHz**, **signed 16-bit little-endian** audio. Turn on
   Gqrx's UDP/network output button. Stereo streams are not supported by this app.
3. Turn on **Play streamed audio in this app** in Ghostbox and enter that same UDP
   port. Start scanning. The status reports incoming audio or missing packets.
4. Adjust the effects while listening:
   - **Voice focus:** approximately 300–3400 Hz speech band emphasis. This is a
     filter, not voice recognition or separation of people from noise/music.
   - **Echo:** a 250 ms delay with decaying feedback; 0% bypasses the audible echo.
   - **Chop:** 0–12 pulses/second for the rhythmic “ch-ch-ch” sound; zero is off.
   - **Sweeping filter:** a slowly moving low-pass filter for a swishing sound.
   - **Volume:** playback level, initially 60%.

If no packets arrive, check the destination IP, Gqrx's UDP output button and local
network/firewall access for the chosen UDP port. The app accepts audio only from
the configured Gqrx host. If Gqrx also plays locally, you may hear both the dry
and processed audio; adjust its local output separately.

## Scan behavior

All runs start at a random point on `minimum + n × step`, at or below the maximum.
Forward/backward wrap; bounce reflects without repeating an endpoint and chooses
a random initial direction. Random draws independently, so it can repeat a point.
Single-point ranges work. An off-grid maximum is not visited.

Dwell is in milliseconds (10–60,000), after an acknowledged tune. Actual hop time
also includes network and Gqrx processing. Frequency inputs accept Hz or suffixes
such as `87.5M`, `531k`, `100kHz`. WFM is broadcast FM; FM is narrowband FM.
Passband is optional. Quiet hides tune activity while preserving errors.

Settings are saved when starting a valid session; effect settings are also saved
when stopping. Changing connection or scan parameters requires stopping first.
The effects remain adjustable while scanning. Stop/background/close cancels
tuning and audio, leaves Gqrx at its last mode/frequency, and does not stop DSP.
There is no automatic reconnect or claim of paranormal detection.

## Build from source

Requires .NET 10, MAUI Windows/Android workloads, Android SDK API 36 and a suitable
JDK. In this `Ghostbox` folder:

```powershell
dotnet workload install maui-android maui-windows
dotnet run --project Ghostbox.Checks

# Windows
dotnet publish Ghostbox.App -f net10.0-windows10.0.19041.0 -c Release -p:GhostboxPlatform=Windows -p:RuntimeIdentifier=win-x64 -p:SelfContained=true -p:WindowsAppSDKSelfContained=true

# Android (provide AndroidSdkDirectory / JavaSdkDirectory if not configured)
dotnet publish Ghostbox.App -f net10.0-android -c Release -p:GhostboxPlatform=Android -p:AndroidPackageFormats=apk -p:AndroidKeyStore=false
```

For a public release, use your own persistent Android signing keystore and a
Windows signing/distribution workflow. This project does not publish to stores.

`Ghostbox.Core` contains the shared scan engine, TCP protocol, validation, and
sample-by-sample DSP. `Ghostbox.App` contains the MAUI UI, UDP reception and native
audio playback (Android AudioTrack / Windows NAudio). `Ghostbox.Checks` is a
dependency-free verification program with a local TCP test server and DSP checks.

Protocol and audio format references:
[Gqrx remote-control specification](https://github.com/gqrx-sdr/gqrx/blob/master/resources/remote-control.txt),
[Gqrx UDP audio](https://www.gqrx.dk/doc/streaming-audio-over-udp).

## Version 1.5: one conversation file
Start the receiver, press Speak to record your question, then stop speaking. With automatic listening enabled, the app records 15 seconds of received radio audio afterward. Repeat as needed, then press Save WAV to export one conversation file (Windows save dialog; Android share/save sheet).

Completed microphone and raw receiver clips are joined in order without changing their samples. Pauses between clips are omitted. Source labels, original filenames and UTC timestamps are embedded in the WAV and stored in a companion markers text file inside the app recording folder. Original clips remain available in Review. Start a new conversation file resets the current collection without deleting recordings. No replies are generated. A recording captures whatever audio the receiver actually supplies.

## Version 1.6: waveform, spectrogram and listening markers
Open Tools → Analysis (or Analysis on the desktop). The top trace is a 2048-sample waveform at 48 kHz; the scrolling display below holds 120 FFT frames, about 15 seconds of received audio. Newest audio is on the right. Frequencies rise from 50 Hz at the bottom to 16 kHz at the top in 32 logarithmic bands. Dark/green/yellow colours show band amplitudes from -80 to 0 dBFS. The same display is available during saved-audio replay. It measures sound before effects; it does not measure heat, RF signal direction, distance or speaker identity. The source label identifies the actual receiver/stream or demo.

Press Mark this moment during receiver recording or replay. A manual marker records the sample-based audio offset and UTC click time in a companion .events.tsv file. Choose the original recording, choose its marker in Analysis, then press Replay in Review to start there. Markers belong to the individual clip, not the joined conversation file; Export recordings includes the companion files. Saved WAV audio is unchanged. Marks are listening notes, not speech detections. During replay the offset follows queued audio and may differ slightly from what is heard because of device buffering.

## Version 1.7: manual microphone stop and slow-motion replay
Your microphone continues recording until you press Stop recording question (or stop/leave the app session). The previous 60-second automatic cutoff has been removed. When automatic listening is enabled, receiver capture starts only after you manually finish the question. Device interruptions, storage errors or the WAV size limit can still end recording. This remains a foreground recorder.

In Review choose a saved radio clip, microphone recording or conversation WAV, then adjust Slow-motion replay speed from 0.25× to 1.00× and press Replay original. The slider also works during replay. Half speed doubles playback duration; quarter speed quadruples it. This is tape-style slow motion: pitch becomes lower. Live reception remains at normal speed to avoid accumulating delay or losing incoming audio. Saved WAV files remain untouched. Speed-adjusted export is not included. Speech transcription still accepts clips up to 120 seconds; long questions can be saved and replayed without transcribing them.

## Version 1.8: adjustable noise filter
Effects → Noise filter adds speech-band filtering (approximately 250–3500 Hz) and a soft downward expander to reduce rumble, high hiss and quiet background sound. Off is an exact bypass. Start around 25–40% and compare with the original; higher settings can suppress faint speech too. Strong continuous noise inside the speech band cannot be cleanly removed by this filter. It does not recognize, reconstruct or generate words.

The filter applies to live received playback before the existing effects and to the separate processed receiver recording. Raw receiver recordings, microphone recordings, joined conversations and original-file replay remain unchanged. Existing slow-motion and noise/demo controls are unchanged. No redesign is included.

## Version 1.9: Reduce noise switch
Effects now has a dedicated Reduce noise switch and a separate Noise reduction strength slider. Off bypasses the filter and disables the slider without forgetting the strength. New installations start switched off with 35% ready for use. Switch state and strength are remembered independently. Existing nonzero version 1.8 strength migrates to enabled if no saved switch preference exists. All other audio and design controls are unchanged.
