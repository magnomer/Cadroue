using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;
using Cadroue.Tests.Interface;

using Xunit;

namespace Cadroue.Tests.Windows;

[Collection("Preset")]
public sealed class TMergeTabGroups
{
    private const string TMergeFirst = @"C:\media\clip (1).mp4";
    private const string TMergeSecond = @"C:\media\clip (2).mp4";
    private const string TMergeThird = @"C:\media\other.mp4";

    private static (QMergeTab, LDocket) TMergeBuild(LSceneTabEntry? layout = null, string preset = "Alpha")
    {
        QList list = TInterfaceList.TListCreate();
        QMergeTab tab = TInterface.TMergeTabCreate(
            TInterface.TPresetSelectionCreate(preset), TInterfaceViewer.TViewerCreate(), list);
        TInterface.TMergeLayoutApply(tab, layout);
        return (tab, list.QListDocket);
    }

    [Fact]
    public void LayoutRoundTrip_CarriesAutoStrictAndMode()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (QMergeTab source, _) = TMergeBuild();
        TInterface.TGroupAutoChange(source.QMergeSelection, true);
        TInterface.TGroupStrictChange(source.QMergeSelection, false);

        LSceneTabEntry layout = TInterface.TMergeLayoutRead(source);
        (QMergeTab target, _) = TMergeBuild(layout);

        Assert.True(layout.LSceneGroupAuto);
        Assert.False(layout.LSceneGroupStrict);
        Assert.True(target.QMergeSelection.LGroupAuto);
        Assert.False(target.QMergeSelection.LGroupStrict);
        TInterface.TMergeClose(source);
        TInterface.TMergeClose(target);
    }

    [Fact]
    public void DocketChange_AutoOn_GroupsSeries_AutoOffLeavesGroupsAlone()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (QMergeTab tab, LDocket docket) = TMergeBuild();

        TInterfaceList.TDocketPathsAdd(docket, TMergeFirst, TMergeSecond, TMergeThird);
        Assert.Empty(tab.QMergeGroup.QGroupRecords);

        TInterface.TGroupAutoChange(tab.QMergeSelection, true);
        TInterfaceList.TDocketPathsRemove(docket, TMergeThird);

        Assert.Single(tab.QMergeGroup.QGroupRecords);
        Assert.Equal(2, tab.QMergeGroup.QGroupRecords[0].QGroupRecordPaths.Count);
        TInterface.TMergeClose(tab);
    }

    [Fact]
    public void GroupsRead_SkipsLockedAndEmpty_FiltersByCohort()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (QMergeTab tab, LDocket docket) = TMergeBuild();
        Guid cohort = Guid.NewGuid();
        TInterfaceList.TDocketPathsAdd(docket, TMergeFirst, TMergeSecond);
        TInterfaceList.TDocketDeliveredAdd(docket, TMergeThird, true);
        TInterface.TGroupAdd(tab.QMergeGroup, [TMergeFirst, TMergeSecond], "Pair");
        TInterface.TGroupAdd(tab.QMergeGroup, [TMergeThird], "Locked");

        IReadOnlyList<LWorkGroup> groups = TInterface.TMergeGroupsRead(tab);
        IReadOnlyList<LWorkGroup> cohortGroups = TInterface.TMergeGroupsRead(tab, cohort);

        Assert.Single(groups);
        Assert.Equal("Pair", groups[0].LWorkGroupName);
        Assert.Empty(cohortGroups);
        Assert.Equal([TMergeFirst, TMergeSecond], TInterface.TMergeEligibleRead(tab));
        Assert.Equal([TMergeFirst, TMergeSecond], TInterface.TMergePathsRead(tab));
        Assert.Equal(2, TInterface.TMergeRelaysRead(tab).Count);
        TInterface.TMergeClose(tab);
    }

    [Fact]
    public void Run_NoPreset_RaisesMissing_CohortRunReturnsZero()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate();
        (QMergeTab tab, _) = TMergeBuild(preset: "Missing");
        int missing = 0;
        TInterface.TMergeMissingAttach(tab, () => missing++);

        TInterface.TMergeRun(tab, LWorkPriority.LWorkPriorityNormal);
        int relayed = TInterface.TMergeCohortRun(tab, Guid.NewGuid());

        Assert.Equal(1, missing);
        Assert.Equal(0, relayed);
        TInterface.TMergeClose(tab);
    }
}
