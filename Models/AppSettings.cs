namespace FileDock.Models;

public sealed class AppSettings
{
    public string WatchPath { get; set; } = string.Empty;
    public bool ShowFolders { get; set; }
    public bool StartWithWindows { get; set; }
    public DockBounds DockBounds { get; set; } = new();
    public string SnapEdge { get; set; } = "right";
    public List<string> FileSnapshot { get; set; } = [];
}

public sealed class DockBounds
{
    public double Left { get; set; } = 0;
    public double Top { get; set; } = 60;
    public double Width { get; set; } = 116;
    public double Height { get; set; } = 760;
}
