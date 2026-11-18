# Recycle Bin Studio

A Windows desktop app for customizing the empty and full Recycle Bin icons independently.

**[Download the latest release](https://github.com/Kaev-1n/recycle-bin-studio/releases/latest)** · Russian interface

1. Extract the ZIP for your PC (x64 or ARM64) and open `RecycleBinStudio.exe`.
2. Drop an ICO, PNG, or JPG onto either card, select a file, or load the Pop Cat preset.
3. Preview both icons and click **Применить**. Use **Значки Windows** to restore the built-in icons.

The app reads both configured icons and the current item count from Windows once per second while the window is open. It converts imports into transparent, multi-size ICO files (16–256 px), keeping the original aspect ratio and matching existing ICO frames where available.

Settings apply to the current Windows user and persist after closing the app. No administrator rights or separate .NET installation are required. If Explorer still shows an old desktop icon, click the desktop and press **F5**. Installed icons are stored in `%LOCALAPPDATA%\RecycleBinStudio\Icons`.

## Build

Use the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) on Windows. See [supported Windows versions](https://learn.microsoft.com/en-us/dotnet/core/install/windows#supported-versions).

```powershell
dotnet run --project src/RecycleBinStudio
```

The WPF window handles the two editable previews. `Images/` decodes sources, prepares ICO files and loads the preset. `Windows/` reads the current configuration, applies per-user settings and notifies Explorer.

## License

The application code is [MIT licensed](LICENSE). The Pop Cat preset is downloaded on request from [sdushantha/recycle-bin-themes](https://github.com/sdushantha/recycle-bin-themes/tree/main/themes/pop-cat). The preset artwork is hosted by the upstream project.
