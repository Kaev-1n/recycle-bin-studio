using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RecycleBinStudio.Images;

internal static class PresetImage
{
    public static BitmapSource Load(string path)
    {
        using var input = File.OpenRead(path);
        var decoder = BitmapDecoder.Create(input, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        if (decoder is not (PngBitmapDecoder or JpegBitmapDecoder or IconBitmapDecoder))
            throw new InvalidDataException("Выберите PNG, JPG или ICO.");

        var frame = decoder.Frames.MaxBy(frame => frame.PixelWidth * (long)frame.PixelHeight)!;
        // Decoded frames retain their dispatcher-bound decoder, even when frozen.
        var pixels = new WriteableBitmap(frame);
        pixels.Freeze();
        return pixels;
    }

    public static (BitmapSource Left, BitmapSource Right) Split(BitmapSource source, double position)
    {
        int width = source.PixelWidth;
        int height = source.PixelHeight;
        if (width < 2)
            throw new InvalidDataException("Изображение слишком узкое для разреза.");

        int cut = Math.Clamp((int)Math.Round(width * position), 1, width - 1);
        var left = new CroppedBitmap(source, new Int32Rect(0, 0, cut, height));
        var right = new CroppedBitmap(source, new Int32Rect(cut, 0, width - cut, height));
        left.Freeze();
        right.Freeze();
        return (left, right);
    }

    public static BitmapSource PreviewSheet(BitmapSource source)
    {
        if (source.PixelWidth < 2)
            throw new InvalidDataException("Изображение слишком узкое для разделения.");

        const int maxPreviewSize = 768;
        int longestSide = Math.Max(source.PixelWidth, source.PixelHeight);
        if (longestSide <= maxPreviewSize)
            return source;

        double scale = (double)maxPreviewSize / longestSide;
        var reduced = new TransformedBitmap(source, new ScaleTransform(
            Math.Max(scale, 2.0 / source.PixelWidth), Math.Max(scale, 1.0 / source.PixelHeight)));
        var preview = new WriteableBitmap(reduced);
        preview.Freeze();
        return preview;
    }
}
