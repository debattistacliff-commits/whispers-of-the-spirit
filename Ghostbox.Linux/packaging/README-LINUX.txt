Whispers of the Spirit 2.3 for Linux (64-bit PC)
================================================

1. Install the system parts it uses (once):

   Ubuntu 22.04+ / Debian 12+ / Linux Mint 21+:
     sudo apt install libwebkit2gtk-4.1-0 libpulse0 libvlc5 vlc-plugin-base ffmpeg

   Fedora 39+:
     sudo dnf install webkit2gtk4.1 pulseaudio-libs vlc-libs ffmpeg
     (vlc and ffmpeg come from RPM Fusion)

   What each one is for:
     WebKitGTK 4.1   the console window                      (required)
     libpulse        sound output and microphone; works with
                     PipeWire and PulseAudio                 (required)
     libvlc / VLC    internet radio stations                 (optional)
     ffmpeg          Review deck: slow-down that keeps the
                     voice pitch, clean-up, MP3/FLAC import
                     and MP3 export                          (optional)

2. Start it: open this folder and run   ./whispers-of-the-spirit
   To add it to your applications menu:  ./install-desktop-entry.sh

3. The gear button (top right) opens Settings: language (English, Malti, Italiano,
   Español, Français, Deutsch), theme (including Material and Ember), text size,
   a short guide and About.

Where things are kept:
   Recordings and review work   ~/.local/share/whispers-of-the-spirit
   Settings                      ~/.config/whispers-of-the-spirit

The microphone is the default input chosen in your desktop's sound settings.
.NET is included; nothing else needs installing.
