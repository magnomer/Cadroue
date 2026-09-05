using System.Windows.Controls;
using System.Windows.Input;
using Cadroue.Core;

namespace Cadroue.UIShell.PDeck;

public sealed partial class PRoster
{
    private readonly HashSet<Guid> pRosterSelectedIds = new();
    private Guid pRosterCurrentId;

    private void PRosterStepSelect(LWorkItem pWorkItem)
    {
        Guid pId = pWorkItem.LWorkId;
        pRosterCardId = Guid.Empty;
        PRosterCardApply();
        ModifierKeys pModifiers = Keyboard.Modifiers;

        if ((pModifiers & ModifierKeys.Shift) != 0 && pRosterCurrentId != Guid.Empty)
        {
            int pAnchor = pRosterOrderedIds.IndexOf(pRosterCurrentId);
            int pTarget = pRosterOrderedIds.IndexOf(pId);
            if (pAnchor >= 0 && pTarget >= 0)
            {
                pRosterSelectedIds.Clear();
                for (int pIndex = Math.Min(pAnchor, pTarget); pIndex <= Math.Max(pAnchor, pTarget); pIndex++)
                {
                    pRosterSelectedIds.Add(pRosterOrderedIds[pIndex]);
                }
            }
        }
        else if ((pModifiers & ModifierKeys.Control) != 0)
        {
            if (!pRosterSelectedIds.Add(pId))
            {
                pRosterSelectedIds.Remove(pId);
            }

            pRosterCurrentId = pId;
        }
        else
        {
            pRosterSelectedIds.Clear();
            pRosterSelectedIds.Add(pId);
            pRosterCurrentId = pId;
        }

        PRosterSelectApply();
        PRosterShadeApply();
        PRosterSelectHandle();
    }

    private void PRosterHoverApply(Guid pId, bool pOver)
    {
        if (pRosterSelectedIds.Contains(pId) || !pRosterStepRows.TryGetValue(pId, out Border? pRow))
        {
            return;
        }

        pRow.Background = pOver ? PRosterTheme.PRosterHeaderBrush : PRosterShadeRead(pId);
    }

    private void PRosterSelectApply()
    {
        foreach ((Guid pRowId, Border pRow) in pRosterStepRows)
        {
            pRow.Background = pRosterSelectedIds.Contains(pRowId)
                ? PRosterTheme.PRosterSelectBrush
                : PRosterShadeRead(pRowId);
        }
    }

    private LWorkItem? PRosterSelectRead() =>
        pRosterCurrentId == Guid.Empty
            ? null
            : pRosterSchedule.LScheduleRecords.FirstOrDefault(pWorkItem => pWorkItem.LWorkId == pRosterCurrentId);

    private IReadOnlyList<LWorkItem> PRosterSelectionRead() =>
        pRosterSchedule.LScheduleRecords
            .Where(pWorkItem => pRosterSelectedIds.Contains(pWorkItem.LWorkId))
            .ToArray();
}
