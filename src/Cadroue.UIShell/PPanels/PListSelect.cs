using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PList
{
    private readonly HashSet<string> pListPathsSelected = new(StringComparer.OrdinalIgnoreCase);
    private string? pListPathAnchor;
    private string? pListPressPath;

    public IReadOnlyList<string> PListSelectionRead() =>
        pListItems
            .Select(pListItem => pListItem.PListItemPath)
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
            PListItem? pListItem = pListItems.FirstOrDefault(pCandidate => string.Equals(
                pCandidate.PListItemPath, pRowPath, StringComparison.OrdinalIgnoreCase));
            if (pListItem is not null)
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

        pListPathsSelected.Clear();
        for (int pIndex = Math.Min(pAnchorIndex, pRowIndex); pIndex <= Math.Max(pAnchorIndex, pRowIndex); pIndex++)
        {
            pListPathsSelected.Add(pListItems[pIndex].PListItemPath);
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

    private int PListIndexRead(string? pRowPath) =>
        pRowPath is null
            ? -1
            : pListItems.FindIndex(pExisting =>
                string.Equals(pExisting.PListItemPath, pRowPath, StringComparison.OrdinalIgnoreCase));

    private void PListKeyHandle(object pKeySender, KeyEventArgs pKeyEvent)
    {
        if (pKeyEvent.Key != Key.A
            || (Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control
            || pListPathsSelected.Count == 0
            || pListItems.Count == 0)
        {
            return;
        }

        PListSelectionApply(pListItems.Select(pListItem => pListItem.PListItemPath));
        pKeyEvent.Handled = true;
    }
}
