using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
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
    private Task refresh = Task.CompletedTask;
    private BinIcons? systemIcons;
    private BitmapSource? emptyPreview;
    private BitmapSource? fullPreview;

    public MainWindow()
    {
        InitializeComponent();
        EmptyCard.Content = empty;
        FullCard.Content = full;
        UpdateActions();
        Feedback.Text = DefaultHint;
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

    private Task RefreshAsync() =>
        !busy && refresh.IsCompleted ? refresh = ReadSnapshotAsync() : refresh;

    private async Task ReadSnapshotAsync()
    {
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
            empty.UpdateSystem(emptyPreview, snapshot.count == 0);
            full.UpdateSystem(fullPreview, snapshot.count > 0);
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
            empty.UpdateSystem(null, false);
            full.UpdateSystem(null, false);
        }
    }

    private async void ChooseIcon(object sender, RoutedEventArgs e)
    {
        var card = (IconCard)((FrameworkElement)sender).DataContext;
        var dialog = new OpenFileDialog { Filter = "Иконки и изображения|*.ico;*.png;*.jpg;*.jpeg" };
        if (dialog.ShowDialog(this) == true)
            await ImportAsync(card, dialog.FileName);
    }

    private void PreviewUpdated(object sender, DataTransferEventArgs e)
    {
        if (sender is Image { IsLoaded: true, Source: not null } image && SystemParameters.ClientAreaAnimation)
            image.BeginAnimation(OpacityProperty, new DoubleAnimation(0.72, 1, TimeSpan.FromMilliseconds(120))
            {
                FillBehavior = FillBehavior.Stop
            });
    }

    private void CardDragOver(object sender, DragEventArgs e)
    {
        bool canDrop = !busy && e.Data.GetData(DataFormats.FileDrop) is string[] { Length: 1 };
        var target = (Border)sender;
        target.BorderBrush = (Brush)FindResource(canDrop ? "Mint" : "Outline");
        e.Effects = canDrop ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private static void ClearDropHighlight(object sender) => ((Border)sender).ClearValue(Border.BorderBrushProperty);

    private void CardDragLeave(object sender, DragEventArgs e) => ClearDropHighlight(sender);

    private async void CardDrop(object sender, DragEventArgs e)
    {
        ClearDropHighlight(sender);
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
        UpdateActions();
        Feedback.Text = HasDraft ? "Изменения ещё не применены." : DefaultHint;
        if (!HasDraft)
            ChooseThemeButton.Focus();
    }

    private void OpenCatalog(object sender, RoutedEventArgs e)
    {
        var window = new CatalogWindow(catalog) { Owner = this };
        if (window.ShowDialog() == true && window.SelectedPreset is { Pair: { } pair } preset)
            StagePair(pair, preset.Name);
    }

    private void OpenMenu(object sender, RoutedEventArgs e)
    {
        MoreButton.ContextMenu.PlacementTarget = MoreButton;
        MoreButton.ContextMenu.IsOpen = true;
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

    private void OpenPresetStudio(object sender, RoutedEventArgs e)
    {
        var window = new PresetWindow { Owner = this };
        if (window.ShowDialog() == true && window.Result is { } pair)
            StagePair(pair, window.ResultName);
    }

    private void StagePair(PresetPair pair, string name)
    {
        empty.SetDraft(pair.Empty with { Name = name });
        full.SetDraft(pair.Full with { Name = name });
        Feedback.Text = $"Тема «{name}» готова к применению.";
        UpdateActions();
        ApplyButton.Focus();
    }

    private async void ApplyIcons(object sender, RoutedEventArgs e)
    {
        if (!HasDraft)
            return;
        var emptyDraft = empty.Draft;
        var fullDraft = full.Draft;

        await RunAsync("Применяем иконки…", async () =>
        {
            await Task.Run(() => RecycleBinShell.Apply(emptyDraft?.Save(), fullDraft?.Save()));
            await RefreshAfterWriteAsync();
            ClearDrafts();
            return "Значки корзины обновлены.";
        });
        (HasDraft ? ApplyButton : ChooseThemeButton).Focus();
    }

    private async void RestoreDefaults(object sender, RoutedEventArgs e)
    {
        string message = "Вернуть стандартные значки Windows для пустой и полной корзины?";
        if (HasDraft)
            message += "\n\nТекущие правки будут сброшены.";
        if (MessageBox.Show(this, message, "Стандартные значки",
                MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK)
            return;

        await RunAsync("Восстанавливаем значки Windows…", async () =>
        {
            await Task.Run(RecycleBinShell.RestoreDefaults);
            await RefreshAfterWriteAsync();
            ClearDrafts();
            return "Стандартные значки Windows восстановлены.";
        });
    }

    private void ClearDrafts()
    {
        empty.SetDraft(null);
        full.SetDraft(null);
    }

    private async Task RefreshAfterWriteAsync()
    {
        await refresh;
        await (refresh = ReadSnapshotAsync());
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
    }

    private const string DefaultHint = "Перетащите ICO, PNG или JPG на нужное превью.";
    private bool HasDraft => empty.HasDraft || full.HasDraft;

    private void UpdateActions()
    {
        Cards.IsEnabled = !busy;
        Actions.IsEnabled = !busy;
        MoreButton.IsEnabled = !busy;
        ChooseThemeButton.Visibility = HasDraft ? Visibility.Collapsed : Visibility.Visible;
        BrowseThemesButton.Visibility = ApplyButton.Visibility = HasDraft ? Visibility.Visible : Visibility.Collapsed;
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        e.Cancel = busy;
        base.OnClosing(e);
    }
}
