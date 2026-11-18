using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using RecycleBinStudio.Images;

namespace RecycleBinStudio.Views;

public partial class PresetWindow : Window
{
    private BitmapSource? sheet;
    private BitmapSource? previewSheet;
    private BitmapSource? empty;
    private BitmapSource? full;
    private bool swapped;
    private bool busy;
    internal PresetPair? Result { get; private set; }
    internal string ResultName { get; private set; } = "";

    public PresetWindow()
    {
        InitializeComponent();
        PromptTemplates.ItemsSource = PresetPrompt.Templates;
        PromptTemplates.SelectedIndex = 0;
        Feedback.Text = "Поддерживаются PNG, JPG и ICO.";
    }

    private void CopyPrompt(object sender, RoutedEventArgs e)
    {
        try
        {
            Clipboard.SetText(PresetPrompt.Build((PromptTemplate)PromptTemplates.SelectedItem));
            Feedback.Text = "Промпт скопирован.";
        }
        catch (System.Runtime.InteropServices.ExternalException)
        {
            Feedback.Text = "Буфер обмена занят. Попробуйте ещё раз.";
        }
    }

    private void OpenGenerator(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo("https://chatgpt.com/") { UseShellExecute = true });
        }
        catch (System.ComponentModel.Win32Exception error)
        {
            Feedback.Text = $"Не удалось открыть браузер. {error.Message}";
        }
    }

    private string? ChooseFile()
    {
        var dialog = new OpenFileDialog { Filter = "Изображения|*.png;*.jpg;*.jpeg;*.ico" };
        return dialog.ShowDialog(this) == true ? dialog.FileName : null;
    }

    private async void ChoosePair(object sender, RoutedEventArgs e)
    {
        if (ChooseFile() is { } path)
            await ImportAsync([path]);
    }

    private async void ChooseSingle(object sender, RoutedEventArgs e)
    {
        if (ChooseFile() is { } path)
            await ImportAsync([path], (string)((Button)sender).Tag == "empty");
    }

    private void ImagesDragOver(object sender, DragEventArgs e)
    {
        bool canDrop = !busy && e.Data.GetData(DataFormats.FileDrop) is string[] { Length: 1 or 2 };
        ImportPanel.BorderBrush = (Brush)FindResource(canDrop ? "Mint" : "Outline");
        e.Effects = canDrop ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void ImagesDragLeave(object sender, DragEventArgs e) => ImportPanel.ClearValue(Border.BorderBrushProperty);

    private async void ImagesDropped(object sender, DragEventArgs e)
    {
        ImportPanel.ClearValue(Border.BorderBrushProperty);
        e.Handled = true;
        if (!busy && e.Data.GetData(DataFormats.FileDrop) is string[] { Length: 1 or 2 } paths)
            await ImportAsync(paths);
    }

    private Task ImportAsync(string[] paths, bool? replaceEmpty = null) => RunAsync(async () =>
    {
        var input = await Task.Run(() =>
        {
            var frames = paths.Select(PresetImage.Load).ToArray();
            return (Frames: frames, Preview: paths.Length == 1 && replaceEmpty is null
                ? PresetImage.PreviewSheet(frames[0]) : null);
        });

        if (replaceEmpty is bool isEmpty)
        {
            (empty, full) = CurrentPair();
            if (isEmpty)
                empty = input.Frames[0];
            else
                full = input.Frames[0];
        }
        else if (input.Frames.Length == 2)
        {
            (empty, full) = (input.Frames[0], input.Frames[1]);
        }

        sheet = input.Preview is null ? null : input.Frames[0];
        previewSheet = null;
        SplitPosition.Value = 0.5;
        previewSheet = input.Preview;
        swapped = false;
        SourceImage.Source = previewSheet;
        SplitControls.Visibility = previewSheet is null ? Visibility.Collapsed : Visibility.Visible;
        UpdateDivider();
        RenderPreviews();
    });

    private void SplitChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (previewSheet is null)
            return;

        UpdateDivider();
        RenderPreviews();
    }

    private void UpdateDivider(object sender, SizeChangedEventArgs e) => UpdateDivider();

    private void UpdateDivider()
    {
        if (previewSheet is not null)
            Canvas.SetLeft(Divider, SourcePreview.ActualWidth * SplitPosition.Value);
    }

    private (BitmapSource? Empty, BitmapSource? Full) CurrentPair() =>
        sheet is null ? (empty, full) : SplitPair(sheet);

    private (BitmapSource Left, BitmapSource Right) SplitPair(BitmapSource source)
    {
        var (left, right) = PresetImage.Split(source, SplitPosition.Value);
        return swapped ? (right, left) : (left, right);
    }

    private void Swap(object sender, RoutedEventArgs e)
    {
        if (sheet is null)
            (empty, full) = (full, empty);
        else
            swapped = !swapped;
        RenderPreviews();
    }

    private void RenderPreviews()
    {
        (BitmapSource? first, BitmapSource? second) = previewSheet is null ? (empty, full) : SplitPair(previewSheet);
        EmptyLarge.Source = Preview(first, 56);
        EmptySmall.Source = Preview(first, 32);
        FullLarge.Source = Preview(second, 56);
        FullSmall.Source = Preview(second, 32);
        UpdateButtons();

        static BitmapSource? Preview(BitmapSource? source, int size) =>
            source is null ? null : IconFile.Render(source, size);
    }

    private void NameChanged(object sender, TextChangedEventArgs e) => UpdateButtons();

    private void UpdateButtons()
    {
        if (PresetName is null)
            return;

        bool hasPair = sheet is not null || (empty is not null && full is not null);
        SaveControls.Visibility = hasPair ? Visibility.Visible : Visibility.Collapsed;
        SavePresetButton.IsEnabled = !busy && hasPair && !string.IsNullOrWhiteSpace(PresetName.Text);
        SwapButton.Visibility = hasPair ? Visibility.Visible : Visibility.Hidden;
    }

    private async void Save(object sender, RoutedEventArgs e)
    {
        if (CurrentPair() is not ({ } first, { } second))
            return;

        string name = PresetName.Text.Trim();
        if (await RunAsync(async () =>
        {
            Result = await Task.Run(() =>
            {
                var pair = new PresetPair(IconFile.Encode(first, "Пустая"), IconFile.Encode(second, "Полная"));
                LocalPresets.Save(name, pair.Empty, pair.Full);
                return pair;
            });
            ResultName = name;
        }, "Сохраняем тему…"))
        {
            DialogResult = true;
        }
    }

    private async Task<bool> RunAsync(Func<Task> action, string progress = "Подготавливаем превью…")
    {
        if (busy)
            return false;

        busy = true;
        Form.IsEnabled = false;
        SaveControls.IsEnabled = false;
        Feedback.Text = progress;
        try
        {
            await action();
            Feedback.Text = "";
            return true;
        }
        catch (Exception error)
        {
            Feedback.Text = error.Message;
            return false;
        }
        finally
        {
            busy = false;
            Form.IsEnabled = true;
            SaveControls.IsEnabled = true;
            UpdateButtons();
        }
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        e.Cancel = busy;
        base.OnClosing(e);
    }
}
