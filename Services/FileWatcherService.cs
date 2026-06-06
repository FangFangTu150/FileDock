using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Windows.Threading;
using FileDock.Models;
using ThreadingTimer = System.Threading.Timer;

namespace FileDock.Services;

public sealed class FileWatcherService : IDisposable
{
    private readonly ObservableCollection<FileEntry> _target;
    private readonly Dispatcher _dispatcher;
    private readonly ShellIconProvider _iconProvider;
    private readonly object _gate = new();

    private AppSettings _settings;
    private ThreadingTimer? _timer;
    private Dictionary<string, DateTime> _lastScan = new(StringComparer.OrdinalIgnoreCase);
    private bool _paused;
    private bool _disposed;
    private int _isScanning;

    public FileWatcherService(
        AppSettings settings,
        ObservableCollection<FileEntry> target,
        Dispatcher dispatcher,
        ShellIconProvider iconProvider)
    {
        _settings = settings;
        _target = target;
        _dispatcher = dispatcher;
        _iconProvider = iconProvider;
    }

    public void Start()
    {
        lock (_gate)
        {
            _timer ??= new ThreadingTimer(_ => Scan(), null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
        }
    }

    public void UpdateSettings(AppSettings settings)
    {
        lock (_gate)
        {
            _settings = settings;
            _lastScan = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        }

        Scan();
    }

    public void SetPaused(bool paused)
    {
        _paused = paused;
        _timer?.Change(paused ? Timeout.InfiniteTimeSpan : TimeSpan.Zero, TimeSpan.FromSeconds(1));
    }

    public FileEntry CreateEntryFromPath(string path)
    {
        var isFolder = Directory.Exists(path);
        var lastModified = isFolder && Directory.Exists(path)
            ? Directory.GetLastWriteTime(path)
            : File.Exists(path)
                ? File.GetLastWriteTime(path)
                : DateTime.MinValue;

        return new FileEntry
        {
            FullPath = path,
            DisplayName = BuildDisplayName(path, isFolder),
            LastModified = lastModified,
            IsFolder = isFolder,
            Icon = _iconProvider.GetIcon(path, isFolder)
        };
    }

    private void Scan()
    {
        if (_disposed || _paused || Interlocked.Exchange(ref _isScanning, 1) == 1)
        {
            return;
        }

        try
        {
            var settings = _settings;
            if (string.IsNullOrWhiteSpace(settings.WatchPath) || !Directory.Exists(settings.WatchPath))
            {
                _dispatcher.InvokeAsync(() => _target.Clear());
                return;
            }

            var entries = Directory
                .GetFileSystemEntries(settings.WatchPath)
                .Select(path => CreateScanEntry(path, settings.ShowFolders))
                .Where(entry => entry is not null)
                .Select(entry => entry!)
                .OrderByDescending(entry => entry.LastModified)
                .ThenBy(entry => entry.FullPath, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var current = entries.ToDictionary(entry => entry.FullPath, entry => entry.LastModified, StringComparer.OrdinalIgnoreCase);
            if (IsSameScan(current))
            {
                return;
            }

            lock (_gate)
            {
                _lastScan = current;
            }

            _dispatcher.InvokeAsync(() => ApplyDiff(entries));
        }
        catch
        {
            _dispatcher.InvokeAsync(() => _target.Clear());
        }
        finally
        {
            Interlocked.Exchange(ref _isScanning, 0);
        }
    }

    private bool IsSameScan(Dictionary<string, DateTime> current)
    {
        lock (_gate)
        {
            if (current.Count != _lastScan.Count)
            {
                return false;
            }

            foreach (var pair in current)
            {
                if (!_lastScan.TryGetValue(pair.Key, out var previous) || previous != pair.Value)
                {
                    return false;
                }
            }

            return true;
        }
    }

    private ScanEntry? CreateScanEntry(string path, bool showFolders)
    {
        try
        {
            var isFolder = Directory.Exists(path);
            if (isFolder && !showFolders)
            {
                return null;
            }

            var lastModified = isFolder
                ? Directory.GetLastWriteTime(path)
                : File.GetLastWriteTime(path);

            return new ScanEntry(path, BuildDisplayName(path, isFolder), lastModified, isFolder);
        }
        catch
        {
            return null;
        }
    }

    private void ApplyDiff(IReadOnlyList<ScanEntry> entries)
    {
        var incomingPaths = entries.Select(entry => entry.FullPath).ToHashSet(StringComparer.OrdinalIgnoreCase);
        for (var index = _target.Count - 1; index >= 0; index--)
        {
            if (!incomingPaths.Contains(_target[index].FullPath))
            {
                _target.RemoveAt(index);
            }
        }

        for (var targetIndex = 0; targetIndex < entries.Count; targetIndex++)
        {
            var incoming = entries[targetIndex];
            var existing = _target.FirstOrDefault(item => string.Equals(item.FullPath, incoming.FullPath, StringComparison.OrdinalIgnoreCase));

            if (existing is null)
            {
                _target.Insert(targetIndex, ToFileEntry(incoming));
                continue;
            }

            existing.LastModified = incoming.LastModified;
            existing.DisplayName = incoming.DisplayName;
            existing.IsFolder = incoming.IsFolder;
            existing.Icon ??= _iconProvider.GetIcon(incoming.FullPath, incoming.IsFolder);

            var currentIndex = _target.IndexOf(existing);
            if (currentIndex != targetIndex)
            {
                _target.Move(currentIndex, targetIndex);
            }
        }
    }

    private FileEntry ToFileEntry(ScanEntry entry)
    {
        return new FileEntry
        {
            FullPath = entry.FullPath,
            DisplayName = entry.DisplayName,
            LastModified = entry.LastModified,
            IsFolder = entry.IsFolder,
            Icon = _iconProvider.GetIcon(entry.FullPath, entry.IsFolder)
        };
    }

    private static string BuildDisplayName(string path, bool isFolder)
    {
        var name = isFolder ? new DirectoryInfo(path).Name : Path.GetFileNameWithoutExtension(path);
        if (string.IsNullOrWhiteSpace(name))
        {
            name = Path.GetFileName(path) ?? path;
        }

        return name.Length <= 24 ? name : $"{name[..23]}...";
    }

    public void Dispose()
    {
        _disposed = true;
        _timer?.Dispose();
    }

    private sealed record ScanEntry(string FullPath, string DisplayName, DateTime LastModified, bool IsFolder);
}
