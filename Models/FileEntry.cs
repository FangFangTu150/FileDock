using System.Windows.Media;

namespace FileDock.Models;

public sealed class FileEntry
{
    public required string FullPath { get; set; }
    public required string DisplayName { get; set; }
    public DateTime LastModified { get; set; }
    public ImageSource? Icon { get; set; }
    public bool IsFolder { get; set; }
}
