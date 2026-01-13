## Utility Platform (PowerToys-like host)

Windows system-tray host for small utilities (“PowerToys”) built on .NET.

### Features
- **System tray app**: runs without a main window, accessible from the tray icon.
- **Settings window**: lists installed PowerToys with an enable/disable toggle and per-toy settings.
- **Extensible**: PowerToys implement `UtilityPlatform.Core.PowerToys.IPowerToy` and are discovered automatically at runtime.

### Included PowerToys
- **Paste to Image**: when enabled, pressing **Ctrl+V** in File Explorer with an image in the clipboard creates a numbered PNG file in the current folder (e.g., `clipboard-image-1.png`, `clipboard-image-2.png`, ...).

### Dev notes
- The app targets `net9.0-windows` and uses WinForms + `NotifyIcon` for the tray experience.
- Settings are stored in `%AppData%\\UtilityPlatform\\settings.json`.

