using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using RecycleBinStudio.Images;

namespace RecycleBinStudio.Views;

public partial class CatalogWindow : Window
{
    private readonly PresetCatalog catalog;
    private bool savedLoaded;
    internal CatalogPreset? SelectedPreset { get; private set; }

    internal CatalogWindow(PresetCatalog catalog)
    {
        this.catalog = catalog;
        InitializeComponent();
        Presets.ItemsSource = catalog.Items;
        Loaded += async (_, _) => await RefreshAsync();
    }

    private void SelectPreset(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is not CatalogPreset { IsReady: true } item)
            return;

        SelectedPreset = item;
        DialogResult = true;
    }

    private async void ThemeTabChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.Source != ThemeTabs || ThemeTabs.SelectedItem != SavedTab || savedLoaded)
            return;

        savedLoaded = true;
        SavedStatus.Text = "Загружаем сохранённые темы…";
        try
        {
            var themes = await Task.Run(() => LocalPresets.Read().Select(LoadSaved).ToArray());
            SavedThemes.ItemsSource = themes;
            SavedStatus.Text = themes.Length == 0 ? "Здесь появятся темы, которые вы сохраните из фото." : "";
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException)
        {
            SavedStatus.Text = $"Не удалось прочитать сохранённые темы. {error.Message}";
            savedLoaded = false;
        }
    }

    private static CatalogPreset LoadSaved(LocalPreset preset)
    {
        var item = new CatalogPreset("", preset.Name, "");
        try
        {
            item.Update(new(PresetCatalog.Decode(Path.GetFileName(preset.EmptyPath), File.ReadAllBytes(preset.EmptyPath)),
                            PresetCatalog.Decode(Path.GetFileName(preset.FullPath), File.ReadAllBytes(preset.FullPath))));
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException or
            FormatException or NotSupportedException or System.Runtime.InteropServices.ExternalException)
        {
            item.Update(null, "Файлы темы недоступны", error.Message);
        }
        return item;
    }

    private async void Retry(object sender, RoutedEventArgs e) => await RefreshAsync();

    private async Task RefreshAsync()
    {
        RetryButton.Visibility = Visibility.Collapsed;
        await catalog.LoadAsync();
        RetryButton.Visibility = catalog.Items.All(item => item.IsReady) ? Visibility.Collapsed : Visibility.Visible;
    }
}
