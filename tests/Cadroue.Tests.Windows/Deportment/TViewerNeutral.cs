using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests.Windows;

public sealed class TViewerNeutral
{
    private const string TNeutralSource = @"C:\media\missing.mp4";

    private static (QViewer, QViewerNeutral) TNeutralBuild()
    {
        QViewer viewer = TInterfaceViewer.TViewerCreate();
        return (viewer, TInterfaceViewer.TViewerNeutralRead(viewer));
    }

    private static void TNeutralMediaSet(QViewer viewer)
    {
        LCargo cargo = TInterfaceViewer.TCargoCreate(
            TNeutralSource, TInterfaceViewer.TViewerInfoCreate(TimeSpan.FromSeconds(60), 1920, 1080), true);
        TInterfaceViewer.TViewerMediaCommit(viewer, cargo, false);
        TInterfaceCrop.TCropSizeHandle(TInterfaceCrop.TCropRead(viewer), 800, 450);
    }

    [Fact]
    public void ToolSet_ArmOnce_RetargetsInPlace_SerialMovesOnCancel()
    {
        (QViewer viewer, QViewerNeutral neutral) = TNeutralBuild();
        TInterfaceViewer.TViewerPlaybackUpdate(viewer, true, TimeSpan.Zero);
        var tools = new List<(bool, LNeutralTarget)>();
        int focuses = 0;
        TInterfaceViewer.TViewerToolAttach(neutral, (armed, target) => tools.Add((armed, target)));
        TInterfaceViewer.TViewerFocusAttach(neutral, () => focuses++);

        TInterfaceViewer.TViewerToolSet(neutral, true, LNeutralTarget.LNeutralTargetGrey);
        int armed = neutral.QViewerNeutralSerial;
        TInterfaceViewer.TViewerToolSet(neutral, true, LNeutralTarget.LNeutralTargetWhite);

        Assert.Equal(armed, neutral.QViewerNeutralSerial);
        Assert.Equal(LNeutralTarget.LNeutralTargetWhite, neutral.QViewerNeutralTarget);
        Assert.Equal(QViewerTool.QViewerToolNeutral, neutral.QViewerTool);
        Assert.True(neutral.QViewerNeutralArmed);
        Assert.Equal(1, focuses);

        TInterfaceViewer.TViewerNeutralCancel(neutral);

        Assert.Equal(armed + 1, neutral.QViewerNeutralSerial);
        Assert.Equal(QViewerTool.QViewerToolNone, neutral.QViewerTool);
        Assert.Equal([(true, LNeutralTarget.LNeutralTargetGrey), (false, LNeutralTarget.LNeutralTargetWhite)], tools);

        TInterfaceViewer.TViewerNeutralCancel(neutral);
        Assert.Equal(armed + 1, neutral.QViewerNeutralSerial);
    }

    [Fact]
    public void KeyHandle_EscapeWhileArmed_Cancels()
    {
        (_, QViewerNeutral neutral) = TNeutralBuild();

        Assert.False(TInterfaceViewer.TViewerKeyHandle(neutral, "Escape"));

        TInterfaceViewer.TViewerToolSet(neutral, true, LNeutralTarget.LNeutralTargetGrey);

        Assert.False(TInterfaceViewer.TViewerKeyHandle(neutral, "Enter"));
        Assert.True(neutral.QViewerNeutralArmed);
        Assert.True(TInterfaceViewer.TViewerKeyHandle(neutral, "Escape"));
        Assert.False(neutral.QViewerNeutralArmed);
    }

    [Fact]
    public void Press_OutsideVideo_StaysArmed()
    {
        (QViewer viewer, QViewerNeutral neutral) = TNeutralBuild();
        TNeutralMediaSet(viewer);
        TInterfaceViewer.TViewerToolSet(neutral, true, LNeutralTarget.LNeutralTargetGrey);

        TInterfaceViewer.TViewerPressHandle(neutral, -5, 10);
        TInterfaceViewer.TViewerPressHandle(neutral, 10, 900);

        Assert.True(neutral.QViewerNeutralArmed);
    }

    [Fact]
    public async Task Press_InsideVideo_EndsToolAndReportsDecodeFailure()
    {
        (QViewer viewer, QViewerNeutral neutral) = TNeutralBuild();
        TNeutralMediaSet(viewer);
        var captures = new List<bool>();
        var samples = new List<LNeutralSample>();
        TInterfaceCrop.TCropCaptureAttach(TInterfaceCrop.TCropDragRead(viewer), captures.Add);
        TInterfaceViewer.TViewerNeutralAttach(neutral, samples.Add);
        TInterfaceViewer.TViewerToolSet(neutral, true, LNeutralTarget.LNeutralTargetWhite);

        Assert.True(TInterfaceCrop.TCropPressHandle(TInterfaceCrop.TCropDragRead(viewer), 400, 225));

        Assert.False(neutral.QViewerNeutralArmed);
        Assert.Equal([false], captures);
        for (int wait = 0; wait < 50 && samples.Count == 0; wait++)
        {
            await Task.Delay(20);
        }

        Assert.Single(samples);
        Assert.Equal(LNeutralOutcome.LNeutralOutcomeDecode, samples[0].LNeutralOutcome);
    }

    [Fact]
    public void EstimateStart_NoMedia_RaisesEmptyWheelOnce()
    {
        (_, QViewerNeutral neutral) = TNeutralBuild();
        var wheels = new List<LNeutralWheel>();
        TInterfaceViewer.TViewerEstimateAttach(neutral, wheels.Add);

        TInterfaceViewer.TViewerEstimateStart(neutral, LWhitebalanceMethod.LWhitebalanceMethodAverage);

        Assert.Single(wheels);
        Assert.False(wheels[0].LNeutralWheelPresent);
    }
}
