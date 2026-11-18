# Recycle Bin Studio

A Windows desktop app for customizing the empty and full Recycle Bin icons independently.

**[Download the latest release](https://github.com/Kaev-1n/recycle-bin-studio/releases/latest)** · Russian interface

1. Run the setup for your PC (x64 or ARM64) and open Recycle Bin Studio from Start.
2. Drop an ICO, PNG, or JPG onto either card, select a file, or apply a pair from the preset catalog.
3. Preview both icons and click **Применить**. Use **Значки Windows** to restore the built-in icons.

The app reads both configured icons and the current item count from Windows once per second while the window is open. It converts imports into transparent, multi-size ICO files (16–256 px), keeping the original aspect ratio and matching existing ICO frames where available.

Settings apply to the current Windows user and persist after closing the app. No administrator rights or separate .NET installation are required. If Explorer still shows an old desktop icon, click the desktop and press **F5**. Installed icons are stored in `%LOCALAPPDATA%\RecycleBinStudio\Icons`.

**AI presets** include two copyable prompts with a shared two-panel layout. Import a single image, adjust the split or swap both sides, and save the pair. You can also import two separate files. Saved presets stay in your local profile when the app is updated or uninstalled.

The preset catalog includes Portrait Bin and Pop Cat. Six additional themes download from a pinned upstream revision on first launch and are then available offline.

**Updates** checks the latest stable GitHub release. Installed copies can download and install updates from the app, with automatic updates on launch available as an opt-in.

## Build

Use the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) on Windows. See [supported Windows versions](https://learn.microsoft.com/en-us/dotnet/core/install/windows#supported-versions).

```powershell
dotnet run --project src/RecycleBinStudio
```

To package the x64 and ARM64 installers with [Inno Setup](https://jrsoftware.org/isinfo.php), run:

```powershell
./installer/build.ps1 -Compiler "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
```

Publish both `RecycleBinStudio-Setup-win-*.exe` files to the matching stable GitHub release. The app selects the installer for its architecture and checks its SHA-256 digest before launching it.

## License

The application code is [MIT licensed](LICENSE). Preset artwork is credited separately. Pop Cat icons come from [sdushantha/recycle-bin-themes](https://github.com/sdushantha/recycle-bin-themes/tree/main/themes/pop-cat). Portrait Bin artwork is contributed by Kaev-1n.
