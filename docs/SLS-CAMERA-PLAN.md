# SLS Camera — detailed plan

Status: **Phase 0 (proof) mostly done on 2026-11-11** — camera, offline engine and skeleton work on the Samsung A05s at 8–9 detections/s. Written 2026-11-11, after the
Android 2.1 release. Work is shared between **Claude** (Claude Code) and **Codex** (OpenAI Codex);
how they cooperate is in [AGENTS.md](../AGENTS.md), and who does what is in [SLS-TASKS.md](SLS-TASKS.md).

## 1. Goal

A new **SLS** part of the Whispers of the Spirit phone app. The camera looks for human-like body
shapes and draws a stick-figure skeleton over them in real time, in the app's night-vision style,
so one investigation can combine radio sweeps (EVP) and camera (SLS).

**Honesty rule (non-negotiable):** the software finds *human-like shapes*, not spirits. Shadows,
coats, chairs and patterns can trigger it. The camera screen, History and Disclaimer must say so,
in all 6 languages.

## 2. What the owner gets (user-visible features)

1. **SLS tab** next to Listen / FX / Review / ⚙ — camera opens full screen.
2. **Live view** with night-vision filters: Green (classic), Ember (orange), Thermal-style, Plain.
3. **Skeleton overlay** — up to 3 figures, 33 points each, corner frame, label "FIGURE 1 · 78%".
4. **SLS intensity bar** (confidence 0–100%) and **Sensitivity** slider (threshold; higher = shows
   weaker, more doubtful shapes). Touch-and-hold protection like the other sliders.
5. **Detection ON/OFF**, front/back camera, torch, red "SCANNING" light whenever the camera is on.
6. **Record** video with the skeleton drawn in; **snapshot** button.
7. **History**: time, place (optional, typed), figures, peak intensity, thumbnail; play, rename,
   share, delete.
8. **Motion** indicator (works even with no figure).
9. **Combined EVP + SLS session**: when the radio is running, the SLS recording is linked to the
   radio recording made at the same time, and markers line up on one timeline in the Review deck.
10. **Optional alert sound** when a figure appears.

## 3. The key technical problem (found while planning)

Android only lets a web page use the camera if the page is a **secure origin** (https or
localhost). The console is loaded today from an HTML *string* (`about:blank` origin), where
`navigator.mediaDevices` does not exist. So the camera **cannot work** until the console is served
from a secure address.

**Planned fix (Android only):** serve the console page and its files through AndroidX
`WebViewAssetLoader` at `https://appassets.androidplatform.net/…` (a secure origin that never
leaves the phone), and grant the page's camera request in a `WebChromeClient.OnPermissionRequest`
after the normal Android CAMERA permission. This must be proven first — see Phase 0.

## 4. Architecture

**Chosen route — "Web route":**
- Camera: `getUserMedia` in the console page (WebView), rendered into a `<video>`.
- Detection: **MediaPipe Pose Landmarker (Tasks Vision, Apache-2.0)**, "lite" model, running on
  the phone (WASM + GPU delegate when available), **offline**: WASM files and the
  `pose_landmarker_lite.task` model are bundled in the APK (about 8–12 MB).
- Drawing: one `<canvas>` over the video (filters via canvas/CSS, skeleton, labels, bars).
- Recording: `canvas.captureStream()` + `MediaRecorder` → WebM video saved through a new
  `whispers://` command to the app's folder (`…/SLS/`), plus JSON metadata.
- Why: the same page works later on Windows (WebView2) and Linux (WebKitGTK) with a webcam,
  and it fits how the whole app is already built.

**Fallback route — "Native route"** (only if Phase 0 shows the web route is too slow on the
Samsung A05s): CameraX + Google ML Kit Pose Detection in C# (Xamarin bindings), drawing in a
native overlay on top of the WebView, landmarks sent to the page as JSON. Faster, but Android-only
and more work.

**Decision rule:** web route if Phase 0 reaches **≥ 10 detections per second** on the A05s with
the lite model at 256-px input; otherwise native route.

## 5. Files (planned)

