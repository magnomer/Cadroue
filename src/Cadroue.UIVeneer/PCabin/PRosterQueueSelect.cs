using System.Windows.Controls;
using System.Windows.Input;
using Cadroue.Core;

namespace Cadroue.UIVeneer.PCabin;

public sealed partial class PRoster
{
    private void PRosterStepSelect(LWorkItem pWorkItem)
    {
        ModifierKeys pModifiers = Keyboard.Modifiers;
        LRoster.LRosterStepSelect(
            pWorkItem.LWorkId,
            (pModifiers & ModifierKeys.Shift) != 0,
            (pModifiers & ModifierKeys.Control) != 0);
        PRosterCardApply();
        PRosterSelectApply();
        PRosterShadeApply();
        PRosterSelectHandle();
    }

    private void PRosterHoverApply(Guid pId, bool pOver)
    {
        if (LRoster.LRosterSelectedCheck(pId) || !pRosterStepRows.TryGetValue(pId, out Border? pRow))
        {
            return;
        }

        pRow.Background = pOver ? PRosterTheme.PRosterHeaderBrush : PRosterShadeRead(pId);
    }

    private void PRosterSelectApply()
    {
        foreach ((Guid pRowId, Border pRow) in pRosterStepRows)
        {
            pRow.Background = LRoster.LRosterSelectedCheck(pRowId)
                ? PRosterTheme.PRosterSelectBrush
                : PRosterShadeRead(pRowId);
        }
    }

    private LWorkItem? PRosterSelectRead() =>
        LRoster.LRosterCurrentId == Guid.Empty
            ? null
            : pRosterSchedule.LScheduleRecords.FirstOrDefault(
                pWorkItem => pWorkItem.LWorkId == LRoster.LRosterCurrentId);

    private IReadOnlyList<LWorkItem> PRosterSelectionRead() =>
        pRosterSchedule.LScheduleRecords
            .Where(pWorkItem => LRoster.LRosterSelectedCheck(pWorkItem.LWorkId))
            .ToArray();
}
