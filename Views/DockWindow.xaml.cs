using System.Windows;
using System.Windows.Input;
using FileDock.Models;
using FileDock.Services;
using FileDock.ViewModels;
using Forms = System.Windows.Forms;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;
using WpfPoint = System.Windows.Point;

namespace FileDock.Views;

public partial class DockWindow : Window
{
    private const int SnapThreshold = 20;

    private readonly SettingsService _settingsService;
    private readonly AppSettings _settings;
    private bool _isRestoringBounds;
    private ResizeEdge? _resizeEdge;
    private WpfPoint _resizeStartPoint;
    private double _resizeStartLeft;
    private double _resizeStartTop;
    private double _resizeStartWidth;
    private double _resizeStartHeight;

    public DockWindow(MainViewModel viewModel, SettingsService settingsService, AppSettings settings)
    {
        InitializeComponent();
        DataContext = viewModel;
        _settingsService = settingsService;
        _settings = settings;

        Loaded += (_, _) => RestoreDockBounds();
    }

    public event EventHandler? SettingsRequested;

    public void ShowFromTray()
    {
        Show();
        Topmost = true;
        Activate();
    }

    public void PersistBounds()
    {
        _settings.DockBounds.Left = Left;
        _settings.DockBounds.Top = Top;
        _settings.DockBounds.Width = Width;
        _settings.DockBounds.Height = Height;
        _settingsService.Save(_settings);
    }

    private void RestoreDockBounds()
    {
        _isRestoringBounds = true;
        Left = _settings.DockBounds.Left;
        Top = _settings.DockBounds.Top;
        Width = Math.Max(96, _settings.DockBounds.Width);
        Height = Math.Max(240, _settings.DockBounds.Height);
        _isRestoringBounds = false;
        SnapToNearestEdge(save: false);
    }

    private void TopBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            DragMove();
            SnapToNearestEdge(save: true);
        }
    }

    private void TopBar_MouseUp(object sender, MouseButtonEventArgs e) => SnapToNearestEdge(save: true);

    private void Window_LocationChanged(object? sender, EventArgs e)
    {
        if (!_isRestoringBounds)
        {
            SaveBoundsOnly();
        }
    }

    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (!_isRestoringBounds)
        {
            SaveBoundsOnly();
        }
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e) => SettingsRequested?.Invoke(this, EventArgs.Empty);

    private void HideButton_Click(object sender, RoutedEventArgs e) => Hide();

    private void Window_Deactivated(object? sender, EventArgs e)
    {
        Topmost = false;
    }

    private void ResizeLeft_MouseDown(object sender, MouseButtonEventArgs e) => BeginResize(ResizeEdge.Left, sender, e);
    private void ResizeRight_MouseDown(object sender, MouseButtonEventArgs e) => BeginResize(ResizeEdge.Right, sender, e);
    private void ResizeTop_MouseDown(object sender, MouseButtonEventArgs e) => BeginResize(ResizeEdge.Top, sender, e);
    private void ResizeBottom_MouseDown(object sender, MouseButtonEventArgs e) => BeginResize(ResizeEdge.Bottom, sender, e);

    private void BeginResize(ResizeEdge edge, object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left)
        {
            return;
        }

        _resizeEdge = edge;
        _resizeStartPoint = PointToScreen(e.GetPosition(this));
        _resizeStartLeft = Left;
        _resizeStartTop = Top;
        _resizeStartWidth = Width;
        _resizeStartHeight = Height;

        if (sender is UIElement element)
        {
            element.CaptureMouse();
        }

        e.Handled = true;
    }

    private void ResizeBorder_MouseMove(object sender, WpfMouseEventArgs e)
    {
        if (_resizeEdge is null || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        var current = PointToScreen(e.GetPosition(this));
        var deltaX = current.X - _resizeStartPoint.X;
        var deltaY = current.Y - _resizeStartPoint.Y;

        switch (_resizeEdge)
        {
            case ResizeEdge.Left:
                ApplyLeftResize(deltaX);
                break;
            case ResizeEdge.Right:
                Width = Math.Max(MinWidth, _resizeStartWidth + deltaX);
                break;
            case ResizeEdge.Top:
                ApplyTopResize(deltaY);
                break;
            case ResizeEdge.Bottom:
                Height = Math.Max(MinHeight, _resizeStartHeight + deltaY);
                break;
        }

        SaveBoundsOnly();
        e.Handled = true;
    }

    private void ResizeBorder_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_resizeEdge is null)
        {
            return;
        }

        _resizeEdge = null;
        if (sender is UIElement element)
        {
            element.ReleaseMouseCapture();
        }

        SnapToNearestEdge(save: true);
        e.Handled = true;
    }

    private void ApplyLeftResize(double deltaX)
    {
        var requestedWidth = _resizeStartWidth - deltaX;
        if (requestedWidth <= MinWidth)
        {
            Width = MinWidth;
            Left = _resizeStartLeft + _resizeStartWidth - MinWidth;
            return;
        }

        Width = requestedWidth;
        Left = _resizeStartLeft + deltaX;
    }

    private void ApplyTopResize(double deltaY)
    {
        var requestedHeight = _resizeStartHeight - deltaY;
        if (requestedHeight <= MinHeight)
        {
            Height = MinHeight;
            Top = _resizeStartTop + _resizeStartHeight - MinHeight;
            return;
        }

        Height = requestedHeight;
        Top = _resizeStartTop + deltaY;
    }

    private void SnapToNearestEdge(bool save)
    {
        var area = GetCurrentScreenWorkingAreaInDip();
        var distances = new Dictionary<string, double>
        {
            ["left"] = Math.Abs(Left - area.Left),
            ["top"] = Math.Abs(Top - area.Top),
            ["right"] = Math.Abs(area.Right - (Left + Width)),
            ["bottom"] = Math.Abs(area.Bottom - (Top + Height))
        };

        var nearest = distances.OrderBy(pair => pair.Value).First();
        if (nearest.Value >= SnapThreshold)
        {
            if (save)
            {
                PersistBounds();
            }

            return;
        }

        switch (nearest.Key)
        {
            case "left":
                Left = area.Left;
                break;
            case "top":
                Top = area.Top;
                break;
            case "right":
                Left = area.Right - Width;
                break;
            case "bottom":
                Top = area.Bottom - Height;
                break;
        }

        _settings.SnapEdge = nearest.Key;
        if (save)
        {
            PersistBounds();
        }
    }

    private Rect GetCurrentScreenWorkingAreaInDip()
    {
        var source = PresentationSource.FromVisual(this);
        var toDevice = source?.CompositionTarget?.TransformToDevice ?? System.Windows.Media.Matrix.Identity;
        var fromDevice = source?.CompositionTarget?.TransformFromDevice ?? System.Windows.Media.Matrix.Identity;

        var center = toDevice.Transform(new WpfPoint(Left + Width / 2, Top + Height / 2));
        var screen = Forms.Screen.FromPoint(new System.Drawing.Point((int)center.X, (int)center.Y)).WorkingArea;
        var topLeft = fromDevice.Transform(new WpfPoint(screen.Left, screen.Top));
        var bottomRight = fromDevice.Transform(new WpfPoint(screen.Right, screen.Bottom));
        return new Rect(topLeft, bottomRight);
    }

    private void SaveBoundsOnly()
    {
        _settings.DockBounds.Left = Left;
        _settings.DockBounds.Top = Top;
        _settings.DockBounds.Width = Width;
        _settings.DockBounds.Height = Height;
    }

    private enum ResizeEdge
    {
        Left,
        Right,
        Top,
        Bottom
    }
}
