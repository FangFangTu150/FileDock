# FileDock

FileDock is a lightweight Windows dock for quickly reopening the files and folders you touched most recently.

It watches a folder you choose, sorts the contents by last modified time, and keeps the latest items in a compact WPF sidebar that stays close at hand without taking over your desktop.

## What It Does

- Monitors a target folder in real time
- Shows recent items in last-modified order
- Optionally includes folders in the dock
- Loads native Windows icons for files and directories
- Opens items with a double-click
- Supports right-click actions to open the item or reveal it in Explorer
- Copies a file or folder to the clipboard with one click
- Lives in the system tray and can be reopened from there
- Supports single-instance startup behavior
- Can pause watcher activity while a fullscreen app is active
- Persists window position, size, and the latest visible snapshot between sessions
- Supports launching automatically with Windows

## Why This Project Exists

Windows gives you recent files in a few scattered places, but not a simple always-available dock that reflects the contents of a working folder in real time. FileDock is built for people who bounce in and out of project outputs, documents, assets, and working directories all day and want faster access without keeping File Explorer open all the time.

## Tech Stack

- .NET 8
- WPF
- Windows Forms folder picker integration
- `Hardcodet.NotifyIcon.Wpf` for tray support

## Project Structure

```text
Assets/        App icons and branding assets
Models/        Settings and file entry models
Services/      File watching, settings persistence, fullscreen detection, icon loading
ViewModels/    Dock and settings view models plus command helpers
Views/         Dock window, settings window, and file card UI
```

## How It Works

1. On startup, FileDock loads saved settings from the user's AppData directory.
2. It restores the previous dock snapshot so the UI is populated immediately.
3. A timer-based watcher scans the configured folder and compares the current results with the last scan.
4. The dock updates only when the directory contents or timestamps change.
5. Icons are loaded asynchronously so the UI stays responsive.
6. When the app closes or hides, it saves settings and the current snapshot for the next launch.

## Running Locally

### Requirements

- Windows
- .NET 8 SDK

### Start In Development

```powershell
dotnet build
dotnet run
```

### Release Build

```powershell
dotnet build -c Release
```

## Usage

1. Launch FileDock.
2. Open the settings window.
3. Choose the folder you want FileDock to monitor.
4. Decide whether folders should appear alongside files.
5. Optionally enable startup with Windows.
6. Use the dock to open, locate, or copy recent items.

## Settings and Data

FileDock stores its local settings under the current user's AppData profile:

```text
%AppData%\FileDock\settings.json
```

The saved state includes:

- watched folder path
- whether folders should be shown
- startup preference
- dock size and position
- last visible item snapshot

## Current Focus

This repository currently centers on the desktop experience and the core file-watching workflow. Good next steps for the project could include:

- pinning favorite items
- filtering by file type
- custom sort modes
- keyboard navigation
- multi-folder support
- installer and packaged releases

## License

No license has been added yet. If you plan to share or reuse this project publicly, adding a license file would be a good next step.