| File | Purpose | Platform |
| --- | --- | --- |
| `Ghostbox.App/WebConsole/sls-camera.js` | SLS screen: camera, detector, overlay, controls, recording, history UI | shared page code, enabled on Android first |
| `Ghostbox.App/WebConsole/sls/` | MediaPipe WASM files + `pose_landmarker_lite.task` | bundled (Android first) |
| `Ghostbox.App/Platforms/Android/SlsWebHost.cs` | WebViewAssetLoader (secure origin) + camera permission bridge | Android |
| `Ghostbox.App/Platforms/Android/AndroidManifest.xml` | CAMERA permission, `uses-feature camera` (not required) | Android |
| `Ghostbox.App/SlsStore.cs` | save videos/snapshots/metadata, list History, link to radio recordings | shared C# |
| `Ghostbox.App/WebConsole/phone-i18n.js` | new SLS texts in 6 languages | Android |
| `THIRD-PARTY.md`, `Licenses/` | MediaPipe Apache-2.0 notice | all |

Page ↔ app messages (new `whispers://` commands): `slssave` (video/snapshot chunks, base64),
`slslist`, `slsdelete`, `slsrename`, `slslink` (link to current radio recording).

History metadata (`SLS/<time>.json`): `{ time, place, durationS, figuresMax, peak, video, snapshot,
radioRecording?, markers: [{t, note}] }`.

## 6. Phases, with "done" checks

| Phase | Work | Done when |
| --- | --- | --- |
| **0 — Proof (spike)** | Secure origin for the console on Android; camera in the page; MediaPipe lite running offline | On the A05s: camera picture shows in the app, a skeleton follows a person, ≥ 10 detections/s, app works offline, Windows/Linux builds unchanged |
| **1 — Camera screen** | SLS tab, filters, skeleton, intensity, sensitivity, ON/OFF, front/back, torch | Owner can use it on the phone; all 6 languages; all skins |
| **2 — Record + History** | Video with overlay, snapshots, History list (play/rename/share/delete) | A 1-minute recording saves, plays back and shares; History survives an app restart |
| **3 — Session features** | Motion, several figures, alert sound, link with radio recordings and Review-deck markers | A combined session shows radio + SLS markers on one timeline |
| **4 — Desktop** | Same SLS page on Windows and Linux with a webcam | Works with a USB webcam on both |

Each phase: tests on the Samsung Galaxy A05s, `dotnet run --project Ghostbox.Checks` passes,
Windows/Linux still build, then a GitHub release (2.2, 2.3, …) after the owner says yes.

## 7. Test plan

- **Speed:** detections per second and battery drop over 10 minutes (A05s).
- **True positives:** a person standing, sitting, partly hidden, in dim light.
- **False positives (to show honestly):** coat on a hook, chair, shadows, dark empty room.
- **Permissions:** first-time camera prompt, "deny" (friendly message), revoking in Android settings.
- **Offline:** airplane mode — detection still works.
- **No regressions:** radio, recording, Review deck, languages, skins, swipe pages unchanged.

## 8. Risks

| Risk | Plan |
| --- | --- |
| Camera blocked in WebView (secure origin) | Phase 0 solves it first; fallback = native route |
| Too slow on a mid-range phone | lite model, 256-px input, skip frames, GPU delegate; fallback = native ML Kit |
| APK grows ~10 MB | acceptable; model downloaded once inside the APK, no internet needed |
| Recording with the radio sound inside the video | radio sound is played natively, not in the page: keep separate files linked in History (simpler, reliable) |
| False figures cause wrong beliefs | honesty note on screen, in History and Disclaimer |
| Privacy | camera on only while SLS is open, red light, nothing uploaded, consent reminder |

## 9. Owner's extra wishes (2026-11-11)

- **Radio + camera together**: when a figure is detected while the radio runs, put an automatic marker
  on the radio recording ("SLS figure 78%") so it lines up in the Review deck (Phase 3).
- **"Heat" picture**: a phone camera cannot see heat. Offer a *Thermal-style* colour filter clearly
  labelled as a colour effect; real heat would need a plug-in thermal camera (InfiRay P2 / FLIR One) — possible extra later.
- **Settings button**: "radar scanner" design (round radar with turning sweep and a figure), chosen by the owner.

## 10. Designs

Three phone screens were drawn on 2026-11-11: (1) live SLS camera, classic green; (2) recording
together with the radio, Ember skin, two figures, frequency on screen; (3) History with the honesty
note. The tab bar becomes LISTEN · FX · REVIEW · SLS · ⚙.
