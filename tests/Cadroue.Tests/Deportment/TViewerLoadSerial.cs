using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

public sealed class TViewerLoadSerial
{
    [Fact]
    public void Serial_Advances_StaleResultIgnored()
    {
        LViewer viewer = TInterface.TViewerCreate();
        TInterface.TViewerCommandSet(viewer, true);

        int first = TInterface.TViewerSerialChange(viewer);
        Assert.True(TInterface.TViewerSerialCheck(viewer, first));

        int second = TInterface.TViewerSerialChange(viewer);

        Assert.Equal(first + 1, second);
        Assert.False(TInterface.TViewerSerialCheck(viewer, first));
        Assert.True(TInterface.TViewerSerialCheck(viewer, second));
    }

    [Fact]
    public void Serial_CommandInactive_Rejects()
    {
        LViewer viewer = TInterface.TViewerCreate();
        int serial = TInterface.TViewerSerialChange(viewer);

        Assert.False(TInterface.TViewerSerialCheck(viewer, serial));

        TInterface.TViewerCommandSet(viewer, true);

        Assert.True(TInterface.TViewerSerialCheck(viewer, serial));
    }

    [Fact]
    public void Serial_Unloaded_RejectsForever()
    {
        LViewer viewer = TInterface.TViewerCreate();
        TInterface.TViewerCommandSet(viewer, true);
        int serial = TInterface.TViewerSerialChange(viewer);

        TInterface.TViewerUnloadSet(viewer);

        Assert.True(viewer.LViewerUnloaded);
        Assert.False(TInterface.TViewerSerialCheck(viewer, serial));
        Assert.False(TInterface.TViewerSerialCheck(viewer, TInterface.TViewerSerialChange(viewer)));
    }

    [Fact]
    public void MediaClose_AdvancesSerial_ClearsIntent()
    {
        LViewer viewer = TInterface.TViewerCreate();
        TInterface.TViewerCommandSet(viewer, true);
        TInterface.TViewerIntentSet(viewer, "clip.mp4", TimeSpan.FromSeconds(3), true);
        int serial = TInterface.TViewerSerialChange(viewer);

        TInterface.TViewerMediaClose(viewer);

        Assert.Null(viewer.LViewerIntent);
        Assert.Null(viewer.LViewerSourcePath);
        Assert.False(TInterface.TViewerSerialCheck(viewer, serial));
    }

    [Fact]
    public void Neutral_ArmOnce_SerialMovesOnCancel()
    {
        LViewer viewer = TInterface.TViewerCreate();
        TInterface.TViewerPlaybackUpdate(viewer, true, TimeSpan.Zero);

        Assert.True(TInterface.TViewerNeutralSet(viewer, Cadroue.Application.LNeutralTarget.LNeutralTargetGrey));
        int armed = viewer.LViewerNeutralSerial;
        Assert.False(TInterface.TViewerNeutralSet(viewer, Cadroue.Application.LNeutralTarget.LNeutralTargetWhite));

        Assert.Equal(armed, viewer.LViewerNeutralSerial);
        Assert.Equal(Cadroue.Application.LNeutralTarget.LNeutralTargetWhite, viewer.LViewerNeutralTarget);
        Assert.Equal(LViewerTool.LViewerToolNeutral, viewer.LViewerTool);
        Assert.True(viewer.LViewerNeutralPlaying);

        Assert.True(TInterface.TViewerNeutralCancel(viewer));
        Assert.Equal(armed + 1, viewer.LViewerNeutralSerial);
        Assert.True(TInterface.TViewerNeutralReset(viewer));
        Assert.Equal(LViewerTool.LViewerToolNone, viewer.LViewerTool);
        Assert.False(TInterface.TViewerNeutralCancel(viewer));
    }
}
