using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;
using Cadroue.Tests.Interface;

using Xunit;

namespace Cadroue.Tests.Windows;

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
        QList list = TInterfaceList.TListCreate();
        QConvertTab tab = TInterface.TConvertTabCreate(
            TInterface.TPresetSelectionCreate("Missing"), TInterfaceViewer.TViewerCreate(), list);
        int missing = 0;
        TInterface.TConvertMissingAttach(tab, () => missing++);

        TInterface.TConvertRun(tab, true);
        TInterface.TConvertAllRun(tab);
        TInterface.TConvertItemsRun(tab, TTabClip);

        Assert.Equal(3, missing);
    }

    [Fact]
    public void Funnel_Dispatch_OneDeliveryPerMatchedItem_EmptyListDeliversNothing()
    {
        QList list = TInterfaceList.TListCreate();
        LDocket docket = list.QListDocket;
        QFunnelTab tab = TInterfaceFunnel.TFunnelTabCreate(TInterfaceViewer.TViewerCreate(), list);
        QStrip strip = TInterface.TStripCreate(key => key, (name, ordinal) => $"{name} {ordinal}");
        QStripTab self = TInterface.TStripTabCreate("Funnel");
        QStripTab target = TInterface.TStripTabCreate("Convert");
        TInterface.TStripAdd(strip, self);
        TInterface.TStripAdd(strip, target);
        TInterface.TStripWorkspaceAttach(target, TInterface.TPresetInitialCreate("Convert"));
        TInterfaceFunnel.TFunnelStripAttach(tab.QFunnel, strip, self.QStripTabId);
        QFunnelRule rule = TInterfaceFunnel.TFunnelRuleAdd(tab.QFunnel, QFunnelForm.QFunnelFormFilename);
        TInterfaceFunnel.TFunnelTextSet(tab.QFunnel, rule, QFunnelKind.QFunnelKindExtension, "mp4");
        TInterfaceFunnel.TFunnelTargetSelect(tab.QFunnel, rule, 1);
        var delivered = new List<(Guid, string)>();
        TInterface.TMessengerDeliverAttach((tabId, path, _) =>
        {
            delivered.Add((tabId, path));
            return true;
        });
        try
        {
            TInterfaceFunnel.TFunnelAllRun(tab);
            Assert.Empty(delivered);

            TInterfaceList.TDocketPathsAdd(docket, TTabClip, TTabSong);
            TInterfaceFunnel.TFunnelAllRun(tab);
            Assert.Equal([(target.QStripTabId, TTabClip)], delivered);

            TInterfaceList.TListSelect(list, TTabSong);
            TInterfaceFunnel.TFunnelRun(tab);
            Assert.Single(delivered);

            TInterfaceFunnel.TFunnelItemsRun(tab, TTabClip);
            Assert.Equal(2, delivered.Count);
        }
        finally
        {
            TInterface.TMessengerDeliverAttach(null);
            TInterfaceFunnel.TFunnelClose(tab);
        }
    }

    [Fact]
    public void Action_EligibleCheck_NoSourceAllows_DocketUnlockedGates_OverrideWins()
    {
        LDocket docket = TInterfaceList.TDocketCreate();
        QAction action = TInterface.TActionCreate();

        Assert.True(TInterface.TActionEligibleCheck(action));

        TInterface.TActionDocketAttach(action, docket);
        Assert.False(TInterface.TActionEligibleCheck(action));

        TInterfaceList.TDocketPathsAdd(docket, TTabClip);
        TInterfaceList.TDocketDeliveredAdd(docket, TTabSong, true);
        Assert.True(TInterface.TActionEligibleCheck(action));
        Assert.True(TInterface.TActionEligibleCheck(action, TTabClip.ToUpperInvariant()));
        Assert.False(TInterface.TActionEligibleCheck(action, TTabSong));

        TInterface.TActionEligibleAttach(action, () => [TTabSong]);
        Assert.True(TInterface.TActionEligibleCheck(action, TTabSong));
        Assert.False(TInterface.TActionEligibleCheck(action, TTabClip));
    }
}
