# Working on Whispers of the Spirit (for AI coding agents: Codex, Claude and others)

The owner is a ghost-box / EVP hobbyist, not a programmer. Explain results in plain language.
Two agents may work on this project: **Codex** and **Claude**. They do not talk to each other
directly — they cooperate through this file, `docs/SLS-TASKS.md` and pull requests.

## Ground rules

1. **Ask the owner before** pushing to GitHub, publishing a release, or installing on the phone.
2. **Don't change what works.** Windows, Linux and Android behaviour stays the same unless a task
   says otherwise. Phone-only features live in `Ghostbox.App/WebConsole/phone-*.js` (embedded only
   for `net10.0-android`) and `Ghostbox.App/Platforms/Android/`.
3. **Honesty.** The app never claims to detect or prove spirits. Keep `DISCLAIMER.md` true.
4. **Rights.** All rights reserved (see `LICENSE`). Add every third-party component to
   `THIRD-PARTY.md` and `Licenses/` with its licence.
5. **Languages.** Every new visible text goes into `phone-i18n.js` in all 6 languages
   (English, Malti, Italiano, Español, Français, Deutsch).
6. **Phone UX.** Pages fit the screen, Start/Rec stay at the bottom, sliders use touch-and-hold.

## Working together

- Pick a task in `docs/SLS-TASKS.md`, set it to `doing`, work on **your own branch**
  (`sls/codex-…` or `sls/claude-…`), never on `main` directly.
- When done: set `review`, open a pull request, and write a short note in the board's Notes.
- The other agent reviews the pull request (correctness, rules above, tests) before it is merged.
- Don't edit files another agent has `doing` on its branch; leave a note instead.

## Before you say "done"

```powershell
dotnet run --project Ghostbox.Checks                                   # all checks pass
dotnet build Ghostbox.App/Ghostbox.App.csproj -p:GhostboxPlatform=Windows -c Release
./scripts/build-android.ps1 -AndroidSdk <sdk> -JavaSdk <jdk>          # Android builds
```
Linux: `./scripts/build-linux.sh` on Linux/WSL (or at least `dotnet build Ghostbox.Linux`).
Test phone features on the owner's Samsung Galaxy A05s (Android 15) when the owner allows it.

## Map

See `README.md` (Repository layout) and `docs/DEVELOPMENT.md`. Current plan: `docs/SLS-CAMERA-PLAN.md`.
