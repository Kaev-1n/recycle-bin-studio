using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace RecycleBinStudio.Images;

internal sealed record BuiltInPreset(string Name, string Description, PreparedIcon Empty, PreparedIcon Full);

internal static class BuiltInPresets
{
    public static IReadOnlyList<BuiltInPreset> Load() =>
    [
        Load("portrait", "Портретная корзина", "Портрет на классической корзине Windows."),
        Load("pop-cat", "Pop Cat", "Открытый рот, когда пусто. Довольный кот, когда корзина полна.")
    ];

    private static BuiltInPreset Load(string key, string title, string description) =>
        new(title, description, Read($"{key}-empty"), Read($"{key}-full"));

    private static PreparedIcon Read(string name)
    {
        var uri = new Uri($"pack://application:,,,/Assets/Presets/{name}.ico");
        using var source = Application.GetResourceStream(uri).Stream;
        using var stream = new MemoryStream();
        source.CopyTo(stream);
        stream.Position = 0;
        var decoder = new IconBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        var preview = new WriteableBitmap(decoder.Frames.MinBy(frame => Math.Abs(frame.PixelWidth - 96))!);
        preview.Freeze();
        return new(name + ".ico", preview, stream.ToArray());
    }
}
