# KeyboardKeyboard
Turns your computer keyboard into a musical keyboard.

## Getting started

1. Install the **.NET Desktop Runtime 10** (Windows x64) from [dotnet.microsoft.com/download/dotnet/10.0](https://dotnet.microsoft.com/download/dotnet/10.0). If you already have the .NET 10 SDK, it's included.
2. Download `KeyboardKeyboard.exe` from the [latest release](https://github.com/cdonges/KeyboardKeyboard/releases/latest).
3. Run it. It sits in the notification area, and everything you type plays music until you pause or exit it.

KeyboardKeyboard runs on Windows only.

## Playing

- The bottom two rows of letters are white keys.
- The top row of letters is black keys.
- The number keys are drums.
- Type the name of a General MIDI instrument (e.g. `violin`, `trumpet`, `pad1`) to switch to it.

Right-click the tray icon to pause/resume or exit, or double-click it to toggle pause. Hover over it to see the current instrument.

## Building from source

Requires the .NET 10 SDK. To run it from the repo root:

```
dotnet run --project KeyboardKeyboard
```

### Publishing

To build a single `KeyboardKeyboard.exe` you can copy to another machine, run this from the repo root (or the **publish** task in VS Code):

```
dotnet publish KeyboardKeyboard -p:PublishProfile=SingleFile
```

The exe is written to `publish\`. The other machine needs the .NET 10 Desktop Runtime.

### Releases

GitHub Actions builds every push and pull request to `master`. To make a release, push a version tag:

```
git tag v1.0.0
git push origin v1.0.0
```

The workflow publishes `KeyboardKeyboard.exe` (stamped with that version) and attaches it to a new GitHub release.
