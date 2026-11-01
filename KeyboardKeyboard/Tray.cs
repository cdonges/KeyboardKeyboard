using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace KeyboardKeyboard;

/// <summary>
/// The app's icon in the notification area (the menu bar on macOS). Its menu pauses/resumes playing
/// or exits, clicking the icon also toggles pause on Windows and Linux, and the tooltip shows the
/// current instrument.
/// </summary>
internal sealed class Tray : IDisposable
{
    // Windows scales tray icons down from 32 px cleanly; the macOS menu bar wants a larger, sharper image.
    private static readonly int IconSize = OperatingSystem.IsWindows() ? 32 : 64;

    private readonly Application application;
    private readonly KeyboardPlayer player;
    private readonly WindowIcon playingIcon = IconArt.CreateIcon(IconSize, paused: false);
    private readonly WindowIcon pausedIcon = IconArt.CreateIcon(IconSize, paused: true);
    private readonly NativeMenuItem pauseItem = new("Pause");
    private readonly TrayIcon trayIcon;

    public Tray(Application application, KeyboardPlayer player, Action exit)
    {
        this.application = application;
        this.player = player;
        player.InstrumentChanged += (_, _) => Dispatcher.UIThread.Post(UpdateState);

        pauseItem.Click += (_, _) => TogglePaused();
        var exitItem = new NativeMenuItem(OperatingSystem.IsMacOS() ? "Quit" : "Exit");
        exitItem.Click += (_, _) => exit();

        trayIcon = new TrayIcon { Menu = [pauseItem, new NativeMenuItemSeparator(), exitItem] };
        trayIcon.Clicked += (_, _) => TogglePaused();

        UpdateState();

        // A tray icon only appears once it's attached to the application.
        TrayIcon.SetIcons(application, [trayIcon]);
    }

    public void Dispose()
    {
        TrayIcon.SetIcons(application, null);
        trayIcon.Dispose();
    }

    private void TogglePaused()
    {
        player.IsPaused = !player.IsPaused;
        UpdateState();
    }

    private void UpdateState()
    {
        pauseItem.Header = player.IsPaused ? "Resume" : "Pause";
        trayIcon.Icon = player.IsPaused ? pausedIcon : playingIcon;
        var instrument = ToWords(player.Instrument.ToString());
        trayIcon.ToolTipText = player.IsPaused ? $"{App.AppName}\n{instrument} (paused)" : $"{App.AppName}\n{instrument}";
    }

    /// <summary>Splits an enum name into words, e.g. "SynthBass1" becomes "Synth Bass 1".</summary>
    private static string ToWords(string name) =>
        Regex.Replace(name, "(?<=[a-z])(?=[A-Z0-9])|(?<=[0-9])(?=[A-Z])", " ");
}
