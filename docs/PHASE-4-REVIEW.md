# Phase 4 — investigation workspace

Implemented audio import/copy, sample-derived waveform envelope and time-aligned spectrogram overview, numerical or two-click time selection, zoom to range/full view, range loop, annotated markers with processing notes, separate Original/Processed preview, selected/full WAV exports with JSON processing recipes, and named investigation sessions.

Session files preserve original path and SHA-256, selection, effect enable states/values, pitch/reverse/channel choices, noise-profile source/range and marker notes. Reopening verifies the original hash before restoring. A live GUI session was saved and reopened successfully; marker content was checked in its JSON. Processing settings changes invalidate an old preview. Comparison changes stop existing review playback so it cannot silently keep playing the wrong version. Exports are always new files.

Import, captured-take loading, actual Apply Preview, playback/pause activation, marker creation, session save and source-hash-verified reopen were exercised in the Windows UI. A captured 15-second receiver clip produced a preview with measured 0.000% clipped samples. Review and processing are independent of tuning; audible review playback requires stopping live monitoring to avoid mixing two sources.

Changed files: new ReviewWorkspace.cs, SettingsJsonContext.cs and retained RecordingsPanel/AudioVisualization integration. Existing recording, conversation and speech controls remain under a disclosed Recording tools section.

Limits: no blind source separation; no automatic assertion of spoken words; no destructive edit operations; sessions reference originals in the development data folder and are not portable audio bundles. Loop and pause still need extended listening checks. The graph is a bounded overview, not a sample-accurate multitrack editor. Process selection exports that range; it does not splice a speed-altered range back into an otherwise unchanged full recording. Advanced controls are in their relevant tabs; the legacy tools are collapsible.
