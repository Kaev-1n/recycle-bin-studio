using System.IO;
using System.Net.Http;

namespace RecycleBinStudio.Images;

internal static class PopCatPreset
{
    private const string Source = "https://raw.githubusercontent.com/sdushantha/recycle-bin-themes/refs/heads/main/themes/pop-cat/";
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(20) };

    public static async Task<(PreparedIcon Empty, PreparedIcon Full)> LoadAsync()
    {
        var empty = DownloadAsync("pop-cat-empty.ico");
        var full = DownloadAsync("pop-cat-full.ico");
        await Task.WhenAll(empty, full);
        return (await empty, await full);
    }

    private static async Task<PreparedIcon> DownloadAsync(string name)
    {
        byte[] bytes = await Client.GetByteArrayAsync(Source + name).ConfigureAwait(false);
        using var stream = new MemoryStream(bytes);
        return IconFile.Load(stream, name);
    }
}
