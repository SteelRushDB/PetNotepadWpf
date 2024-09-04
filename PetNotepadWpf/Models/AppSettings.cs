using System.Windows.Controls;

namespace PetNotepadWpf;

public class OpenFileInfo
{
    public string FilePath { get; set; }
    public string FileExtension { get; set; }
}

public class AppSettings
{
    public string Theme { get; set; }
    public double WindowWidth { get; set; }
    public double WindowHeight { get; set; }
    public bool IsAutoSaveEnabled { get; set; }
    public int AutoSaveInterval { get; set; }
    public List<OpenFileInfo> OpenFiles { get; set; } = new List<OpenFileInfo>();
}