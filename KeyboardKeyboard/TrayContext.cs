using System.Text.RegularExpressions;

namespace KeyboardKeyboard;

/// <summary>
/// Runs the app from the notification area. The tray menu pauses/resumes playing or exits,
/// double-clicking the icon also toggles pause, and the tooltip shows the current instrument.
/// </summary>
internal sealed class TrayContext : ApplicationContext
{
    private const string AppName = "KeyboardKeyboard";

    private readonly KeyboardPlayer player;
    private readonly Icon playingIcon = IconArt.CreateIcon(SystemInformation.SmallIconSize.Width, paused: false);
    private readonly Icon pausedIcon = IconArt.CreateIcon(SystemInformation.SmallIconSize.Width, paused: true);
    private readonly ContextMenuStrip menu = new();
    private readonly ToolStripMenuItem pauseItem;
    private readonly NotifyIcon trayIcon;

    public TrayContext(KeyboardPlayer player)
    {
        this.player = player;
        player.InstrumentChanged += (_, _) => UpdateState();

        pauseItem = new ToolStripMenuItem("Pause", null, (_, _) => TogglePaused());
        menu.Items.Add(pauseItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("Exit", null, (_, _) => ExitThread()));

        trayIcon = new NotifyIcon { ContextMenuStrip = menu, Visible = true };
        trayIcon.MouseDoubleClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                TogglePaused();
            }
        };

        UpdateState();
    }

    protected override void ExitThreadCore()
    {
        // Hide the icon straight away, otherwise it lingers in the tray until the mouse passes over it.
        trayIcon.Visible = false;
        base.ExitThreadCore();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            trayIcon.Dispose();
            menu.Dispose();
            playingIcon.Dispose();
            pausedIcon.Dispose();
        }

        base.Dispose(disposing);
    }

    private void TogglePaused()
    {
        player.IsPaused = !player.IsPaused;
        UpdateState();
    }

    private void UpdateState()
    {
        pauseItem.Text = player.IsPaused ? "Resume" : "Pause";
        trayIcon.Icon = player.IsPaused ? pausedIcon : playingIcon;
        var instrument = ToWords(player.Instrument.ToString());
        trayIcon.Text = player.IsPaused ? $"{AppName}\n{instrument} (paused)" : $"{AppName}\n{instrument}";
    }

    /// <summary>Splits an enum name into words, e.g. "SynthBass1" becomes "Synth Bass 1".</summary>
    private static string ToWords(string name) =>
        Regex.Replace(name, "(?<=[a-z])(?=[A-Z0-9])|(?<=[0-9])(?=[A-Z])", " ");
}
