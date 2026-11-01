# KeyboardKeyboard
Turns your computer keyboard into a musical keyboard. Runs on Windows, macOS and Linux.

## Getting started

Download the file for your computer from the [latest release](https://github.com/cdonges/KeyboardKeyboard/releases/latest). Nothing else needs installing: .NET and the instrument sounds are built in.

| System | Download |
|---|---|
| Windows (x64) | `KeyboardKeyboard.exe` |
| macOS (Apple Silicon) | `KeyboardKeyboard-osx-arm64.zip` |
| macOS (Intel) | `KeyboardKeyboard-osx-x64.zip` |
| Linux (x64) | `KeyboardKeyboard-linux-x64.tar.gz` |

Run it. It sits in the notification area (the menu bar on macOS), and everything you type plays music until you pause or exit it.

### Windows

Run `KeyboardKeyboard.exe`. Windows 11 may tuck the icon into the hidden icons behind the **^** in the taskbar.

### macOS

1. Unzip the download and move `KeyboardKeyboard.app` to Applications.
2. Open it. The app isn't notarised by Apple, so the first time macOS blocks it: go to **System Settings > Privacy & Security**, scroll down and click **Open Anyway**.
3. KeyboardKeyboard asks for **Accessibility** permission, which macOS requires before an app can see what you type. Turn it on in **System Settings > Privacy & Security > Accessibility**, then open KeyboardKeyboard again.

After updating to a new version, macOS may ask for Accessibility permission again.

### Linux

```
tar -xzf KeyboardKeyboard-linux-x64.tar.gz
./KeyboardKeyboard
```

- On an **X11** session it works as is.
- On **Wayland**, apps can't normally see what you type in other apps, so KeyboardKeyboard reads the keyboard device directly. That needs you to be in the `input` group: run `sudo usermod -aG input $USER`, then log out and back in. Wayland also only tells it which key was pressed, not which character was typed, so keys play as on a US layout.
- The tray icon needs a desktop with tray (StatusNotifier) support. KDE and most others have it; on GNOME, install the AppIndicator extension.

## Playing

- The bottom two rows of letters are white keys.
- The top row of letters is black keys.
- The number keys are drums.
- Type the name of a General MIDI instrument (e.g. `violin`, `trumpet`, `pad1`) to switch to it.

Use the tray icon's menu to pause/resume or exit. On Windows and Linux, clicking the icon also toggles pause. Hover over it to see the current instrument.

## Building from source

Requires the .NET 10 SDK. To run it from the repo root:

```
dotnet run --project KeyboardKeyboard
```

The first build downloads the GeneralUser GS SoundFont (32 MB) into `KeyboardKeyboard/obj/soundfont/` and embeds it in the app.

### Publishing

To build a single file you can copy to another machine, run this from the repo root (or the **publish** task in VS Code):

```
dotnet publish KeyboardKeyboard -p:PublishProfile=SingleFile
```

It builds for the OS you're on; add `-r win-x64`, `-r osx-arm64`, `-r osx-x64` or `-r linux-x64` to choose another. The file is written to `publish/` and includes .NET, so the other machine doesn't need it installed. To turn a macOS build into a signed `KeyboardKeyboard.app`, run [packaging/package.sh](packaging/package.sh) on a Mac.

### Releases

GitHub Actions builds every platform on every push and pull request to `master`. To make a release, push a version tag:

```
git tag v1.0.0
git push origin v1.0.0
```

The workflow publishes each platform's download (stamped with that version) and attaches them all to a new GitHub release.

### Icons

The app icon is drawn in code ([IconArt.cs](KeyboardKeyboard/IconArt.cs)). After changing it, regenerate `app.ico` and `app.icns`:

```
dotnet run tools/make-icon.cs
```

## Credits

Instrument sounds are from [GeneralUser GS](https://github.com/mrbumpy409/GeneralUser-GS) by S. Christian Collins.
