using Cadroue.Core;
using System.ComponentModel;
using System.Windows.Media;
using Cadroue.UIVeneer.PAsset;
using Cadroue.UIVeneer.PPanel;
using Cadroue.Application;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PToolbar;

public sealed class PTabRecord : INotifyPropertyChanged
{
    private static readonly Brush pTabActiveBrush = PTabActiveCreate();

    private static Brush PTabActiveCreate()
    {
        var pBrush = new SolidColorBrush(Color.FromRgb(0x4C, 0x86, 0xF7));
        pBrush.Freeze();
        return pBrush;
    }

    public PTabRecord(
        LStripTab lStripTab,
        string pTabIconPath,
        LPreset? lExportSpecificState = null,
        LSceneTabRecord? lPreferenceTabLayout = null)
    {
        LStripTab = lStripTab;
        PTabIconSource = PIcon.PIconRead(pTabIconPath);
        PTabIconActive = PIcon.PIconRead(pTabIconPath, pTabActiveBrush);
        PTabWorkspace = new PWorkspace(lStripTab.LStripTabKey, lExportSpecificState, lPreferenceTabLayout);
        if (PTabWorkspace.PWorkspaceSurface.PTabAction is { } pTabAction)
        {
            pTabAction.PActionRelayAttach(PTabId);
        }
    }

    public LStripTab LStripTab { get; }

    public Guid PTabId => LStripTab.LStripTabId;

    public string PTabLayoutKey => LStripTab.LStripTabKey;

    public string PTabTitle => LStripTab.LStripTabTitle;

    public string PTabNameCustom => LStripTab.LStripTabCustom;

    public bool PTabNameActive => LStripTab.LStripTabEditing;

    public bool PTabSelectState => LStripTab.LStripTabSelected;

    public bool PTabSeparatorState => LStripTab.LStripTabSeparator;

    public bool PTabRelayState
    {
        get => !PTabWorkspace.PWorkspaceRoot.IsEnabled;
        set => PTabWorkspace.PWorkspaceRoot.IsEnabled = !value;
    }

    public ImageSource PTabIconSource { get; }

    public ImageSource PTabIconActive { get; }

    public PWorkspace PTabWorkspace { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    internal void PTabUpdate() =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
}
