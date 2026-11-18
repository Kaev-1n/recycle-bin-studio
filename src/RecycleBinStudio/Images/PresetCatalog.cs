using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Media.Imaging;

namespace RecycleBinStudio.Images;

internal sealed record PresetPair(PreparedIcon Empty, PreparedIcon Full);

internal sealed class CatalogPreset(string key, string name, string description, PresetPair? pair = null)
    : INotifyPropertyChanged
{
    public string Key { get; } = key;
    public string Name { get; } = name;
    public string Description { get; } = description;
    public PresetPair? Pair { get; private set; } = pair;
    public bool IsReady => Pair is not null;
    public string Status { get; private set; } = pair is null ? "Ожидает загрузки" : "";
    public string? Error { get; private set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Update(PresetPair? value, string status = "", string? error = null)
    {
        Pair = value;
        Status = status;
        Error = error;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
    }
}

internal sealed class PresetCatalog
{
    private const string Revision = "06cdeeae2c405c3580ffe37633ee8a84f12018b6";
    private const string Source = "https://raw.githubusercontent.com/sdushantha/recycle-bin-themes/" + Revision + "/themes/";
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(20) };
    private static readonly string CacheDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "RecycleBinStudio", "Catalog", Revision);
    private Task? refresh;

    public IReadOnlyList<CatalogPreset> Items { get; } =
    [
        BuiltIn("portrait", "Портретная корзина", "Портрет на классической корзине Windows."),
        BuiltIn("pop-cat", "Pop Cat", "Открытый рот, когда пусто. Довольный кот, когда корзина полна."),
        new("patrick-star", "Patrick Star", "Две версии Патрика из «Губки Боба»."),
        new("kirby", "Kirby", "Две реакции Кирби."),
        new("sword-kirby", "Sword Kirby", "Вариант Кирби с мечом."),
        new("kanna", "Kanna", "Канна из «Дракона-горничной госпожи Кобаяси»."),
        new("french-fries", "French Fries", "Пустая и полная упаковка картофеля фри."),
        new("minecraft-chest", "Minecraft Chest", "Два состояния сундука Minecraft.")
    ];

    public Task LoadAsync() => refresh is { IsCompleted: false }
        ? refresh
        : refresh = Task.WhenAll(Items.Where(item => !item.IsReady).Select(LoadPairAsync));

    private static async Task LoadPairAsync(CatalogPreset item)
    {
        item.Update(null, "Загружается…");
        try
        {
            var pair = await Task.Run(async () =>
            {
                var empty = LoadIconAsync(item.Key, "empty");
                var full = LoadIconAsync(item.Key, "full");
                await Task.WhenAll(empty, full);
                return new PresetPair(await empty, await full);
            });
            item.Update(pair);
        }
        catch (Exception error)
        {
            item.Update(null, "Не удалось загрузить", error.Message);
        }
    }

    private static async Task<PreparedIcon> LoadIconAsync(string key, string state)
    {
        string name = $"{key}-{state}.ico";
        string path = Path.Combine(CacheDirectory, name);
        if (File.Exists(path))
        {
            byte[] cached = await File.ReadAllBytesAsync(path);
            try
            {
                return Decode(name, cached);
            }
            catch (Exception error) when (error is FormatException or NotSupportedException or InvalidDataException)
            {
                // A damaged cache entry can be replaced by the pinned copy.
            }
        }

        byte[] bytes = await Client.GetByteArrayAsync($"{Source}{key}/{name}");
        var icon = Decode(name, bytes);
        Directory.CreateDirectory(CacheDirectory);
        string pending = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllBytesAsync(pending, bytes);
            File.Move(pending, path, overwrite: true);
        }
        finally
        {
            File.Delete(pending);
        }

        return icon;
    }

    private static CatalogPreset BuiltIn(string key, string name, string description) =>
        new(key, name, description, new(ReadResource(key, "empty"), ReadResource(key, "full")));

    private static PreparedIcon ReadResource(string key, string state)
    {
        string name = $"{key}-{state}.ico";
        var uri = new Uri($"pack://application:,,,/RecycleBinStudio;component/Assets/Presets/{name}");
        using var source = Application.GetResourceStream(uri).Stream;
        using var stream = new MemoryStream();
        source.CopyTo(stream);
        return Decode(name, stream.ToArray());
    }

    internal static PreparedIcon Decode(string name, byte[] bytes)
    {
        using var stream = new MemoryStream(bytes, writable: false);
        var decoder = new IconBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        var frame = decoder.Frames.MinBy(frame => Math.Abs(frame.PixelWidth - 96))
            ?? throw new InvalidDataException("В ICO нет изображений.");
        var preview = new WriteableBitmap(frame);
        preview.Freeze();
        return new(name, preview, bytes);
    }
}
