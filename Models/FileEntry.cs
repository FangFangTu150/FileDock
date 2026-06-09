using System.Windows.Media;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FileDock.Models;

public sealed class FileEntry : INotifyPropertyChanged
{
    private string _fullPath = string.Empty;
    private string _displayName = string.Empty;
    private DateTime _lastModified;
    private ImageSource? _icon;
    private bool _isFolder;
    private bool _isIconLoaded;

    public event PropertyChangedEventHandler? PropertyChanged;

    public required string FullPath
    {
        get => _fullPath;
        set => SetField(ref _fullPath, value);
    }

    public required string DisplayName
    {
        get => _displayName;
        set => SetField(ref _displayName, value);
    }

    public DateTime LastModified
    {
        get => _lastModified;
        set => SetField(ref _lastModified, value);
    }

    public ImageSource? Icon
    {
        get => _icon;
        set => SetField(ref _icon, value);
    }

    public bool IsFolder
    {
        get => _isFolder;
        set => SetField(ref _isFolder, value);
    }

    public bool IsIconLoaded
    {
        get => _isIconLoaded;
        set => SetField(ref _isIconLoaded, value);
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
