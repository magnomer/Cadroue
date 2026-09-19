using Cadroue.Core;
using System.Collections.ObjectModel;
using Cadroue.UIVeneer.PAsset;
using Cadroue.UIVeneer.PCabin;
using Cadroue.UIVeneer.PWing;
using Cadroue.Application;
using Cadroue.ShellEngine;
using Cadroue.UIDeportment;

using Cadroue.Infrastructure;

namespace Cadroue.UIVeneer.PPorch;

public sealed partial class PStrip
{
    private const string pStripSplitIcon = "/PAsset/PTab/PSplitButton.svg";
    private const string pStripEditIcon = "/PAsset/PTab/PEditButton.svg";
    private const string pStripFixIcon = "/PAsset/PTab/PFixButton.svg";
    private const string pStripAudioIcon = "/PAsset/PTab/PAudioButton.svg";
    private const string pStripConvertIcon = "/PAsset/PTab/PConvertButton.svg";
    private const string pStripMergeIcon = "/PAsset/PTab/PMergeButton.svg";
    private const string pStripFunnelIcon = "/PAsset/PTab/PFunnelButton.svg";
    private const string pStripWorklistIcon = "/PAsset/PTab/PWorklistButton.svg";

    public PStrip()
    {
        PStripRecords = new ObservableCollection<PTabRecord>();
        LStrip = new LStrip(
            PStripTitleRead,
            (pStripName, pStripOrdinal) =>
                LLocalization.LLocalizationFormat("Tab.Numbered", pStripName, pStripOrdinal));
        LStrip.LStripChange += PStripOrderHandle;
        LStrip.LStripTabChange += PStripTabHandle;
        LStrip.LStripSelectChange += PStripSelectHandle;
        LStrip.LStripTitleChange += PStripRelayUpdate;
        LStrip.LStripRelayAttach();
        PStripCurrent = this;
    }

    public static PStrip? PStripCurrent { get; private set; }

    public LStrip LStrip { get; }

    public ObservableCollection<PTabRecord> PStripRecords { get; }

    public static string PStripTitleRead(Guid pStripTabId) =>
        PStripTabFind(pStripTabId)?.PTabTitle ?? LCartographer.LCartographerStageRead(pStripTabId);

    public PTabRecord? PStripSelected => PStripRecordFind(LStrip.LStripSelected);

    public event Action<PTabRecord?>? PStripSelectChange;

    private PTabRecord? PStripRecordFind(LStripTab? lStripTab) =>
        lStripTab is null
            ? null
            : PStripRecords.FirstOrDefault(pTabRecord => ReferenceEquals(pTabRecord.LStripTab, lStripTab));

    private void PStripSelectHandle(LStripTab? lStripTab) =>
        PStripSelectChange?.Invoke(PStripRecordFind(lStripTab));

    private void PStripTabHandle(LStripTab lStripTab) => PStripRecordFind(lStripTab)?.PTabUpdate();

    private void PStripOrderHandle()
    {
        int pStripCount = Math.Min(LStrip.LStripTabs.Count, PStripRecords.Count);
        for (int pStripIndex = 0; pStripIndex < pStripCount; pStripIndex++)
        {
            PTabRecord? pTabRecord = PStripRecordFind(LStrip.LStripTabs[pStripIndex]);
            if (pTabRecord is null)
            {
                continue;
            }

            int pStripCurrent = PStripRecords.IndexOf(pTabRecord);
            if (pStripCurrent != pStripIndex)
            {
                PStripRecords.Move(pStripCurrent, pStripIndex);
            }
        }
    }

    public void PStripUpdateSuspend() => LStrip.LStripUpdateSuspend();

    public void PStripUpdateResume() => LStrip.LStripUpdateResume();

    internal void PStripHoverSet(PTabRecord pTabRecord) => LStrip.LStripHoverSet(pTabRecord.LStripTab);

    internal void PStripHoverClear(PTabRecord? pTabRecord = null) => LStrip.LStripHoverClear(pTabRecord?.LStripTab);

    public PTabRecord PStripAdd()
    {
        return PStripAdd("Split");
    }

