using System.Diagnostics;
using System.IO;
using System.Windows;
using RecycleBinStudio.Updates;

namespace RecycleBinStudio.Views;

public partial class UpdatesWindow : Window
{
    private readonly bool installed = UpdateService.IsInstalled;
    private readonly bool canRestart;
    private AvailableUpdate? update;
    private bool installing;

    internal UpdatesWindow(bool canRestart)
    {
        this.canRestart = canRestart;
        InitializeComponent();
        VersionLabel.Text = $"Версия {UpdateService.VersionLabel}";
        Automatic.IsEnabled = installed;
        Automatic.IsChecked = installed && UpdateService.Automatic;
        Automatic.Checked += ToggleAutomatic;
        Automatic.Unchecked += ToggleAutomatic;
        Details.Text = installed
            ? "Новая стабильная версия установится при запуске. Приложение откроется снова."
            : "В этой копии доступна проверка. Для установки обновления откройте релиз на GitHub.";
    }

    private void ToggleAutomatic(object sender, RoutedEventArgs e)
    {
        try
        {
            UpdateService.Automatic = Automatic.IsChecked == true;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            Automatic.IsEnabled = false;
            Status.Text = $"Не удалось сохранить настройку. {error.Message}";
        }
    }

    private async void Check(object sender, RoutedEventArgs e)
    {
        CheckButton.IsEnabled = false;
        InstallButton.Visibility = Visibility.Collapsed;
        Status.Text = "Проверяем релизы GitHub…";
        try
        {
            update = await UpdateService.CheckAsync();
            Status.Text = update is null ? "Обновлений нет." : $"Доступна версия {update.Version}.";
            if (update is not null && installed)
            {
                InstallButton.Visibility = canRestart ? Visibility.Visible : Visibility.Collapsed;
                if (!canRestart)
                    Status.Text += " Примените или отмените черновики иконок перед установкой.";
            }
        }
        catch (Exception error)
        {
            Status.Text = $"Не удалось проверить обновления. {error.Message}";
        }
        finally
        {
            CheckButton.IsEnabled = true;
        }
    }

    private async void Install(object sender, RoutedEventArgs e)
    {
        if (update is null || !installed || !canRestart || installing)
            return;

        installing = true;
        CheckButton.IsEnabled = false;
        InstallButton.IsEnabled = false;
        Status.Text = "Скачиваем и проверяем установщик…";
        try
        {
            string path = await UpdateService.DownloadAsync(update);
            UpdateService.StartInstaller(path);
        }
        catch (Exception error)
        {
            Status.Text = $"Не удалось установить обновление. {error.Message}";
            return;
        }
        finally
        {
            installing = false;
            CheckButton.IsEnabled = true;
            InstallButton.IsEnabled = true;
        }

        DialogResult = true;
    }

    private void OpenReleases(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo(UpdateService.ReleasesPage) { UseShellExecute = true });
        }
        catch (System.ComponentModel.Win32Exception error)
        {
            Status.Text = $"Не удалось открыть браузер. {error.Message}";
        }
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        e.Cancel = installing;
        base.OnClosing(e);
    }
}
