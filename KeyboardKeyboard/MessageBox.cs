using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace KeyboardKeyboard;

/// <summary>A minimal message box with an OK button, since Avalonia doesn't include one.</summary>
internal static class MessageBox
{
    public static Task ShowAsync(string message)
    {
        var ok = new Button { Content = "OK", IsDefault = true, MinWidth = 80, HorizontalContentAlignment = HorizontalAlignment.Center };
        var window = new Window
        {
            Title = App.AppName,
            Icon = IconArt.CreateIcon(64, paused: false),
            SizeToContent = SizeToContent.WidthAndHeight,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            Content = new StackPanel
            {
                Margin = new Thickness(24),
                Spacing = 20,
                Children =
                {
                    new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap, MaxWidth = 420 },
                    new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Children = { ok } },
                },
            },
        };

        var closed = new TaskCompletionSource();
        ok.Click += (_, _) => window.Close();
        window.Closed += (_, _) => closed.TrySetResult();
        window.Show();
        window.Activate();
        return closed.Task;
    }
}
