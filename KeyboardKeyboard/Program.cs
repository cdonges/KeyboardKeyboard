using Melanchall.DryWetMidi.Multimedia;

namespace KeyboardKeyboard;

internal static class Program
{
    private const string AppName = "KeyboardKeyboard";

    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        // A second copy would play every note twice.
        using var singleInstance = new Mutex(true, $@"Local\{AppName}", out var isFirstInstance);
        if (!isFirstInstance)
        {
            MessageBox.Show($"{AppName} is already running in the notification area.", AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (OutputDevice.GetDevicesCount() == 0)
        {
            MessageBox.Show("No MIDI output device found.", AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        using var output = OutputDevice.GetByIndex(0);
        using var player = new KeyboardPlayer(output);
        using var tray = new TrayContext(player);

        Application.Run(tray);
    }
}
