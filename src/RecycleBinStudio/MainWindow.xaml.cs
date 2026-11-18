using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using RecycleBinStudio.Images;
using RecycleBinStudio.Updates;
using RecycleBinStudio.Views;
using RecycleBinStudio.Windows;

namespace RecycleBinStudio;

public partial class MainWindow : Window
{
    private readonly IconCard empty = new("Пустая");
    private readonly IconCard full = new("Полная");
    private readonly PresetCatalog catalog = new();
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private bool busy;
    private bool refreshing;
    private BinIcons? systemIcons;
    private BitmapSource? emptyPreview;
    private BitmapSource? fullPreview;

    public MainWindow()
    {
        InitializeComponent();
        EmptyCard.Content = empty;
        FullCard.Content = full;
        timer.Tick += async (_, _) => await RefreshAsync();
        Loaded += async (_, _) =>
        {
            timer.Start();
            _ = catalog.LoadAsync();
            await RefreshAsync();
            await CheckAutomaticUpdateAsync();
        };
        Closed += (_, _) => timer.Stop();
    }

    private async Task RefreshAsync()
    {
        if (refreshing || busy)
            return;

        refreshing = true;
        try
        {
            var previous = systemIcons;
            var snapshot = await Task.Run(() =>
            {
                var icons = RecycleBinShell.ReadIcons();
                long? count = RecycleBinShell.ItemCount();
                var currentEmpty = previous?.Empty == icons.Empty && emptyPreview is not null
                    ? emptyPreview : RecycleBinShell.Preview(icons.Empty);
                var currentFull = previous?.Full == icons.Full && fullPreview is not null
                    ? fullPreview : RecycleBinShell.Preview(icons.Full);
                return (icons, count, currentEmpty, currentFull);
            });

            systemIcons = snapshot.icons;
            emptyPreview = snapshot.currentEmpty;
            fullPreview = snapshot.currentFull;
            empty.UpdateSystem(systemIcons.Empty, emptyPreview, snapshot.count == 0);
            full.UpdateSystem(systemIcons.Full, fullPreview, snapshot.count > 0);
            BinStatus.Text = snapshot.count switch
            {
                null => "Состояние корзины недоступно",
                0 => "Корзина пуста",
                1 => "Корзина содержит 1 объект",
                _ => $"Объектов в корзине: {snapshot.count}"
            };
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or
            System.Security.SecurityException or ExternalException or ArgumentException)
        {
            BinStatus.Text = "Не удалось прочитать состояние корзины";
            empty.UpdateSystem(null, null, false);
            full.UpdateSystem(null, null, false);
        }
        finally
        {
            refreshing = false;
        }
    }

    private async void ChooseIcon(object sender, RoutedEventArgs e)
    {
        var card = (IconCard)((FrameworkElement)sender).DataContext;
        var dialog = new OpenFileDialog { Filter = "Иконки и изображения|*.ico;*.png;*.jpg;*.jpeg" };
        if (dialog.ShowDialog(this) == true)
            await ImportAsync(card, dialog.FileName);
    }

    private void CardDragOver(object sender, DragEventArgs e)
    {
        e.Effects = !busy && e.Data.GetData(DataFormats.FileDrop) is string[] { Length: 1 }
            ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private async void CardDrop(object sender, DragEventArgs e)
    {
        e.Handled = true;
        if (!busy && e.Data.GetData(DataFormats.FileDrop) is string[] { Length: 1 } files)
            await ImportAsync((IconCard)((FrameworkElement)sender).DataContext, files[0]);
    }

    private Task ImportAsync(IconCard card, string path) => RunAsync("Подготавливаем иконку…", async () =>
    {
        card.SetDraft(await Task.Run(() => IconFile.Load(path)));
        return "Иконка готова к применению.";
    });

    private void DiscardIcon(object sender, RoutedEventArgs e)
    {
        ((IconCard)((FrameworkElement)sender).DataContext).SetDraft(null);
        Feedback.Text = "";
        UpdateActions();
    }

    private async void OpenCatalog(object sender, RoutedEventArgs e)
    {
        var window = new CatalogWindow(catalog) { Owner = this };
        if (window.ShowDialog() != true || window.SelectedPreset is not { Pair: { } pair } preset)
            return;

        await RunAsync("Применяем пресет…", async () =>
        {
            await Task.Run(() => RecycleBinShell.Apply(pair.Empty.Save(), pair.Full.Save()));
            ClearDrafts();
            return $"Пресет «{preset.Name}» применён.";
        });
    }

    private void OpenUpdates(object sender, RoutedEventArgs e)
    {
        var window = new UpdatesWindow(!busy && !empty.HasDraft && !full.HasDraft) { Owner = this };
        if (window.ShowDialog() == true)
            Close();
    }

    private async Task CheckAutomaticUpdateAsync()
    {
        try
        {
            if (!UpdateService.IsInstalled || !UpdateService.Automatic)
                return;

            var update = await UpdateService.CheckAsync();
            if (update is null || !CanInstallAutomatically())
                return;

            Feedback.Text = $"Скачиваем обновление {update.Version}…";
            string path = await UpdateService.DownloadAsync(update);
            if (!CanInstallAutomatically())
            {
                File.Delete(path);
                if (IsVisible && !busy)
                    Feedback.Text = "Обновление доступно в разделе «Обновления».";
                return;
            }

            UpdateService.StartInstaller(path);
            Close();
        }
        catch (Exception error)
        {
            if (IsVisible && !busy)
                Feedback.Text = $"Не удалось обновиться автоматически. {error.Message}";
        }
    }

    private bool CanInstallAutomatically() => IsVisible && !busy && !empty.HasDraft && !full.HasDraft &&
        !OwnedWindows.Cast<Window>().Any(window => window.IsVisible) && UpdateService.Automatic;

    private async void OpenPresetStudio(object sender, RoutedEventArgs e)
    {
        var window = new PresetWindow { Owner = this };
        if (window.ShowDialog() != true)
            return;

        ClearDrafts();
        UpdateActions();
        Feedback.Text = "Пресет применён.";
        await RefreshAsync();
    }

    private async void ApplyIcons(object sender, RoutedEventArgs e)
    {
        var emptyDraft = empty.Draft;
        var fullDraft = full.Draft;

        await RunAsync("Применяем иконки…", async () =>
        {
            await Task.Run(() => RecycleBinShell.Apply(emptyDraft?.Save(), fullDraft?.Save()));
            ClearDrafts();
            return "Иконки применены.";
        });
    }

    private async void RestoreDefaults(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show(this, "Вернуть стандартные значки Windows для обоих состояний?", Title,
                MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK)
            return;

        await RunAsync("Восстанавливаем значки Windows…", async () =>
        {
            await Task.Run(RecycleBinShell.RestoreDefaults);
            ClearDrafts();
            return "Стандартные значки Windows восстановлены.";
        });
    }

    private void ClearDrafts()
    {
        empty.SetDraft(null);
        full.SetDraft(null);
    }

    private async Task RunAsync(string working, Func<Task<string>> action)
    {
        if (busy)
            return;

        busy = true;
        UpdateActions();
        Feedback.Text = working;

        try
        {
            Feedback.Text = await action();
        }
        catch (Exception error)
        {
            Feedback.Text = $"Не удалось завершить операцию. {error.Message}";
        }
        finally
        {
            busy = false;
            UpdateActions();
        }

        await RefreshAsync();
    }

    private void UpdateActions()
    {
        Cards.IsEnabled = !busy;
        Actions.IsEnabled = !busy;
        ApplyButton.IsEnabled = empty.HasDraft || full.HasDraft;
    }
}
