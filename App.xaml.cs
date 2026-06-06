using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using FileDock.Models;
using FileDock.Services;
using FileDock.ViewModels;
using FileDock.Views;
using Hardcodet.Wpf.TaskbarNotification;
using WpfApplication = System.Windows.Application;
using WpfContextMenu = System.Windows.Controls.ContextMenu;
using WpfMenuItem = System.Windows.Controls.MenuItem;
using WpfSeparator = System.Windows.Controls.Separator;
using MediaColor = System.Windows.Media.Color;

namespace FileDock;

public partial class App : WpfApplication
{
    private const string MutexName = "FileDock.SingleInstance";
    private const string ShowEventName = "FileDock.ShowWindow";

    private Mutex? _mutex;
    private EventWaitHandle? _showEvent;
    private RegisteredWaitHandle? _showRegistration;
    private SettingsService? _settingsService;
    private AppSettings? _settings;
    private MainViewModel? _mainViewModel;
    private FileWatcherService? _watcher;
    private FullscreenDetector? _fullscreenDetector;
    private DockWindow? _dockWindow;
    private TaskbarIcon? _trayIcon;
    private bool _allowExit;
    private bool _ownsMutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        _mutex = new Mutex(initiallyOwned: true, MutexName, out var createdNew);
        _ownsMutex = createdNew;
        if (!createdNew)
        {
            using var existingEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowEventName);
            existingEvent.Set();
            Shutdown();
            return;
        }

        base.OnStartup(e);

        _showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowEventName);
        _showRegistration = ThreadPool.RegisterWaitForSingleObject(
            _showEvent,
            (_, _) => Dispatcher.Invoke(ShowDockFromTray),
            null,
            Timeout.InfiniteTimeSpan,
            executeOnlyOnce: false);

        _settingsService = new SettingsService();
        _settings = _settingsService.Load();
        _mainViewModel = new MainViewModel();
        _mainViewModel.ApplySettings(_settings);

        var iconProvider = new ShellIconProvider();
        _watcher = new FileWatcherService(_settings, _mainViewModel.FileList, Dispatcher, iconProvider);

        var snapshotEntries = _settings.FileSnapshot
            .Where(path => File.Exists(path) || Directory.Exists(path))
            .Select(_watcher.CreateEntryFromPath)
            .OrderByDescending(entry => entry.LastModified)
            .ToList();
        _mainViewModel.LoadSnapshot(snapshotEntries);

        _dockWindow = new DockWindow(_mainViewModel, _settingsService, _settings);
        _dockWindow.SettingsRequested += (_, _) => ShowSettings();
        _dockWindow.Closing += DockWindow_Closing;
        _dockWindow.Show();

        _trayIcon = CreateTrayIcon();
        _fullscreenDetector = new FullscreenDetector();
        _fullscreenDetector.FullscreenChanged += (_, isFullscreen) => _watcher.SetPaused(isFullscreen);
        _watcher.Start();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        SaveSnapshot();
        _trayIcon?.Dispose();
        _fullscreenDetector?.Dispose();
        _watcher?.Dispose();
        _showRegistration?.Unregister(null);
        _showEvent?.Dispose();
        if (_ownsMutex)
        {
            _mutex?.ReleaseMutex();
        }
        _mutex?.Dispose();
        base.OnExit(e);
    }

    private TaskbarIcon CreateTrayIcon()
    {
        var icon = new TaskbarIcon
        {
            ToolTipText = "FileDock",
            IconSource = CreateTrayImage(),
            ContextMenu = CreateTrayMenu()
        };

        icon.TrayLeftMouseUp += (_, _) => ToggleDock();

        return icon;
    }

    private WpfContextMenu CreateTrayMenu()
    {
        var menu = new WpfContextMenu();

        var showItem = new WpfMenuItem { Header = "显示 Dock" };
        showItem.Click += (_, _) => ShowDockFromTray();

        var settingsItem = new WpfMenuItem { Header = "设置" };
        settingsItem.Click += (_, _) => ShowSettings();

        var exitItem = new WpfMenuItem { Header = "退出" };
        exitItem.Click += (_, _) => ExitApplication();

        menu.Items.Add(showItem);
        menu.Items.Add(settingsItem);
        menu.Items.Add(new WpfSeparator());
        menu.Items.Add(exitItem);
        return menu;
    }

    private static ImageSource CreateTrayImage()
    {
        var group = new DrawingGroup();
        var coral = new SolidColorBrush(MediaColor.FromRgb(204, 120, 92));
        var ink = new SolidColorBrush(MediaColor.FromRgb(20, 20, 19));
        coral.Freeze();
        ink.Freeze();

        group.Children.Add(new GeometryDrawing(coral, null, new RectangleGeometry(new Rect(2, 2, 28, 28), 8, 8)));
        group.Children.Add(new GeometryDrawing(ink, null, Geometry.Parse("M8,15 H24 V18 H8 Z M15,8 H18 V24 H15 Z")));
        group.Freeze();
        return new DrawingImage(group);
    }

    private void ToggleDock()
    {
        ShowDockFromTray();
    }

    private void ShowDock()
    {
        if (_dockWindow is null)
        {
            return;
        }

        _dockWindow.Show();
        _dockWindow.Activate();
    }

    private void ShowDockFromTray()
    {
        if (_dockWindow is null)
        {
            return;
        }

        _dockWindow.ShowFromTray();
    }

    private void ShowSettings()
    {
        if (_settings is null || _settingsService is null || _mainViewModel is null || _watcher is null)
        {
            return;
        }

        var window = new SettingsWindow(_settings, _settingsService)
        {
            Owner = _dockWindow
        };
        window.SettingsSaved += (_, _) =>
        {
            _mainViewModel.ApplySettings(_settings);
            _watcher.UpdateSettings(_settings);
        };
        window.ShowDialog();
    }

    private void DockWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_allowExit)
        {
            SaveSnapshot();
            return;
        }

        e.Cancel = true;
        _dockWindow?.Hide();
        SaveSnapshot();
    }

    private void ExitApplication()
    {
        _allowExit = true;
        SaveSnapshot();
        Shutdown();
    }

    private void SaveSnapshot()
    {
        if (_settings is null || _settingsService is null || _mainViewModel is null)
        {
            return;
        }

        _settings.FileSnapshot = _mainViewModel.FileList.Select(entry => entry.FullPath).ToList();
        _dockWindow?.PersistBounds();
        _settingsService.Save(_settings);
    }
}
