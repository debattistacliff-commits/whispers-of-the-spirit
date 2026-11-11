# SLS Camera — task board

Shared by **Claude** and **Codex**. Update the status when you start or finish a task, and leave a
line in the Notes section for the other one. Statuses: `todo` · `doing` · `review` · `done`.
Plan: [SLS-CAMERA-PLAN.md](SLS-CAMERA-PLAN.md). Rules: [AGENTS.md](../AGENTS.md).

## Phase 0 — Proof (spike)

| # | Task | Owner | Reviewer | Status | Branch |
| --- | --- | --- | --- | --- | --- |
| 0.1 | Serve the console on Android from a secure origin (`WebViewAssetLoader`, `https://appassets.androidplatform.net/`) without changing Windows/Linux | Codex | Claude | done (Claude) | `sls/codex-secure-origin` |
| 0.2 | Android CAMERA permission + `WebChromeClient.OnPermissionRequest` granting the page's camera request | Codex | Claude | done (Claude) | `sls/codex-secure-origin` |
| 0.3 | Bundle MediaPipe Tasks Vision WASM + `pose_landmarker_lite.task` offline; licence notice | Codex | Claude | done (Claude) | `sls/codex-mediapipe` |
| 0.4 | Minimal test page in the app: camera + skeleton + detections-per-second counter | Claude | Codex | done (Claude) | `sls/claude-spike-ui` |
| 0.5 | Measure on the Samsung A05s (speed, offline, battery 10 min) and decide web vs native route | Claude | Codex | done: web route (10/s back cam, 9/s front); battery test still open | — |

## Phase 1 — Camera screen

| # | Task | Owner | Reviewer | Status |
| --- | --- | --- | --- | --- |
| 1.1 | SLS tab in the phone bar; full-screen camera page | Claude | Codex | done (Claude) |
| 1.2 | Overlay: skeleton, corner frame, label, up to 3 figures | Claude | Codex | done for 1 figure (Claude) — 3 figures later |
| 1.3 | Intensity bar, sensitivity slider (touch-and-hold), detection ON/OFF | Claude | Codex | done (Claude) |
| 1.4 | Filters: Green, Ember, Thermal-style, Plain; front/back camera; torch | Codex | Claude | done (Claude) |
| 1.5 | Texts in 6 languages + honesty note | Claude | Codex | done (Claude) |

## Phase 2 — Record + History

| # | Task | Owner | Reviewer | Status |
| --- | --- | --- | --- | --- |
| 2.1 | `SlsStore.cs`: save video/snapshot/metadata, list, rename, delete, share | Claude (owner asked to keep going) | Codex | done — needs review |
| 2.2 | Recording with the overlay (canvas + MediaRecorder) and snapshots | Claude | Codex | done — needs review |
| 2.3 | History screen | Claude | Codex | done — needs review |

## Phase 3 — Session features

| # | Task | Owner | Reviewer | Status |
| --- | --- | --- | --- | --- |
| 3.1 | Motion indicator | Claude (owner asked to keep going) | Codex | done — needs review |
| 3.2 | Link SLS recordings with radio recordings; markers in the Review deck | Claude | Codex | done — needs review |
| 3.3 | Optional alert sound | Claude (owner asked to keep going) | Codex | done — needs review |

## Phase 4 — Desktop

| # | Task | Owner | Reviewer | Status |
| --- | --- | --- | --- | --- |
| 4.1 | Windows (WebView2 webcam permission) | Codex | Claude | todo |
| 4.2 | Linux (WebKitGTK webcam permission in Ghostbox.Linux) | Claude | Codex | todo |

## Notes between Claude and Codex

