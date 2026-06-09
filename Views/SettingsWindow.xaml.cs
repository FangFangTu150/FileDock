using System.IO;
using System.Windows;
using System.Windows.Input;
using FileDock.Models;
using FileDock.Services;
using FileDock.ViewModels;
using Forms = System.Windows.Forms;

namespace FileDock.Views;

public partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;
    private readonly SettingsService _settingsService;
    private readonly SettingsViewModel _viewModel;

    public SettingsWindow(AppSettings settings, SettingsService settingsService)
    {
        InitializeComponent();
        _settings = settings;
        _settingsService = settingsService;
        _viewModel = new SettingsViewModel(settings);
        DataContext = _viewModel;
    }

    public event EventHandler? SettingsSaved;

    private void WindowChrome_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            DragMove();
        }
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new Forms.FolderBrowserDialog
        {
            Description = "选择 FileDock 要监听的目录",
            UseDescriptionForTitle = true,
            SelectedPath = Directory.Exists(_viewModel.WatchPath) ? _viewModel.WatchPath : string.Empty
        };

        if (dialog.ShowDialog() == Forms.DialogResult.OK)
        {
            _viewModel.WatchPath = dialog.SelectedPath;
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        _settings.WatchPath = _viewModel.WatchPath.Trim();
        _settings.ShowFolders = _viewModel.ShowFolders;
        _settings.StartWithWindows = _viewModel.StartWithWindows;
        _settingsService.ApplyStartupRegistration(_settings.StartWithWindows);
        _settingsService.Save(_settings);
        SettingsSaved?.Invoke(this, EventArgs.Empty);
        Close();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();
}
