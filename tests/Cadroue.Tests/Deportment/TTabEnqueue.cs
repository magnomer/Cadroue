using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

[Collection("Preset")]
public sealed class TTabEnqueue
{
    private const string TTabClip = @"C:\media\clip.mp4";
    private const string TTabSong = @"C:\media\song.wav";

    [Fact]
    public void Convert_NoPreset_RaisesMissingOncePerGesture()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate();
        LDocket docket = TInterface.TDocketCreate();
        LConvertTab tab = TInterface.TConvertTabCreate(
            TInterface.TPresetSelectionCreate("Missing"), TInterface.TListCreate(docket), docket);
        int missing = 0;
        TInterface.TConvertMissingAttach(tab, () => missing++);

        TInterface.TConvertRun(tab, LWorkPriority.LWorkPriorityHigh);
        TInterface.TConvertAllRun(tab);
        TInterface.TConvertItemsRun(tab, TTabClip);

        Assert.Equal(3, missing);
    }

    [Fact]
    public void Funnel_Dispatch_OneDeliveryPerMatchedItem_EmptyListDeliversNothing()
    {
        LDocket docket = TInterface.TDocketCreate();
        LList list = TInterface.TListCreate(docket);
        LFunnelTab tab = TInterface.TFunnelTabCreate(list, docket);
        LStrip strip = TInterface.TStripCreate(key => key, (name, ordinal) => $"{name} {ordinal}");
        LStripTab self = TInterface.TStripTabCreate("Funnel");
        LStripTab target = TInterface.TStripTabCreate("Convert");
        TInterface.TStripAdd(strip, self);
        TInterface.TStripAdd(strip, target);
        TInterface.TStripWorkspaceAttach(
            target, TInterface.TPresetInitialCreate("Convert"), TInterface.TDocketCreate());
        TInterface.TFunnelStripAttach(tab.LFunnel, strip, self.LStripTabId);
        LFunnelRule rule = TInterface.TFunnelRuleAdd(tab.LFunnel, LFunnelForm.LFunnelFormFilename);
        TInterface.TFunnelTextSet(tab.LFunnel, rule, LFunnelKind.LFunnelKindExtension, "mp4");
        TInterface.TFunnelTargetSelect(tab.LFunnel, rule, 1);
        var delivered = new List<(Guid, string)>();
        TInterface.TMessengerDeliverAttach((tabId, path, _) =>
        {
            delivered.Add((tabId, path));
            return true;
        });
        try
        {
            TInterface.TFunnelAllRun(tab);
            Assert.Empty(delivered);

            TInterface.TDocketPathsAdd(docket, TTabClip, TTabSong);
            TInterface.TFunnelAllRun(tab);
            Assert.Equal([(target.LStripTabId, TTabClip)], delivered);

            TInterface.TListSelect(list, TTabSong);
            TInterface.TFunnelRun(tab);
            Assert.Single(delivered);

            TInterface.TFunnelItemsRun(tab, TTabClip);
            Assert.Equal(2, delivered.Count);
        }
        finally
        {
            TInterface.TMessengerDeliverAttach(null);
            TInterface.TFunnelClose(tab);
        }
    }

    [Fact]
    public void Action_EligibleCheck_NoSourceAllows_DocketUnlockedGates_OverrideWins()
    {
        LDocket docket = TInterface.TDocketCreate();
        LAction action = TInterface.TActionCreate();

        Assert.True(TInterface.TActionEligibleCheck(action));

        TInterface.TActionDocketAttach(action, docket);
        Assert.False(TInterface.TActionEligibleCheck(action));

        TInterface.TDocketPathsAdd(docket, TTabClip);
        TInterface.TDocketDeliveredAdd(docket, TTabSong, true);
        Assert.True(TInterface.TActionEligibleCheck(action));
        Assert.True(TInterface.TActionEligibleCheck(action, TTabClip.ToUpperInvariant()));
        Assert.False(TInterface.TActionEligibleCheck(action, TTabSong));

        TInterface.TActionEligibleAttach(action, () => [TTabSong]);
        Assert.True(TInterface.TActionEligibleCheck(action, TTabSong));
        Assert.False(TInterface.TActionEligibleCheck(action, TTabClip));
    }
}
