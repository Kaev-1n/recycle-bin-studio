using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace RecycleBinStudio.Windows;

internal sealed record BinIcons(string? Empty, string? Full);

internal static class RecycleBinShell
{
    private const string ClassId = "{645FF040-5081-101B-9F08-00AA002F954E}";
    private const string UserKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer\CLSID\" + ClassId + @"\DefaultIcon";
    private const string MachineKey = @"SOFTWARE\Classes\CLSID\" + ClassId + @"\DefaultIcon";
    private static readonly string[] IconValues = ["Empty", "Full", ""];

    public static BinIcons ReadIcons()
    {
        using var user = Registry.CurrentUser.OpenSubKey(UserKey);
        using var machine = Registry.LocalMachine.OpenSubKey(MachineKey);
        return new(
            (user?.GetValue("Empty") ?? machine?.GetValue("Empty")) as string,
            (user?.GetValue("Full") ?? machine?.GetValue("Full")) as string);
    }

    public static long? ItemCount()
    {
        var info = new RecycleBinInfo { Size = Marshal.SizeOf<RecycleBinInfo>() };
        return SHQueryRecycleBin(null, ref info) >= 0 ? info.Items : null;
    }

    public static BitmapSource? Preview(string? location)
    {
        if (string.IsNullOrWhiteSpace(location))
            return null;

        int index = 0;
        int comma = location.LastIndexOf(',');
        if (comma >= 0 && int.TryParse(location.AsSpan(comma + 1), NumberStyles.Integer,
                CultureInfo.InvariantCulture, out int parsedIndex))
        {
            location = location[..comma];
            index = parsedIndex;
        }

        string path = Environment.ExpandEnvironmentVariables(location.Trim().Trim('"'));
        uint count = PrivateExtractIcons(path, index, 96, 96, out IntPtr icon, IntPtr.Zero, 1, 0);
        if (icon == IntPtr.Zero)
            return null;

        try
        {
            if (count != 1)
                return null;

            var image = Imaging.CreateBitmapSourceFromHIcon(icon, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            image.Freeze();
            return image;
        }
        finally
        {
            DestroyIcon(icon);
        }
    }

    public static void Apply(string? emptyPath, string? fullPath)
    {
        UpdateOverrides(key =>
        {
            long count = ItemCount() ?? throw new IOException("Windows не вернула состояние корзины.");
            var current = ReadIcons();

            if (emptyPath is not null)
                key.SetValue("Empty", IconLocation(emptyPath));
            if (fullPath is not null)
                key.SetValue("Full", IconLocation(fullPath));

            string? active = count == 0
                ? (emptyPath is null ? current.Empty : IconLocation(emptyPath))
                : (fullPath is null ? current.Full : IconLocation(fullPath));

            if (active is not null)
                key.SetValue("", active);
            else
                key.DeleteValue("", false);
        });
    }

    public static void RestoreDefaults()
    {
        using var existing = Registry.CurrentUser.OpenSubKey(UserKey);
        if (existing is null)
            return;

        UpdateOverrides(key =>
        {
            foreach (string name in IconValues)
                key.DeleteValue(name, false);
        });
    }

    private static string IconLocation(string path) => $"\"{path}\",0";

    private static void UpdateOverrides(Action<RegistryKey> update)
    {
        using var key = Registry.CurrentUser.CreateSubKey(UserKey);
        var previous = IconValues.Select(name =>
        {
            var value = key.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
            var kind = value is null ? RegistryValueKind.Unknown : key.GetValueKind(name);
            return (Name: name, Value: value, Kind: kind);
        }).ToArray();

        try
        {
            update(key);
        }
        catch
        {
            foreach (var (name, value, kind) in previous)
            {
                if (value is null)
                    key.DeleteValue(name, false);
                else
                    key.SetValue(name, value, kind);
            }

            throw;
        }

        const int associationsChanged = 0x08000000;
        const int itemUpdated = 0x00002000;
        const uint flushNoWait = 0x2000;
        SHChangeNotify(associationsChanged, flushNoWait, IntPtr.Zero, IntPtr.Zero);
        if (SHParseDisplayName("::" + ClassId, IntPtr.Zero, out IntPtr item, 0, out _) >= 0)
        {
            try
            {
                SHChangeNotify(itemUpdated, flushNoWait, item, IntPtr.Zero);
            }
            finally
            {
                Marshal.FreeCoTaskMem(item);
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    private struct RecycleBinInfo
    {
        public int Size;
        public long Bytes;
        public long Items;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHQueryRecycleBin(string? root, ref RecycleBinInfo info);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHParseDisplayName(string name, IntPtr binding, out IntPtr pidl,
        uint requestedAttributes, out uint attributes);

    [DllImport("shell32.dll")]
    private static extern void SHChangeNotify(int eventId, uint flags, IntPtr item, IntPtr other);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern uint PrivateExtractIcons(string file, int index, int width, int height,
        out IntPtr icon, IntPtr iconIds, uint count, uint flags);

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr icon);
}
