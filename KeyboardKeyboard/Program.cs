using Avalonia;
using Avalonia.Controls;

namespace KeyboardKeyboard;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        // A second copy would play every note twice.
        using var singleInstance = new Mutex(true, $@"Local\{App.AppName}", out var isFirstInstance);
        App.IsFirstInstance = isFirstInstance;

        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .With(new MacOSPlatformOptions { ShowInDock = false })
            .StartWithClassicDesktopLifetime(args, ShutdownMode.OnExplicitShutdown);
    }
}
