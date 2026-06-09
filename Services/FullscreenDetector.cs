using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using ThreadingTimer = System.Threading.Timer;

namespace FileDock.Services;

public sealed class FullscreenDetector : IDisposable
{
    private readonly ThreadingTimer _timer;
    private bool _lastState;

    public FullscreenDetector()
    {
        _timer = new ThreadingTimer(_ => Detect(), null, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2));
    }

    public event EventHandler<bool>? FullscreenChanged;

    private void Detect()
    {
        var isFullscreen = IsForegroundWindowFullscreen();
        if (isFullscreen == _lastState)
        {
            return;
        }

        _lastState = isFullscreen;
        FullscreenChanged?.Invoke(this, isFullscreen);
    }

    private static bool IsForegroundWindowFullscreen()
    {
        var handle = GetForegroundWindow();
        if (handle == IntPtr.Zero || !GetWindowRect(handle, out var rect))
        {
            return false;
        }

        var screen = Screen.FromHandle(handle).Bounds;
        const int tolerance = 2;
        return Math.Abs(rect.Left - screen.Left) <= tolerance
               && Math.Abs(rect.Top - screen.Top) <= tolerance
               && Math.Abs(rect.Right - screen.Right) <= tolerance
               && Math.Abs(rect.Bottom - screen.Bottom) <= tolerance;
    }

    public void Dispose() => _timer.Dispose();

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out NativeRect rect);

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct NativeRect
    {
        public readonly int Left;
        public readonly int Top;
        public readonly int Right;
        public readonly int Bottom;
    }
}
