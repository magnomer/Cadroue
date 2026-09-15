using Cadroue.Application;
using Cadroue.Infrastructure;
using Cadroue.ShellEngine;

namespace Cadroue.UIShell.PToolbar;

public sealed partial class PStrip
{
    public bool PStripContentClear()
    {
        bool pStripCleared = false;
        IReadOnlySet<Guid> pStripActiveBatches = LBastion.LBastionCohortsRead();
        foreach (PTabRecord pTabRecord in PStripRecords)
        {
            pStripCleared |= pTabRecord.PTabWorkspace.PWorkspaceMediaClear(pStripActiveBatches);
        }

        LTraceLog.LTraceInfoRecord($"Tabs cleared across {PStripRecords.Count} tab(s)");
        return pStripCleared;
    }

    public bool PStripBusyCheck(PTabRecord? pTabRecord = null) =>
        pTabRecord is null
            ? PStripRecords.Any(pTabItem => pTabItem.PTabWorkspace.PWorkspaceSurface.PTabBusyCheck())
            : pTabRecord.PTabWorkspace.PWorkspaceSurface.PTabBusyCheck();

    public bool PStripCloseConfirm(System.Windows.Window? pOwner, PTabRecord? pTabRecord = null)
    {
        if (!PStripBusyCheck(pTabRecord))
        {
            return true;
        }

        bool pStripConfirmed = PSCasement.PSAlert.PSAlertConfirm(
            pOwner,
            LLocalization.LLocalizationTextRead("Tab.Close.BusyTitle"),
            LLocalization.LLocalizationTextRead(
                pTabRecord is null ? "Tab.Close.BusyAllMessage" : "Tab.Close.BusyMessage"),
            LLocalization.LLocalizationTextRead("Terms.Stop"));
        if (!pStripConfirmed)
        {
            LTraceLog.LTraceInfoRecord("Close declined: a worklist is still working");
        }

        return pStripConfirmed;
    }

    public void PStripClose(PTabRecord pTabRecord)
    {
        var pTabIndex = PStripRecords.IndexOf(pTabRecord);
        if (pTabIndex < 0)
        {
            return;
        }

        var pTabWasSelected = ReferenceEquals(PStripSelected, pTabRecord);
        if (ReferenceEquals(pStripHovered, pTabRecord))
        {
            pStripHovered = null;
        }

        string pTabClosedTitle = pTabRecord.PTabTitle;
        pTabRecord.PTabWorkspace.PWorkspaceClose();
        LCartographer.LCartographerTabRemove(pTabRecord.PTabId);
        PStripRecords.RemoveAt(pTabIndex);
        PStripTitleUpdate();
        LTraceLog.LTraceInfoRecord($"Tab closed '{pTabClosedTitle}': {PStripRecords.Count} tab(s) open");

        if (!pTabWasSelected)
        {
            PStripSeparatorUpdate();
            return;
        }

        if (PStripRecords.Count == 0)
        {
            PStripSelected = null;
            return;
        }

        var pTabNextIndex = LTabset.LTabsetNextResolve(PStripRecords.Count, pTabIndex);
        PStripSelect(PStripRecords[pTabNextIndex]);
    }

    public void PStripAllClose()
    {
        foreach (PTabRecord pTabRecord in PStripRecords)
        {
            pTabRecord.PTabWorkspace.PWorkspaceClose();
            LCartographer.LCartographerTabRemove(pTabRecord.PTabId);
        }

        pStripHovered = null;
        PStripRecords.Clear();
        PStripSelected = null;
        LTraceLog.LTraceInfoRecord("All tabs closed");
    }
}
