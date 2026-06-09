using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MediaColor = System.Windows.Media.Color;
using MediaPen = System.Windows.Media.Pen;

namespace FileDock.Services;

public sealed class ShellIconProvider
{
    private const uint ShgfiIcon = 0x000000100;
    private const uint ShgfiLargeIcon = 0x000000000;
    private const uint ShgfiUseFileAttributes = 0x000000010;
    private const uint FileAttributeNormal = 0x00000080;
    private const uint FileAttributeDirectory = 0x00000010;

    private readonly ConcurrentDictionary<string, ImageSource> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly ImageSource _filePlaceholder = CreateFallbackIcon(isFolder: false);
    private readonly ImageSource _folderPlaceholder = CreateFallbackIcon(isFolder: true);

    public ImageSource? GetIcon(string path, bool isFolder)
    {
        var extension = Path.GetExtension(path);
        var key = isFolder
            ? "__folder"
            : string.Equals(extension, ".lnk", StringComparison.OrdinalIgnoreCase)
                ? path
                : extension;
        if (string.IsNullOrWhiteSpace(key))
        {
            key = "__file";
        }

        return _cache.GetOrAdd(key, _ => LoadIcon(path, isFolder));
    }

    public ImageSource GetPlaceholderIcon(bool isFolder) => isFolder ? _folderPlaceholder : _filePlaceholder;

    public Task<ImageSource?> GetIconAsync(string path, bool isFolder)
    {
        var completion = new TaskCompletionSource<ImageSource?>();
        var thread = new Thread(() =>
        {
            try
            {
                completion.SetResult(GetIcon(path, isFolder));
            }
            catch (Exception exception)
            {
                completion.SetException(exception);
            }
        });

        thread.IsBackground = true;
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return completion.Task;
    }

    private static ImageSource LoadIcon(string path, bool isFolder)
    {
        var flags = ShgfiIcon | ShgfiLargeIcon;
        var attributes = isFolder ? FileAttributeDirectory : FileAttributeNormal;

        if (!File.Exists(path) && !Directory.Exists(path))
        {
            flags |= ShgfiUseFileAttributes;
        }

        var info = new ShFileInfo();
        _ = SHGetFileInfo(path, attributes, ref info, (uint)Marshal.SizeOf<ShFileInfo>(), flags);

        if (info.HIcon == IntPtr.Zero)
        {
            return CreateFallbackIcon(isFolder);
        }

        try
        {
            var image = Imaging.CreateBitmapSourceFromHIcon(
                info.HIcon,
                Int32Rect.Empty,
                BitmapSizeOptions.FromWidthAndHeight(64, 64));
            image.Freeze();
            return image;
        }
        finally
        {
            _ = DestroyIcon(info.HIcon);
        }
    }

    private static ImageSource CreateFallbackIcon(bool isFolder)
    {
        var group = new DrawingGroup();
        var brush = new SolidColorBrush(isFolder ? MediaColor.FromRgb(232, 165, 90) : MediaColor.FromRgb(204, 120, 92));
        brush.Freeze();
        var penBrush = new SolidColorBrush(MediaColor.FromRgb(20, 20, 19));
        penBrush.Freeze();
        var pen = new MediaPen(penBrush, 1);
        pen.Freeze();

        var geometry = isFolder
            ? Geometry.Parse("M3,8 L12,8 L14,11 L29,11 L29,26 L3,26 Z")
            : Geometry.Parse("M8,3 L21,3 L28,10 L28,29 L8,29 Z M21,3 L21,10 L28,10");

        group.Children.Add(new GeometryDrawing(brush, pen, geometry));
        group.Freeze();
        return new DrawingImage(group);
    }

    [DllImport("Shell32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SHGetFileInfo(
        string pszPath,
        uint dwFileAttributes,
        ref ShFileInfo psfi,
        uint cbFileInfo,
        uint uFlags);

    [DllImport("User32.dll")]
    private static extern bool DestroyIcon(IntPtr hIcon);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ShFileInfo
    {
        public IntPtr HIcon;
        public int IIcon;
        public uint DwAttributes;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string SzDisplayName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string SzTypeName;
    }
}
