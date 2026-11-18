using System.Windows;
using RecycleBinStudio.Images;

namespace RecycleBinStudio.Views;

public partial class CatalogWindow : Window
{
    internal BuiltInPreset? SelectedPreset { get; private set; }

    public CatalogWindow()
    {
        InitializeComponent();
        Presets.ItemsSource = BuiltInPresets.Load();
    }

    private void SelectPreset(object sender, RoutedEventArgs e)
    {
        SelectedPreset = (BuiltInPreset)((FrameworkElement)sender).DataContext;
        DialogResult = true;
    }
}
