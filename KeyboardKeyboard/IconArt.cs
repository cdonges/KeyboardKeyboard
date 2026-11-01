using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace KeyboardKeyboard;

/// <summary>
/// Draws the app icon: a computer keycap with piano keys on its face.
/// Used for the tray icon at runtime, and by tools/make-icon.cs to generate app.ico and app.icns.
/// </summary>
public static class IconArt
{
    // The artwork is laid out on a 32 x 32 grid and scaled to the requested size.
    private const double Grid = 32;

    private static readonly IBrush PlayingSide = new SolidColorBrush(Color.FromRgb(0, 84, 153));
    private static readonly IBrush PlayingFace = new SolidColorBrush(Color.FromRgb(0, 120, 212));
    private static readonly IBrush PausedSide = new SolidColorBrush(Color.FromRgb(90, 90, 90));
    private static readonly IBrush PausedFace = new SolidColorBrush(Color.FromRgb(138, 138, 138));
    private static readonly IBrush BlackKey = new SolidColorBrush(Color.FromRgb(30, 30, 30));

    public static Bitmap Draw(int size, bool paused)
    {
        var bitmap = new RenderTargetBitmap(new PixelSize(size, size));
        using (var context = bitmap.CreateDrawingContext())
        using (context.PushTransform(Matrix.CreateScale(size / Grid, size / Grid)))
        {
            // Keycap: the darker side shows below the face, like a key seen from slightly above.
            context.DrawRectangle(paused ? PausedSide : PlayingSide, null, new RoundedRect(new Rect(1, 1, 30, 30), 6));
            context.DrawRectangle(paused ? PausedFace : PlayingFace, null, new RoundedRect(new Rect(3, 2.5, 26, 23), 4.5));

            // Piano: three white keys with two black keys between them.
            var keys = new Rect(6.5, 6, 19, 16);
            var whiteWidth = keys.Width / 3;
            context.DrawRectangle(Brushes.White, null, new RoundedRect(keys, 1.5));
            for (var i = 1; i < 3; i++)
            {
                var x = keys.X + whiteWidth * i;
                context.DrawRectangle(BlackKey, null, new Rect(x - 0.75, keys.Y, 1.5, keys.Height));
                context.DrawRectangle(BlackKey, null, new Rect(x - 2.25, keys.Y, 4.5, keys.Height * 0.6));
            }
        }

        return bitmap;
    }

    public static WindowIcon CreateIcon(int size, bool paused)
    {
        using var bitmap = Draw(size, paused);
        using var png = new MemoryStream();
        bitmap.Save(png, PngBitmapEncoderOptions.Default);
        png.Position = 0;
        return new WindowIcon(png);
    }
}
