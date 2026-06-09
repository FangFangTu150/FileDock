using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using FileDock.Models;
using FileDock.ViewModels;
using WpfUserControl = System.Windows.Controls.UserControl;
using MediaBrush = System.Windows.Media.Brush;
using MediaBrushes = System.Windows.Media.Brushes;
using MediaColor = System.Windows.Media.Color;

namespace FileDock.Views;

public partial class FileCard : WpfUserControl
{
    public static readonly DependencyProperty EntryProperty =
        DependencyProperty.Register(nameof(Entry), typeof(FileEntry), typeof(FileCard), new PropertyMetadata(null));

    public static readonly DependencyProperty OpenFileCommandProperty =
        DependencyProperty.Register(nameof(OpenFileCommand), typeof(ICommand), typeof(FileCard), new PropertyMetadata(null));

    public static readonly DependencyProperty OpenLocationCommandProperty =
        DependencyProperty.Register(nameof(OpenLocationCommand), typeof(ICommand), typeof(FileCard), new PropertyMetadata(null));

    public static readonly DependencyProperty CopyFileCommandProperty =
        DependencyProperty.Register(nameof(CopyFileCommand), typeof(ICommand), typeof(FileCard), new PropertyMetadata(null));

    public FileCard()
    {
        InitializeComponent();
    }

    public FileEntry? Entry
    {
        get => (FileEntry?)GetValue(EntryProperty);
        set => SetValue(EntryProperty, value);
    }

    public ICommand? OpenFileCommand
    {
        get => (ICommand?)GetValue(OpenFileCommandProperty);
        set => SetValue(OpenFileCommandProperty, value);
    }

    public ICommand? OpenLocationCommand
    {
        get => (ICommand?)GetValue(OpenLocationCommandProperty);
        set => SetValue(OpenLocationCommandProperty, value);
    }

    public ICommand? CopyFileCommand
    {
        get => (ICommand?)GetValue(CopyFileCommandProperty);
        set => SetValue(CopyFileCommandProperty, value);
    }

    private void OnMouseDoubleClick(object sender, MouseButtonEventArgs e) => Execute(OpenFileCommand);

    private void OpenMenuItem_Click(object sender, RoutedEventArgs e) => Execute(OpenFileCommand);

    private void OpenLocationMenuItem_Click(object sender, RoutedEventArgs e) => Execute(OpenLocationCommand);

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        if (ExecuteCopy())
        {
            AnimateCopySuccess();
        }

        e.Handled = true;
    }

    private void Execute(ICommand? command)
    {
        if (Entry is not null && command?.CanExecute(Entry) == true)
        {
            command.Execute(Entry);
        }
    }

    private bool ExecuteCopy()
    {
        if (Entry is null || CopyFileCommand is null)
        {
            return false;
        }

        if (CopyFileCommand is IResultCommand<FileEntry> resultCommand)
        {
            return resultCommand.TryExecute(Entry);
        }

        if (!CopyFileCommand.CanExecute(Entry))
        {
            return false;
        }

        CopyFileCommand.Execute(Entry);
        return true;
    }

    private void AnimateCopySuccess()
    {
        var brush = new SolidColorBrush(MediaColor.FromRgb(93, 184, 114));
        CopyButton.Background = brush;
        CopyButton.Foreground = MediaBrushes.White;

        var animation = new ColorAnimation
        {
            To = MediaColor.FromRgb(250, 249, 245),
            Duration = TimeSpan.FromSeconds(3),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        animation.Completed += (_, _) => CopyButton.Foreground = (MediaBrush)System.Windows.Application.Current.Resources["MutedBrush"];
        brush.BeginAnimation(SolidColorBrush.ColorProperty, animation);
    }
}
