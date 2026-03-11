# tray — Windows System Tray App

> WIP — not yet implemented.

A lightweight Windows system tray application for quick file/paste uploads.

## Planned features

- Lives in the system tray (minimal, no main window)
- Right-click menu to upload a file (opens file picker)
- Global hotkey to upload clipboard contents as a paste
- Expiry picker per upload
- Automatically copies the returned URL to clipboard
- Configurable: API key, server URL, default expiry
- Auto-start with Windows

## Likely tech

- **.NET / C# WinForms or WPF** — native, no runtime needed with self-contained publish, small binary
- Or **Rust + tray-icon crate** — to match rustypaste's ecosystem
