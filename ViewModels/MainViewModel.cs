using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using FileDock.Models;

namespace FileDock.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private string _watchPath = string.Empty;
    private bool _showFolders;

    public MainViewModel()
    {
        FileList.CollectionChanged += OnFileListChanged;
        OpenFileCommand = new RelayCommand<FileEntry>(OpenFile, entry => entry is not null);
        OpenLocationCommand = new RelayCommand<FileEntry>(OpenLocation, entry => entry is not null);
        CopyFileCommand = new RelayCommand<FileEntry>(CopyFile, entry => entry is not null);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<FileEntry> FileList { get; } = [];

    public ICommand OpenFileCommand { get; }
    public ICommand OpenLocationCommand { get; }
    public ICommand CopyFileCommand { get; }

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
            OnPropertyChanged(nameof(EmptyStateText));
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

    public bool HasFiles => FileList.Count > 0;

    public string EmptyStateText => string.IsNullOrWhiteSpace(WatchPath)
        ? "点击设置按钮添加监听路径"
        : "这个目录暂时还没有可展示的项目";

    public void ApplySettings(AppSettings settings)
    {
        WatchPath = settings.WatchPath;
        ShowFolders = settings.ShowFolders;
    }

    public void LoadSnapshot(IEnumerable<FileEntry> entries)
    {
        FileList.Clear();
        foreach (var entry in entries)
        {
            FileList.Add(entry);
        }
    }

    private static void OpenFile(FileEntry? entry)
    {
        if (entry is null)
        {
            return;
        }

        Process.Start(new ProcessStartInfo(entry.FullPath)
        {
            UseShellExecute = true
        });
    }

    private static void OpenLocation(FileEntry? entry)
    {
        if (entry is null)
        {
            return;
        }

        var argument = entry.IsFolder
            ? $"\"{entry.FullPath}\""
            : $"/select,\"{entry.FullPath}\"";

        Process.Start(new ProcessStartInfo("explorer.exe", argument)
        {
            UseShellExecute = true
        });
    }

    private static void CopyFile(FileEntry? entry)
    {
        if (entry is null || (!File.Exists(entry.FullPath) && !Directory.Exists(entry.FullPath)))
        {
            return;
        }

        var files = new StringCollection { entry.FullPath };
        System.Windows.Clipboard.SetFileDropList(files);
    }

    private void OnFileListChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(HasFiles));
        OnPropertyChanged(nameof(EmptyStateText));
    }

    private void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
