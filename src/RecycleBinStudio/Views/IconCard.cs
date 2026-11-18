using System.ComponentModel;
using System.Windows.Media.Imaging;
using RecycleBinStudio.Images;

namespace RecycleBinStudio.Views;

internal sealed class IconCard(string title) : INotifyPropertyChanged
{
    private BitmapSource? systemPreview;
    private string? location;
    private bool active;

    public string Title { get; } = title;
    public PreparedIcon? Draft { get; private set; }
    public BitmapSource? Preview => Draft?.Preview ?? systemPreview;
    public string Caption => Draft is not null ? Draft.Name : systemPreview is not null ? "Иконка из Windows" : "Иконка недоступна";
    public bool Active => active;
    public bool HasDraft => Draft is not null;

    public void UpdateSystem(string? newLocation, BitmapSource? image, bool isActive)
    {
        if (location == newLocation && ReferenceEquals(systemPreview, image) && active == isActive)
            return;

        location = newLocation;
        systemPreview = image;
        active = isActive;
        NotifyChanged();
    }

    public void SetDraft(PreparedIcon? icon)
    {
        Draft = icon;
        NotifyChanged();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void NotifyChanged() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
}
