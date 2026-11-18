using System.IO;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RecycleBinStudio.Images;

internal sealed record PreparedIcon(string Name, BitmapSource Preview, byte[] Bytes)
{
    public string Save()
    {
        string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RecycleBinStudio", "Icons");
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, Convert.ToHexString(SHA256.HashData(Bytes)) + ".ico");
        string temporary = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".tmp");

        try
        {
            File.WriteAllBytes(temporary, Bytes);
            File.Move(temporary, path, overwrite: true);
            return path;
        }
        finally
        {
            File.Delete(temporary);
        }
    }
}

internal static class IconFile
{
    private static readonly int[] Sizes = [16, 20, 24, 32, 40, 48, 64, 96, 128, 256];

    public static PreparedIcon Load(string path)
    {
        using var input = File.OpenRead(path);
        return Load(input, Path.GetFileName(path));
    }

    public static PreparedIcon Load(Stream input, string name)
    {
        var decoder = BitmapDecoder.Create(input, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        if (decoder is not (IconBitmapDecoder or PngBitmapDecoder or JpegBitmapDecoder))
            throw new InvalidDataException("Выберите файл ICO, PNG или JPG.");

        var frames = decoder.Frames.OrderByDescending(frame => frame.PixelWidth * (long)frame.PixelHeight)
            .ThenByDescending(frame => frame.Format.BitsPerPixel).ToArray();
        return Encode(frames, name);
    }

    public static PreparedIcon Encode(BitmapSource source, string name) => Encode([source], name);

    private static PreparedIcon Encode(BitmapSource[] frames, string name)
    {
        using var output = new MemoryStream();
        using var writer = new BinaryWriter(output);
        writer.Write((ushort)0);
        writer.Write((ushort)1);
        writer.Write((ushort)Sizes.Length);

        int offset = 6 + 16 * Sizes.Length;
        var encoded = new List<byte[]>();
        BitmapSource? preview = null;

        foreach (int size in Sizes)
        {
            var original = frames.FirstOrDefault(frame => frame.PixelWidth == size && frame.PixelHeight == size)
                ?? frames[0];
            var scaled = Render(original, size);
            if (size == Sizes[^1])
                preview = scaled;

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(scaled));
            using var frameStream = new MemoryStream();
            encoder.Save(frameStream);
            byte[] png = frameStream.ToArray();

            byte dimension = size == 256 ? (byte)0 : (byte)size;
            writer.Write(dimension);
            writer.Write(dimension);
            writer.Write((ushort)0);
            writer.Write((ushort)1);
            writer.Write((ushort)32);
            writer.Write(png.Length);
            writer.Write(offset);
            offset += png.Length;
            encoded.Add(png);
        }

        foreach (byte[] frame in encoded)
            writer.Write(frame);

        writer.Flush();
        return new(name, preview!, output.ToArray());
    }

    public static BitmapSource Render(BitmapSource source, int size)
    {
        double scale = Math.Min((double)size / source.PixelWidth, (double)size / source.PixelHeight);
        double width = source.PixelWidth * scale;
        double height = source.PixelHeight * scale;
        var visual = new DrawingVisual();
        RenderOptions.SetBitmapScalingMode(visual, BitmapScalingMode.HighQuality);
        using (var drawing = visual.RenderOpen())
            drawing.DrawImage(source, new Rect((size - width) / 2, (size - height) / 2, width, height));

        var result = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        result.Render(visual);
        result.Freeze();
        return result;
    }
}