    public PTabRecord PStripAdd(
        string pTabLayoutKey,
        LPreset? lExportSpecificState = null,
        LSceneTabRecord? lPreferenceTabLayout = null)
    {
        string pTabIconPath = pTabLayoutKey switch
        {
            "Edit" => pStripEditIcon,
            "Fix" => pStripFixIcon,
            "Audio" => pStripAudioIcon,
            "Convert" => pStripConvertIcon,
            "Merge" => pStripMergeIcon,
            "Funnel" => pStripFunnelIcon,
            "Worklist" => pStripWorklistIcon,
            _ => pStripSplitIcon
        };
        string pTabKey = pTabLayoutKey switch
        {
            "Edit" or "Fix" or "Audio" or "Convert" or "Merge" or "Funnel" or "Worklist" => pTabLayoutKey,
            _ => "Split"
        };

        var lStripTab = new LStripTab(pTabKey);
        var pTabRecord = new PTabRecord(lStripTab, pTabIconPath, lExportSpecificState, lPreferenceTabLayout);
        PStripRecords.Add(pTabRecord);
        LStrip.LStripAdd(lStripTab);
        LTraceLog.LTraceInfoRecord(
            $"Tab opened '{pTabRecord.PTabTitle}' ({pTabKey}): {PStripRecords.Count} tab(s) open");
        return pTabRecord;
    }

    public void PStripTitleUpdate() => LStrip.LStripTitleUpdate();

    public static void PStripRelayUpdate()
    {
        if (PStripCurrent is not { } pStripTabset)
        {
            return;
        }

        foreach (PTabRecord pTabRecord in pStripTabset.PStripRecords)
        {
            pTabRecord.PTabWorkspace.PWorkspaceSurface.PTabAction?.PActionRelayApply(
                LCartographer.LCartographerTargetRead(pTabRecord.PTabId));
        }
    }

    public static IReadOnlyList<PActionRelayOption> PStripRelayRead(Guid pStripSourceTab)
    {
        if (PStripCurrent is not { } pStripTabset)
        {
            return Array.Empty<PActionRelayOption>();
        }

        var pStripOptions = new List<PActionRelayOption>();
        foreach (PTabRecord pTabRecord in pStripTabset.PStripRecords)
        {
            if (pTabRecord.PTabId == pStripSourceTab
                || pTabRecord.PTabWorkspace.PWorkspaceSurface.PTabList is null)
            {
                continue;
            }

            pStripOptions.Add(new PActionRelayOption(
                pTabRecord.PTabId, pTabRecord.PTabTitle, pTabRecord.PTabIconSource));
        }

        return pStripOptions;
    }

    public static PTabRecord? PStripTabFind(LWorkItem lWorkItem)
    {
        Guid pStripSourceTab = lWorkItem.LWorkRelaySource;
        if (pStripSourceTab == Guid.Empty)
        {
            return null;
        }

        if (LCartographerPlanStore.LCartographerPlanRead(lWorkItem.LWorkBatchId, out LCartographerPlanRecord pStripPlan)
            && pStripPlan.LCartographerStages.FirstOrDefault(
                pStripStage => pStripStage.LCartographerStageId == pStripSourceTab) is { } pStripSourceStage)
        {
            pStripSourceTab = pStripSourceStage.LCartographerOriginalTab;
        }

        return PStripTabFind(pStripSourceTab);
    }

    public static PTabRecord? PStripTabFind(Guid pStripTabId) =>
        PStripCurrent?.PStripRecords.FirstOrDefault(pTabRecord => pTabRecord.PTabId == pStripTabId);

    public void PStripNameSet(PTabRecord pTabRecord, string pTabName)
    {
        bool pStripHadCustom = pTabRecord.PTabNameCustom.Length > 0;
        if (LStrip.LStripNameSet(pTabRecord.LStripTab, pTabName))
        {
            LTraceLog.LTraceInfoRecord($"Tab renamed to '{pTabRecord.PTabTitle}' ({pTabRecord.PTabLayoutKey})");
        }
        else if (pStripHadCustom)
        {
            LTraceLog.LTraceInfoRecord($"Tab name reset to the standard name for {pTabRecord.PTabLayoutKey}");
        }
    }

    private static string PStripTitleRead(string pTabLayoutKey) =>
        LLocalization.LLocalizationTextRead(pTabLayoutKey switch
        {
            "Edit" => "Tab.Edit",
            "Fix" => "Tab.Fix",
            "Audio" => "Tab.Audio",
            "Convert" => "Tab.Convert",
            "Merge" => "Tab.Merge",
            "Funnel" => "Tab.Funnel",
            "Worklist" => "Tab.Worklist",
            _ => "Tab.Split"
        });

    public void PStripSelect(PTabRecord? pTabRecord) => LStrip.LStripSelect(pTabRecord?.LStripTab);

    public void PStripMove(PTabRecord pTabRecord, int pTabTargetIndex) =>
        LStrip.LStripMove(pTabRecord.LStripTab, pTabTargetIndex);
}
