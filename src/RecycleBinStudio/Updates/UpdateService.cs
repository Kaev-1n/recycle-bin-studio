using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Win32;

namespace RecycleBinStudio.Updates;

internal sealed record AvailableUpdate(Version Version, Uri Download, string Sha256, long Size);

internal static class UpdateService
{
    private const string SettingsKey = @"Software\RecycleBinStudio";
    private const string Repository = "Kaev-1n/recycle-bin-studio";
    public const string ReleasesPage = "https://github.com/" + Repository + "/releases/latest";

    private static readonly HttpClient Client = new()
    {
        Timeout = TimeSpan.FromSeconds(30),
        DefaultRequestHeaders = { UserAgent = { new("RecycleBinStudio", "1.0") } }
    };

    public static Version CurrentVersion { get; } = typeof(UpdateService).Assembly.GetName().Version!;
    public static string VersionLabel => CurrentVersion.ToString(3);

    public static bool IsInstalled
    {
        get
        {
            using var settings = Registry.CurrentUser.OpenSubKey(SettingsKey);
            return settings?.GetValue("InstallLocation") is string location &&
                string.Equals(Path.TrimEndingDirectorySeparator(location),
                    Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory), StringComparison.OrdinalIgnoreCase);
        }
    }

    public static bool Automatic
    {
        get
        {
            using var settings = Registry.CurrentUser.OpenSubKey(SettingsKey);
            return settings?.GetValue("AutoUpdate") is 1;
        }
        set
        {
            using var settings = Registry.CurrentUser.CreateSubKey(SettingsKey);
            settings.SetValue("AutoUpdate", value ? 1 : 0, RegistryValueKind.DWord);
        }
    }

    public static async Task<AvailableUpdate?> CheckAsync()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{Repository}/releases/latest");
        request.Headers.Accept.ParseAdd("application/vnd.github+json");
        using var response = await Client.SendAsync(request).ConfigureAwait(false);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
        return Parse(json.RootElement, CurrentVersion, RuntimeInformation.ProcessArchitecture);
    }

    internal static AvailableUpdate? Parse(JsonElement release, Version installed, Architecture architecture)
    {
        if (release.GetProperty("draft").GetBoolean() || release.GetProperty("prerelease").GetBoolean())
            return null;

        string tag = release.GetProperty("tag_name").GetString() ?? "";
        if (!tag.StartsWith('v') || !Version.TryParse(tag[1..], out var version) ||
            version.Build < 0 || version <= new Version(installed.Major, installed.Minor, installed.Build))
            return null;

        string assetName = architecture switch
        {
            Architecture.X64 => "RecycleBinStudio-Setup-win-x64.exe",
            Architecture.Arm64 => "RecycleBinStudio-Setup-win-arm64.exe",
            _ => throw new PlatformNotSupportedException("Архитектура не поддерживается.")
        };
        foreach (var asset in release.GetProperty("assets").EnumerateArray())
        {
            if (asset.GetProperty("name").GetString() != assetName)
                continue;

            string expected = $"https://github.com/{Repository}/releases/download/{tag}/{assetName}";
            string? digest = asset.GetProperty("digest").GetString();
            long size = asset.GetProperty("size").GetInt64();
            if (asset.GetProperty("browser_download_url").GetString() != expected ||
                digest is null || !digest.StartsWith("sha256:", StringComparison.Ordinal) ||
                digest.Length != 71 || !digest[7..].All(Uri.IsHexDigit) || size <= 0)
                throw new InvalidDataException("У релиза нет проверяемого установщика.");

            return new(version, new Uri(expected), digest[7..], size);
        }

        throw new InvalidDataException("В релизе нет установщика для этой архитектуры.");
    }

    public static async Task<string> DownloadAsync(AvailableUpdate update)
    {
        string path = Path.Combine(Path.GetTempPath(), $"RecycleBinStudio-Update-{Guid.NewGuid():N}.exe");
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(5));
            using var response = await Client.GetAsync(update.Download, HttpCompletionOption.ResponseHeadersRead,
                timeout.Token).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            await using var file = new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None,
                bufferSize: 81920, useAsync: true);
            await using var input = await response.Content.ReadAsStreamAsync(timeout.Token).ConfigureAwait(false);
            byte[] buffer = new byte[81920];
            int read;
            while ((read = await input.ReadAsync(buffer.AsMemory(), timeout.Token).ConfigureAwait(false)) != 0)
            {
                if (file.Length + read > update.Size)
                    throw new InvalidDataException("Размер установщика превышает указанный в релизе.");

                await file.WriteAsync(buffer.AsMemory(0, read), timeout.Token).ConfigureAwait(false);
            }

            if (file.Length != update.Size)
                throw new InvalidDataException("Установщик скачан не полностью.");

            file.Position = 0;
            string hash = Convert.ToHexString(await SHA256.HashDataAsync(file, timeout.Token).ConfigureAwait(false));
            if (!hash.Equals(update.Sha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Контрольная сумма установщика не совпала.");

            return path;
        }
        catch
        {
            File.Delete(path);
            throw;
        }
    }

    public static void StartInstaller(string path)
    {
        using var installer = Process.Start(new ProcessStartInfo(path)
        {
            UseShellExecute = true,
            Arguments = "/SILENT /SUPPRESSMSGBOXES /NORESTART /RBSUPDATE=1 /LOG"
        }) ?? throw new IOException("Установщик не запустился.");
    }
}
