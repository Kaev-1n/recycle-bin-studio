using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using RecycleBinStudio.Images;
using RecycleBinStudio.Windows;

namespace RecycleBinStudio.Views;

public partial class PresetWindow : Window
{
    private BitmapSource? sheet;
    private BitmapSource? empty;
    private BitmapSource? full;
    private bool swapped;
    private LocalPreset? saved;
    private bool busy;

    public PresetWindow()
    {
        InitializeComponent();
        PromptTemplates.ItemsSource = PresetPrompt.Templates;
        PromptTemplates.SelectedIndex = 0;
        UpdateSavedList();
    }

    private void UpdateSavedList()
    {
        try
        {
            var presets = LocalPresets.Read();
            SavedPresets.ItemsSource = presets;
            SavedPresets.SelectedIndex = 0;
            SavedSection.Visibility = presets.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
            Feedback.Text = $"Не удалось прочитать список пресетов. {error.Message}";
        }
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
        e.Effects = !busy && e.Data.GetData(DataFormats.FileDrop) is string[] { Length: 1 or 2 }
            ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private async void ImagesDropped(object sender, DragEventArgs e)
    {
        e.Handled = true;
        if (!busy && e.Data.GetData(DataFormats.FileDrop) is string[] { Length: 1 or 2 } paths)
            await ImportAsync(paths);
    }

    private Task ImportAsync(string[] paths, bool? replaceEmpty = null) => RunAsync(async () =>
    {
        var frames = await Task.Run(() => paths.Select(PresetImage.Load).ToArray());
        if (replaceEmpty is bool isEmpty)
        {
            sheet = null;
            if (isEmpty)
                empty = frames[0];
            else
                full = frames[0];
        }
        else if (frames.Length == 2)
        {
            sheet = null;
            (empty, full) = (frames[0], frames[1]);
        }
        else
        {
            var pair = PresetImage.Split(frames[0], 0.5);
            sheet = null;
            SplitPosition.Value = 0.5;
            sheet = frames[0];
            swapped = false;
            (empty, full) = pair;
        }

        if (saved is not null)
            PresetName.Clear();
        saved = null;
        PresetName.IsReadOnly = false;
        SplitControls.Visibility = sheet is null ? Visibility.Collapsed : Visibility.Visible;
        RenderPreviews();
    });

    private void SplitChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (sheet is null)
            return;
        SplitSheet();
        RenderPreviews();
    }

    private void SplitSheet()
    {
        var (left, right) = PresetImage.Split(sheet!, SplitPosition.Value);
        (empty, full) = swapped ? (right, left) : (left, right);
    }

    private void Swap(object sender, RoutedEventArgs e)
    {
        swapped = !swapped;
        (empty, full) = (full, empty);
        RenderPreviews();
    }

    private void RenderPreviews()
    {
        EmptyLarge.Source = Preview(empty, 64);
        EmptySmall.Source = Preview(empty, 32);
        FullLarge.Source = Preview(full, 64);
        FullSmall.Source = Preview(full, 32);
        UpdateButtons();

        static BitmapSource? Preview(BitmapSource? source, int size) =>
            source is null ? null : IconFile.Render(source, size);
    }

    private void NameChanged(object sender, TextChangedEventArgs e) => UpdateButtons();

    private void UpdateButtons()
    {
        if (PresetName is null)
            return;

        bool hasPair = empty is not null && full is not null;
        ApplyPresetButton.Content = saved is null ? "Сохранить и применить" : "Применить";
        ApplyPresetButton.IsEnabled = !busy && hasPair && !string.IsNullOrWhiteSpace(PresetName.Text);
        SwapButton.IsEnabled = !busy && hasPair && saved is null;
    }

    private async void LoadSaved(object sender, RoutedEventArgs e)
    {
        if (SavedPresets.SelectedItem is not LocalPreset preset)
            return;

        await RunAsync(async () =>
        {
            (empty, full) = await Task.Run(() => (PresetImage.Load(preset.EmptyPath), PresetImage.Load(preset.FullPath)));
            sheet = null;
            SplitControls.Visibility = Visibility.Collapsed;
            saved = preset;
            PresetName.Text = preset.Name;
            PresetName.IsReadOnly = true;
            RenderPreviews();
        });
    }

    private async void Apply(object sender, RoutedEventArgs e)
    {
        if (empty is not { } first || full is not { } second)
            return;

        string name = PresetName.Text.Trim();
        bool applied = await RunAsync(async () =>
        {
            if (saved is null)
            {
                saved = await Task.Run(() =>
                    LocalPresets.Save(name, IconFile.Encode(first, "empty"), IconFile.Encode(second, "full")));
                PresetName.IsReadOnly = true;
                UpdateSavedList();
            }

            var preset = saved;
            try
            {
                await Task.Run(() => RecycleBinShell.Apply(preset.EmptyPath, preset.FullPath));
            }
            catch (Exception error)
            {
                throw new IOException($"Пресет сохранён. Не удалось применить иконки в Windows. {error.Message}", error);
            }
        });

        if (applied)
            DialogResult = true;
    }

    private async Task<bool> RunAsync(Func<Task> action)
    {
        if (busy)
            return false;

        busy = true;
        Form.IsEnabled = false;
        Feedback.Text = "Обрабатываем…";
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
            UpdateButtons();
        }
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        e.Cancel = busy;
        base.OnClosing(e);
    }
}
