# Phase 2 — desktop HUD preview

Implemented only in the isolated development copy. Three persistent desktop columns, centered WHISPERS OF THE SPIRIT header, blue translucent panels, restrained procedural background, thin borders, sample-driven central waveform and circular red receiver recording control. No phone-style column collapse was introduced. Minimum window size is 1240×760; the visually checked window is 1600×1000 logical pixels on this display.

Five tabs separate Analysis, Cleanup, Voice, Effects and Review. The bottom transport has receiver Start/Stop, receiver Record, review Pause/Play and Save WAV. Pause applies to review playback, not RF tuning. Controls have labels/tooltips, switches, numerical slider values and resets. Disabled recording/playback states were inspected. The activity indicator only occupies space while processing in the final source.

The receiver, demodulator choices, range validation, band presets and source-specific advanced controls are reused. The reference is implemented as native interactive controls, not embedded as a screenshot. Synthetic demo and white-noise overlays remain labelled.

Changed files: Dashboard.cs, App.xaml.cs, MainPage.xaml.cs, Resources/Styles/Styles.xaml; new InvestigationControls.cs. The development project has its own app ID. Production files are unchanged.

Remaining acceptance: visual approval, smaller desktop/DPI combinations and a complete keyboard/control-state walkthrough. No design deployment approval has been assumed.
