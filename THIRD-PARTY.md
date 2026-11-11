# Third-party components

Internet playback uses [LibVLCSharp](https://code.videolan.org/videolan/LibVLCSharp)
and [LibVLC](https://www.videolan.org/vlc/libvlc.html), distributed by VideoLAN.
Their source, license notices and native dependency details are available from
the linked upstream projects and the corresponding official NuGet packages.
LibVLCSharp and the supplied LibVLC libraries are dynamically linked components.
Native VLC plugins retain their upstream licenses and notices.

Windows audio input/output uses [NAudio](https://github.com/naudio/NAudio) (MIT).
The SLS camera (Android and Windows) uses Google [MediaPipe Tasks Vision](https://github.com/google-ai-edge/mediapipe)
1.2.0 and the Pose Landmarker "lite" model, both under the
[Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0). They are bundled inside the apps and
run on the device without an internet connection.
The optional Kinect support on Windows uses Microsoft's Kinect for Windows Runtime 1.8, which the user
installs separately; it is not included in the download. Kinect and Xbox are trademarks of Microsoft.
The README illustrations of the SLS camera were rendered with [three.js](https://threejs.org/) (MIT);
three.js is not part of the apps.
The Linux version shows the console with [Photino.NET](https://github.com/tryphotino/photino.NET)
(Apache-2.0), which uses the system's WebKitGTK; it plays and records sound through the
system's PulseAudio/PipeWire client library and plays internet radio with the system's LibVLC.
These system libraries are installed by the user and are not included in the Linux package.
The UI uses [.NET MAUI](https://github.com/dotnet/maui) (MIT).
Optional local transcription uses [faster-whisper](https://github.com/SYSTRAN/faster-whisper)
and multilingual Whisper model weights; these are installed separately by the
user and are not embedded in the APK or Windows archive.
Station directory data comes from [Radio Browser](https://www.radio-browser.info/).
Broadcast content belongs to the respective broadcasters; the app does not
redistribute a station-content library or provide recordings bundled as radio.

The console page embeds generated CSS (`Ghostbox.App/WebConsole/console.offline.css`) so it
works without an internet connection. It contains output of [Tailwind CSS](https://tailwindcss.com)
with the forms and container-queries plugins (MIT), icon shapes from
[Font Awesome Free 6.4.0](https://fontawesome.com) (icons CC BY 4.0), and the fonts
[Share Tech Mono](https://fonts.google.com/specimen/Share+Tech+Mono),
[JetBrains Mono](https://www.jetbrains.com/lp/mono/) and
[Space Grotesk](https://fonts.google.com/specimen/Space+Grotesk), all under the SIL Open Font
License 1.1, taken from the Fontsource packages. Rebuild it with `npm install && npm run build`
in `Ghostbox.App/WebConsole/tools`.
Review deck playback slow-down with pitch kept uses FFmpeg's rubberband filter from the
locally installed FFmpeg build; FFmpeg is not bundled.
