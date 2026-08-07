using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.Application;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PList
{
    private readonly HashSet<string> pListPathsSelected = new(StringComparer.OrdinalIgnoreCase);
    private string? pListPathAnchor;
    private string? pListPressPath;

    public IReadOnlyList<string> PListSelectionRead() =>
        pListDocket.LDocketPathsRead()
            .Where(pListPathsSelected.Contains)
            .ToArray();

    private bool PListSelectionCheck(string pRowPath) => pListPathsSelected.Contains(pRowPath);

    private void PListSelectApply(string? pSelectPath)
    {
        pListPathsSelected.Clear();
        if (pSelectPath is not null)
        {
            pListPathsSelected.Add(pSelectPath);
        }

        pListPathAnchor = pSelectPath;
        PListCurrentApply(pSelectPath);
    }

    private void PListCurrentApply(string? pCurrentPath)
    {
        pListPathCurrent = pCurrentPath;
        PListSelectionUpdate();
        PListPathChange?.Invoke(pListPathCurrent);
        PListLockChange?.Invoke(PListLockCheck());
    }

    private void PListSelectionUpdate()
    {
        foreach ((string pRowPath, Border pRowBorder) in pListRows)
        {
            if (pListDocket.LDocketItemFind(pRowPath) is { } pListItem)
            {
                pRowBorder.Background = PListBackgroundRead(pListItem);
            }
        }
    }

    private void PListPressHandle(string pRowPath, MouseButtonEventArgs pRowEvent)
    {
        pListPressPath = null;

        if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
        {
            PListRangeApply(pRowPath);
            return;
        }

        if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            PListSelectionToggle(pRowPath);
            return;
        }

        if (PListSelectionCheck(pRowPath) && pListPathsSelected.Count > 1)
        {
            pListPressPath = pRowPath;
            pListPathAnchor = pRowPath;
            PListCurrentApply(pRowPath);
            return;
        }

        PListSelectApply(pRowPath);
    }

    private void PListReleaseHandle()
    {
        if (pListPressPath is { } pCollapsePath)
        {
            PListSelectApply(pCollapsePath);
        }

        pListPressPath = null;
    }

    private void PListSelectionToggle(string pRowPath)
    {
        pListPathAnchor = pRowPath;
        if (pListPathsSelected.Add(pRowPath))
        {
            PListCurrentApply(pRowPath);
            return;
        }

        pListPathsSelected.Remove(pRowPath);
        PListCurrentApply(pListPathCurrent is not null && pListPathsSelected.Contains(pListPathCurrent)
            ? pListPathCurrent
            : PListSelectionRead().LastOrDefault());
    }

    private void PListRangeApply(string pRowPath)
    {
        int pRowIndex = PListIndexRead(pRowPath);
        if (pRowIndex < 0)
        {
            return;
        }

        int pAnchorIndex = PListIndexRead(pListPathAnchor ?? pListPathCurrent);
        if (pAnchorIndex < 0)
        {
            pAnchorIndex = pRowIndex;
        }

        IReadOnlyList<string> pPaths = pListDocket.LDocketPathsRead();
        pListPathsSelected.Clear();
        for (int pIndex = Math.Min(pAnchorIndex, pRowIndex); pIndex <= Math.Max(pAnchorIndex, pRowIndex); pIndex++)
        {
            pListPathsSelected.Add(pPaths[pIndex]);
        }

        PListCurrentApply(pRowPath);
    }

    private void PListSelectionApply(IEnumerable<string> pSelectPaths)
    {
        pListPathsSelected.Clear();
        foreach (string pSelectPath in pSelectPaths)
        {
            pListPathsSelected.Add(pSelectPath);
        }

        PListCurrentApply(pListPathCurrent is not null && pListPathsSelected.Contains(pListPathCurrent)
            ? pListPathCurrent
            : PListSelectionRead().LastOrDefault());
    }

    private int PListIndexRead(string? pRowPath)
    {
        if (pRowPath is null)
        {
            return -1;
        }

        IReadOnlyList<string> pPaths = pListDocket.LDocketPathsRead();
        for (int pIndex = 0; pIndex < pPaths.Count; pIndex++)
        {
            if (string.Equals(pPaths[pIndex], pRowPath, StringComparison.OrdinalIgnoreCase))
            {
                return pIndex;
            }
        }

        return -1;
    }

    private void PListKeyHandle(object pKeySender, KeyEventArgs pKeyEvent)
    {
        IReadOnlyList<string> pPaths = pListDocket.LDocketPathsRead();
        if (pKeyEvent.Key != Key.A
            || (Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control
            || pListPathsSelected.Count == 0
            || pPaths.Count == 0)
        {
            return;
        }

        PListSelectionApply(pPaths);
        pKeyEvent.Handled = true;
    }
}
