using Cadroue.Core;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using Cadroue.UIShell.PAssets;
using Cadroue.UIShell.PPanels;
using Cadroue.Application;

namespace Cadroue.UIShell.PControlBar;

public sealed class PTabRecord : INotifyPropertyChanged
{
    private static readonly Brush pTabActiveBrush = PTabActiveCreate();

    private static Brush PTabActiveCreate()
    {
        var pBrush = new SolidColorBrush(Color.FromRgb(0x4C, 0x86, 0xF7));
        pBrush.Freeze();
        return pBrush;
    }

    private string pTabTitle = string.Empty;
    private string pTabNameCustom = string.Empty;
    private bool pTabSelectState;
    private bool pTabSeparatorState;
    private bool pTabNameActive;

    public PTabRecord(
        string pTabTitle,
        string pTabLayoutKey,
        string pTabIconPath,
        LPreset? lExportSpecificState = null,
        LSceneTabRecord? lPreferenceTabLayout = null)
    {
        PTabId = Guid.NewGuid();
        PTabTitle = pTabTitle;
        PTabLayoutKey = pTabLayoutKey;
        PTabIconSource = PIcon.PIconRead(pTabIconPath);
        PTabIconActiveSource = PIcon.PIconRead(pTabIconPath, pTabActiveBrush);
        PTabWorkspace = new PWorkspace(pTabLayoutKey, lExportSpecificState, lPreferenceTabLayout);
        if (PTabWorkspace.PWorkspaceSurface.PTabAction is { } pTabAction)
        {
            pTabAction.PActionRelayAttach(PTabId);
        }
    }

    public Guid PTabId { get; }

    public string PTabTitle
    {
        get => pTabTitle;
        set
        {
            if (pTabTitle == value)
            {
                return;
            }

            pTabTitle = value;
            PTabPropertyChange();
        }
    }

    public string PTabNameCustom
    {
        get => pTabNameCustom;
        set
        {
            string pTabTrimmed = (value ?? string.Empty).Trim();
            if (pTabNameCustom == pTabTrimmed)
            {
                return;
            }

            pTabNameCustom = pTabTrimmed;
            PTabPropertyChange();
        }
    }

    public bool PTabNameActive
    {
        get => pTabNameActive;
        set
        {
            if (pTabNameActive == value)
            {
                return;
            }

            pTabNameActive = value;
            PTabPropertyChange();
        }
    }

    public string PTabLayoutKey { get; }

    public int PTabOrdinal { get; set; } = 1;

    public ImageSource PTabIconSource { get; }

    public ImageSource PTabIconActiveSource { get; }

    public PWorkspace PTabWorkspace { get; }

    public bool PTabSelectState
    {
        get => pTabSelectState;
        set
        {
            if (pTabSelectState == value)
            {
                return;
            }

            pTabSelectState = value;
            PTabPropertyChange();
        }
    }

    public bool PTabSeparatorState
    {
        get => pTabSeparatorState;
        set
        {
            if (pTabSeparatorState == value)
            {
                return;
            }

            pTabSeparatorState = value;
            PTabPropertyChange();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void PTabPropertyChange([CallerMemberName] string? pTabPropertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(pTabPropertyName));
    }
}
