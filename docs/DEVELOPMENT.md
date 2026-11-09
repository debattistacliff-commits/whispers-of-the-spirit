# Whispers of the Spirit — isolated Windows development preview

Extract Whispers-Development-Preview.zip and run Whispers-Development/Ghostbox.App.exe. It uses app ID uk.whispers.development, so it does not use or replace the working VOX recording/settings folder. The production source and release downloads remain where they were.

The preview needs a local FFmpeg build with Rubber Band support for investigation processing. This computer already has one installed. On another computer, enter its FFmpeg executable path in Review. No paid API is used and the FFmpeg binary is not bundled.

Workflow: connect receiver → configure sweep → listen → record received audio → Review / Load selected saved take → choose a range → choose cleanup/voice tools → Apply Preview → compare Original/Processed → export or save a named session. Use the separate microphone button to record a question until you stop it. Live monitoring must stop before review playback, so the two sources are not mixed.

Effects start off. White noise is synthetic. Creative preview/export and live effects recording are separate explicit opt-ins. Raw reception and microphone originals are retained. Strong cleanup may erase faint words or create artifacts; compare against the original. Voice activity or transcription never establishes a speaker's identity or a signal's origin.

Read PHASE-1-INSPECTION.md through PHASE-5-VALIDATION.md for implementation details, tested paths and outstanding acceptance. This preview is for review only. No upgrade to the working app has been approved or deployed.

Backup-before-redesign.zip contains the pre-change project, runtime/releases, recordings and settings snapshot with a SHA-256 manifest. It contains your own private recordings/settings; keep it private. It is not included inside the preview or source ZIP.

Windows export fix: WAV/ZIP exports now open Save As instead of Windows Share. Opening and cancelling the dialog were verified. Use Windows-Preview-Final/Ghostbox.App.exe for the corrected build.

## Linux version (Ghostbox.Linux)

The Linux app compiles the same `Ghostbox.App` C# files and console page as Windows/Android; only
the MAUI start-up files (`App.xaml.cs`, `AppShell.xaml.cs`, `MauiProgram.cs`, `Platforms/`) are left out.
MAUI cannot open windows on Linux, so `Ghostbox.Linux` supplies the platform pieces:

- `LinuxHost.cs` opens a Photino.NET window (WebKitGTK 4.1) and runs the app logic on its own UI thread.
- `MauiStandIns.cs` replaces `WebView`, `Preferences`, `FileSystem`, `MainThread`, `FilePicker`,
  `Permissions` and `SemanticScreenReader`. The console's `whispers://` commands arrive through
  Photino's message channel (`window.__whispersSend`) instead of page navigation.
- `PulseAudio.cs` plays and records 48 kHz mono through libpulse-simple (PulseAudio or PipeWire).
- The shared files contain `#elif LINUX` branches only for audio output, microphone and the Save dialog.

Build with `./scripts/build-linux.sh` on Linux or WSL (.NET 10 SDK, no MAUI workloads). For automated
checks, `WHISPERS_TRACE=1` logs console commands and audio-device events, and `WHISPERS_TEST_SCRIPT`
names a JavaScript file that is run in the console page after it loads (used to click through the
console in WSLg tests). Neither is set in normal use.
