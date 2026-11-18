using System.IO;
using System.Text.Json;

namespace RecycleBinStudio.Images;

internal sealed record LocalPreset(string Name, string EmptyPath, string FullPath);

internal static class LocalPresets
{
    private static readonly string DirectoryPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RecycleBinStudio", "Presets");

    public static IReadOnlyList<LocalPreset> Read()
    {
        if (!Directory.Exists(DirectoryPath))
            return [];

        return Directory.EnumerateFiles(DirectoryPath, "*.json")
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .Select(path => JsonSerializer.Deserialize<LocalPreset>(File.ReadAllText(path))
                ?? throw new InvalidDataException("Не удалось прочитать сохранённый пресет."))
            .ToArray();
    }

    public static LocalPreset Save(string name, PreparedIcon empty, PreparedIcon full)
    {
        Directory.CreateDirectory(DirectoryPath);
        var preset = new LocalPreset(name, empty.Save(), full.Save());
        string path = Path.Combine(DirectoryPath, Guid.NewGuid().ToString("N"));
        string pending = path + ".tmp";
        try
        {
            File.WriteAllText(pending, JsonSerializer.Serialize(preset));
            File.Move(pending, path + ".json");
        }
        finally
        {
            File.Delete(pending);
        }

        return preset;
    }
}
