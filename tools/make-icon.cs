// Regenerates KeyboardKeyboard/app.ico (Windows exe icon) and KeyboardKeyboard/app.icns (macOS app icon)
// from IconArt, so they match the tray icon.
// Run from the repo root: dotnet run tools/make-icon.cs
#:property TargetFramework=net10.0
#:property PublishAot=false
#:property PublishTrimmed=false
#:project ../KeyboardKeyboard/KeyboardKeyboard.csproj

using System.Buffers.Binary;
using System.Text;
using Avalonia;
using Avalonia.Media.Imaging;
using KeyboardKeyboard;

// Drawing needs Avalonia's renderer, but no window.
AppBuilder.Configure<Application>().UsePlatformDetect().SetupWithoutStarting();

byte[] Png(int size)
{
    using var bitmap = IconArt.Draw(size, paused: false);
    using var stream = new MemoryStream();
    bitmap.Save(stream, PngBitmapEncoderOptions.Default);
    return stream.ToArray();
}

WriteIco(Path.Combine("KeyboardKeyboard", "app.ico"), [16, 20, 24, 32, 40, 48, 64, 256]);
WriteIcns(Path.Combine("KeyboardKeyboard", "app.icns"),
[
    ("icp4", 16), ("icp5", 32), ("icp6", 64), ("ic07", 128), ("ic08", 256), ("ic09", 512),
    ("ic10", 1024), ("ic11", 32), ("ic12", 64), ("ic13", 256), ("ic14", 512),
]);

// ICO file: a header, one directory entry per size, then each image stored as a PNG.
void WriteIco(string outputPath, int[] sizes)
{
    var images = sizes.Select(Png).ToArray();

    using var writer = new BinaryWriter(File.Create(outputPath));
    writer.Write((short)0); // reserved
    writer.Write((short)1); // type: icon
    writer.Write((short)sizes.Length);

    var offset = 6 + 16 * sizes.Length;
    for (var i = 0; i < sizes.Length; i++)
    {
        writer.Write((byte)(sizes[i] % 256)); // width, 0 means 256
        writer.Write((byte)(sizes[i] % 256)); // height, 0 means 256
        writer.Write((byte)0);  // palette size
        writer.Write((byte)0);  // reserved
        writer.Write((short)1); // colour planes
        writer.Write((short)32); // bits per pixel
        writer.Write(images[i].Length);
        writer.Write(offset);
        offset += images[i].Length;
    }

    foreach (var image in images)
    {
        writer.Write(image);
    }

    Console.WriteLine($"Wrote {outputPath} ({string.Join(", ", sizes)} px)");
}

// ICNS file: "icns" and the total length, then one entry per image: a four-character type
// (which says the size and scale), the entry length and a PNG. Lengths are big-endian.
void WriteIcns(string outputPath, (string Type, int Size)[] entries)
{
    var images = entries.Select(entry => (entry.Type, Png: Png(entry.Size))).ToArray();

    using var writer = new BinaryWriter(File.Create(outputPath));
    writer.Write(Encoding.ASCII.GetBytes("icns"));
    writer.Write(BinaryPrimitives.ReverseEndianness(8 + images.Sum(image => 8 + image.Png.Length)));

    foreach (var (type, png) in images)
    {
        writer.Write(Encoding.ASCII.GetBytes(type));
        writer.Write(BinaryPrimitives.ReverseEndianness(8 + png.Length));
        writer.Write(png);
    }

    Console.WriteLine($"Wrote {outputPath} ({string.Join(", ", entries.Select(entry => entry.Size).Distinct())} px)");
}