- 2026-11-11 (Claude): plan and board written; nothing started. Waiting for the owner's go-ahead.
- 2026-11-11 (Claude): owner said go. To keep the spike fast Claude did 0.1–0.4 alone (not on a branch yet, uncommitted in the working copy) — Codex, please review when you join:
  `Platforms/Android/SlsWebHost.cs` (secure origin https://appassets.androidplatform.net/, assets served from APK, camera permission),
  `AndroidManifest.xml` (CAMERA), `MauiProgram.cs` (#if ANDROID register), `WebConsole.cs` (#if ANDROID BaseUrl, loads phone-sls.js),
  `WebConsole/phone-sls.js` (test screen), MediaPipe tasks-vision **1.2.0** + pose_landmarker_lite in `Platforms/Android/Assets/sls/`.
  Results on the Samsung A05s: camera + permission dialog OK, engine loads offline on **GPU**, person detected (front camera),
  speed **8–9 detections/s** (goal 10; numPoses lowered 3→1). Web route looks viable; decide after a test in normal room light.
  Known: language/theme reset once because the page origin changed. Settings entry is a "radar scanner" button (owner's choice A).
  Still to do for Phase 0: THIRD-PARTY/Licenses entry for MediaPipe (Apache-2.0), battery test, and Codex review.
- 2026-11-11 (Claude): Phase 1 built and tested on the A05s (still uncommitted, same working copy):
  SLS is page 8 with an "SLS" tab (`phone-material.js`: plan entry `['sls', …]`, `pp-fill` page, `whispers-page` event,
  `window.__whispersGoToSection`, `window.__whispersTr`); `phone-sls.js` is now the full page: night-vision filters
  (Green / Ember / Thermal-style SVG colour map / Plain), flip, torch (only when the camera supports it), DETECTION switch,
  SLS intensity bar (smoothed), sensitivity slider (touch-and-hold, saved), honesty note; texts in 6 languages (`phone-i18n.js`).
  Camera runs only while page 8 is shown and the app is visible — verified with `dumpsys media.camera` (DISCONNECT on leaving).
  Speed 10/s back camera, 9/s front camera, GPU. numPoses = 1 for speed (3 figures = later option).
  Settings radar button now jumps to page 8. Codex: please review Phase 0 + 1 before Phase 2.
- 2026-11-11 (Claude): Phase 2 built and tested on the Galaxy A05s. `Platforms/Android/SlsStore.cs` is a
  `@JavascriptInterface` (`window.WhispersSls`: begin/append/finish/list/rename/remove/share) writing to
  `AppDataDirectory/SLS`; names are checked by regex `^SLS-\d{8}-\d{6}(-\d+)?\.(webm|jpg|json)$`, so the page
  cannot reach other files. `SlsWebHost` serves them at `/sls-files/` (whole file, no Range support — the History
  player therefore fetches the WebM into a blob URL). Recording = composite canvas (filter + skeleton + time stamp
  + REC dot) → `captureStream(15)` → MediaRecorder VP8 1.5 Mbit/s, 1 s chunks base64 to the bridge, max 10 min,
  saved when leaving the page. Thumbnail = strongest moment. Codex: please review security of the bridge
  (base64 size, names) and whether Range support in `/sls-files/` is worth adding.
- 2026-11-11 (Claude): Phase 3 built and tested on the A05s.
  - Motion: 48×36 grey frame difference every 3rd frame, smoothed, blue bar "MOTION" (works with detection off).
  - Figure event = a figure in 3 frames in a row after none, max one per 5 s. It (a) beeps if the 🔔 switch is on
    (Web Audio, off by default, saved as `sls-bell`), (b) adds `{t, note}` to the SLS video's `markers`, and
    (c) if a radio recording runs (`state.record === 'RECORDING'`, read by chaining `window.whispersNativeUpdate`)
    sends `whispers://slsmark?value=score|figures`.
  - Android-only `Platforms/Android/SlsMarkers.cs` (partial MainPage): `MarkSlsFigure` appends
    "SLS camera: figure NN%" to the recording's `.events.tsv` (same file as "Mark this moment"), and
    `LoadSavedEventMarkers` adds `.events.tsv` rows to the Review deck markers when a recording is loaded
    (`#if ANDROID` hook in `ReviewWorkspace.cs`; Windows/Linux unchanged). Verified: 2 camera markers at ~9 s and
    ~17.5 s shown in the deck's MARKERS lane.
  - Not done: several figures at once (numPoses stays 1 for speed). Codex: please review the `#if ANDROID` hooks
    in `WebConsole.cs` / `ReviewWorkspace.cs`.
