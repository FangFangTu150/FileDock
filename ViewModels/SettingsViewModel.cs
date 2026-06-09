using System.ComponentModel;
using FileDock.Models;

namespace FileDock.ViewModels;

public sealed class SettingsViewModel : INotifyPropertyChanged
{
    private string _watchPath;
    private bool _showFolders;
    private bool _startWithWindows;

    public SettingsViewModel(AppSettings settings)
    {
        _watchPath = settings.WatchPath;
        _showFolders = settings.ShowFolders;
        _startWithWindows = settings.StartWithWindows;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string WatchPath
    {
        get => _watchPath;
        set
        {
            if (_watchPath == value)
            {
                return;
            }

            _watchPath = value;
            OnPropertyChanged(nameof(WatchPath));
        }
    }

    public bool ShowFolders
    {
        get => _showFolders;
        set
        {
            if (_showFolders == value)
            {
                return;
            }

            _showFolders = value;
            OnPropertyChanged(nameof(ShowFolders));
        }
    }

    public bool StartWithWindows
    {
        get => _startWithWindows;
        set
        {
            if (_startWithWindows == value)
            {
                return;
            }

            _startWithWindows = value;
            OnPropertyChanged(nameof(StartWithWindows));
        }
    }

    private void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
