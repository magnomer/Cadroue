using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

[Collection("Preset")]
public sealed class TMergeTabGroups
{
    private const string TMergeFirst = @"C:\media\clip (1).mp4";
    private const string TMergeSecond = @"C:\media\clip (2).mp4";
    private const string TMergeThird = @"C:\media\other.mp4";

    private static (LMergeTab, LDocket) TMergeBuild(LSceneTabRecord? layout = null, string preset = "Alpha")
    {
        LDocket docket = TInterface.TDocketCreate();
        LMergeTab tab = TInterface.TMergeTabCreate(TInterface.TPresetSelectionCreate(preset), docket, layout);
        return (tab, docket);
    }

    [Fact]
    public void LayoutRoundTrip_CarriesAutoStrictAndMode()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (LMergeTab source, _) = TMergeBuild();
        TInterface.TGroupAutoChange(source.LMergeSelection, true);
        TInterface.TGroupStrictChange(source.LMergeSelection, false);

        LSceneTabRecord layout = TInterface.TMergeLayoutRead(source);
        (LMergeTab target, _) = TMergeBuild(layout);

        Assert.True(layout.LSceneGroupAuto);
        Assert.False(layout.LSceneGroupStrict);
        Assert.True(target.LMergeSelection.LGroupAuto);
        Assert.False(target.LMergeSelection.LGroupStrict);
        TInterface.TMergeClose(source);
        TInterface.TMergeClose(target);
    }

    [Fact]
    public void DocketChange_AutoOn_GroupsSeries_AutoOffLeavesGroupsAlone()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (LMergeTab tab, LDocket docket) = TMergeBuild();

        TInterface.TDocketPathsAdd(docket, TMergeFirst, TMergeSecond, TMergeThird);
        Assert.Empty(tab.LMergeGroup.LGroupRecords);

        TInterface.TGroupAutoChange(tab.LMergeSelection, true);
        TInterface.TDocketPathsRemove(docket, TMergeThird);

        Assert.Single(tab.LMergeGroup.LGroupRecords);
        Assert.Equal(2, tab.LMergeGroup.LGroupRecords[0].LGroupRecordPaths.Count);
        TInterface.TMergeClose(tab);
    }

    [Fact]
    public void GroupsRead_SkipsLockedAndEmpty_FiltersByCohort()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (LMergeTab tab, LDocket docket) = TMergeBuild();
        Guid cohort = Guid.NewGuid();
        TInterface.TDocketPathsAdd(docket, TMergeFirst, TMergeSecond);
        TInterface.TDocketDeliveredAdd(docket, TMergeThird, true);
        TInterface.TGroupAdd(tab.LMergeGroup, [TMergeFirst, TMergeSecond], "Pair");
        TInterface.TGroupAdd(tab.LMergeGroup, [TMergeThird], "Locked");

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
        (LMergeTab tab, _) = TMergeBuild(preset: "Missing");
        int missing = 0;
        TInterface.TMergeMissingAttach(tab, () => missing++);

        TInterface.TMergeRun(tab, LWorkPriority.LWorkPriorityNormal);
        int relayed = TInterface.TMergeCohortRun(tab, Guid.NewGuid());

        Assert.Equal(1, missing);
        Assert.Equal(0, relayed);
        TInterface.TMergeClose(tab);
    }
}
