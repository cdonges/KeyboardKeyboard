using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;
using SharpHook;
using SharpHook.Data;
using SharpHook.Providers;

namespace KeyboardKeyboard;

/// <summary>
/// Starts the synth, the keyboard listener and the tray icon, and explains what went wrong if any
/// of them can't start. The app has no main window; it exits from the tray menu.
/// </summary>
internal sealed class App : Application
{
    public const string AppName = "KeyboardKeyboard";

    /// <summary>False when another copy is already running, in which case this one just says so and exits.</summary>
    public static bool IsFirstInstance { get; set; } = true;

    public override void Initialize()
    {
        Name = AppName;
        Styles.Add(new FluentTheme());
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var lifetime = (IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;
        _ = RunAsync(lifetime);
        base.OnFrameworkInitializationCompleted();
    }

    private static async Task RunAsync(IClassicDesktopStyleApplicationLifetime lifetime)
    {
        if (!IsFirstInstance)
        {
            await MessageBox.ShowAsync($"{AppName} is already running in the {TrayName}.");
            lifetime.Shutdown();
            return;
        }

        // macOS only lets apps with Accessibility permission see key presses. This asks for it if needed.
        if (!UioHookProvider.Instance.IsAxApiEnabled(promptUserIfDisabled: true))
        {
            await MessageBox.ShowAsync(
                $"{AppName} needs Accessibility permission to hear what you type.\n\n" +
                $"Turn on {AppName} in System Settings > Privacy & Security > Accessibility, then start it again.");
            lifetime.Shutdown();
            return;
        }

        Synth synth;
        try
        {
            synth = new Synth();
        }
        catch (Exception e)
        {
            await MessageBox.ShowAsync($"{AppName} couldn't start playing sound.\n\n{e.Message}");
            lifetime.Shutdown();
            return;
        }

        var player = new KeyboardPlayer(synth);
        var tray = new Tray(Current!, player, () => lifetime.Shutdown());
        lifetime.Exit += (_, _) =>
        {
            tray.Dispose();
            player.Dispose();
            synth.Dispose();
        };

        try
        {
            await player.RunAsync();
        }
        catch (HookException e)
        {
            tray.Dispose();
            await MessageBox.ShowAsync($"{AppName} can't listen to the keyboard.\n\n{DescribeHookError(e.Result)}");
            lifetime.Shutdown();
        }
    }

    private static string TrayName => OperatingSystem.IsMacOS() ? "menu bar" : "notification area";

    private static string DescribeHookError(UioHookResult result) => result switch
    {
        UioHookResult.ErrorAxApiDisabled or UioHookResult.ErrorAxApiRevoked =>
            $"Turn on {AppName} in System Settings > Privacy & Security > Accessibility, then start it again.",
        UioHookResult.ErrorLinuxInitUdev or UioHookResult.ErrorLinuxInitLibinput or UioHookResult.ErrorLinuxAssignSeat
            or UioHookResult.ErrorLinuxNoInputDevices =>
            "On Wayland, reading the keyboard needs access to /dev/input. Add yourself to the input group " +
            "(sudo usermod -aG input $USER), log out and back in, then start it again.",
        _ => $"The keyboard hook failed with {result}.",
    };
}
