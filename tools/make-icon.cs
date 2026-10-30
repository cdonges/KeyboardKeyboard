// Regenerates KeyboardKeyboard/app.ico from IconArt, so the exe icon matches the tray icon.
// Run from the repo root: dotnet run tools/make-icon.cs
#:property TargetFramework=net10.0-windows
#:property UseWindowsForms=true
#:property PublishAot=false
#:property PublishTrimmed=false
#:project ../KeyboardKeyboard/KeyboardKeyboard.csproj

using System.Drawing.Imaging;
using KeyboardKeyboard;

int[] sizes = [16, 20, 24, 32, 40, 48, 64, 256];
var outputPath = Path.Combine("KeyboardKeyboard", "app.ico");

var images = sizes.Select(size =>
{
    using var bitmap = IconArt.Draw(size, paused: false);
    using var stream = new MemoryStream();
    bitmap.Save(stream, ImageFormat.Png);
    return stream.ToArray();
}).ToArray();

// ICO file: a header, one directory entry per size, then each image stored as a PNG.
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
