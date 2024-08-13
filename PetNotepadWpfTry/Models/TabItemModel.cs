using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Documents;

namespace PetNotepadWpf;

public class TabItemModel
{
    public string Header { get; set; }
    public FlowDocument Content { get; set; }
    public bool IsPlaceholder { get; set; }
}