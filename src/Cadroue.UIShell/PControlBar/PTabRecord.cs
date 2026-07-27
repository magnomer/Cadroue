using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using Cadroue.UIShell.PPanels;

namespace Cadroue.UIShell.PControlBar;

public sealed class PTabRecord : INotifyPropertyChanged
{
    private string pTabTitle = string.Empty;
    private bool pTabSelectState;
    private bool pTabSeparatorState;

    public PTabRecord(
        string pTabTitle,
        string pTabLayoutKey,
        ImageSource pTabIconSource,
        LExportSpecificState? lExportSpecificState = null)
    {
        PTabId = Guid.NewGuid();
        PTabTitle = pTabTitle;
        PTabLayoutKey = pTabLayoutKey;
        PTabIconSource = pTabIconSource;
        PTabWorkspace = new PWorkspace(pTabLayoutKey, lExportSpecificState);
    }

    public Guid PTabId { get; }

    /// <summary>
    /// Display title. Owned by <see cref="LTabset"/>, which renumbers every tab
    /// whenever the set changes, so this must notify.
    /// </summary>
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

    public string PTabLayoutKey { get; }

    /// <summary>
    /// Number shown after the layout key when more than one tab of this kind exists.
    /// Assigned once at creation and owned by <see cref="LTabset"/>; it follows the tab
    /// rather than its position, so reordering does not renumber anything.
    /// </summary>
    public int PTabOrdinal { get; set; } = 1;

    public ImageSource PTabIconSource { get; }

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
