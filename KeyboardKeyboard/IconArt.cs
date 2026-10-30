using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace KeyboardKeyboard;

/// <summary>
/// Draws the app icon: a computer keycap with piano keys on its face.
/// Used for the tray icon at runtime, and by tools/make-icon.cs to generate app.ico.
/// </summary>
public static class IconArt
{
    // The artwork is laid out on a 32 x 32 grid and scaled to the requested size.
    private const float Grid = 32;

    private static readonly Color PlayingSide = Color.FromArgb(0, 84, 153);
    private static readonly Color PlayingFace = Color.FromArgb(0, 120, 212);
    private static readonly Color PausedSide = Color.FromArgb(90, 90, 90);
    private static readonly Color PausedFace = Color.FromArgb(138, 138, 138);
    private static readonly Color BlackKey = Color.FromArgb(30, 30, 30);

    public static Bitmap Draw(int size, bool paused)
    {
        var bitmap = new Bitmap(size, size);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.ScaleTransform(size / Grid, size / Grid);

        using var side = new SolidBrush(paused ? PausedSide : PlayingSide);
        using var face = new SolidBrush(paused ? PausedFace : PlayingFace);
        using var black = new SolidBrush(BlackKey);

        // Keycap: the darker side shows below the face, like a key seen from slightly above.
        graphics.FillRoundedRectangle(side, new RectangleF(1, 1, 30, 30), new SizeF(12, 12));
        graphics.FillRoundedRectangle(face, new RectangleF(3, 2.5f, 26, 23), new SizeF(9, 9));

        // Piano: three white keys with two black keys between them.
        var keys = new RectangleF(6.5f, 6, 19, 16);
        var whiteWidth = keys.Width / 3;
        graphics.FillRoundedRectangle(Brushes.White, keys, new SizeF(3, 3));
        for (var i = 1; i < 3; i++)
        {
            var x = keys.X + whiteWidth * i;
            graphics.FillRectangle(black, x - 0.75f, keys.Y, 1.5f, keys.Height);
            graphics.FillRectangle(black, x - 2.25f, keys.Y, 4.5f, keys.Height * 0.6f);
        }

        return bitmap;
    }

    public static Icon CreateIcon(int size, bool paused)
    {
        using var bitmap = Draw(size, paused);
        var handle = bitmap.GetHicon();
        try
        {
            using var icon = Icon.FromHandle(handle);
            return (Icon)icon.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr handle);
}
