using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Cadroue.UIShell.PControlBar;

public sealed class PTabRecord : INotifyPropertyChanged
{
    private bool pTabSelectState;
    private bool pTabSeparatorState;

    public PTabRecord(string pTabTitle, string pTabLayoutKey, string pTabIconPath)
    {
        PTabId = Guid.NewGuid();
        PTabTitle = pTabTitle;
        PTabLayoutKey = pTabLayoutKey;
        PTabIconPath = pTabIconPath;
        PTabWorkspace = new PWorkspace(pTabLayoutKey);
    }

    public Guid PTabId { get; }

    public string PTabTitle { get; }

    public string PTabLayoutKey { get; }

    public string PTabIconPath { get; }

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
