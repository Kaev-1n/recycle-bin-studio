using System.ComponentModel;
using System.Windows.Media.Imaging;
using RecycleBinStudio.Images;

namespace RecycleBinStudio.Views;

internal sealed class IconCard(string title) : INotifyPropertyChanged
{
    private BitmapSource? systemPreview;
    private bool active;

    public string Title { get; } = title;
    public PreparedIcon? Draft { get; private set; }
    public BitmapSource? Preview => Draft?.Preview ?? systemPreview;
    public string Caption => Draft is not null ? $"Черновик · {Draft.Name}" : systemPreview is not null ? "Установленный значок" : "Значок недоступен";
    public bool Active => active;
    public bool HasDraft => Draft is not null;

    public void UpdateSystem(BitmapSource? image, bool isActive)
    {
        if (active != isActive)
        {
            active = isActive;
            NotifyChanged(nameof(Active));
        }
        if (ReferenceEquals(systemPreview, image))
            return;

        systemPreview = image;
        if (!HasDraft)
        {
            NotifyChanged(nameof(Preview));
            NotifyChanged(nameof(Caption));
        }
    }

    public void SetDraft(PreparedIcon? icon)
    {
        if (ReferenceEquals(Draft, icon))
            return;

        Draft = icon;
        NotifyChanged();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void NotifyChanged(string? property = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
}
