using System.Windows;
using RecycleBinStudio.Images;

namespace RecycleBinStudio.Views;

public partial class CatalogWindow : Window
{
    private readonly PresetCatalog catalog;
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

    private async void Retry(object sender, RoutedEventArgs e) => await RefreshAsync();

    private async Task RefreshAsync()
    {
        RetryButton.Visibility = Visibility.Collapsed;
        await catalog.LoadAsync();
        RetryButton.Visibility = catalog.Items.All(item => item.IsReady) ? Visibility.Collapsed : Visibility.Visible;
    }
}
