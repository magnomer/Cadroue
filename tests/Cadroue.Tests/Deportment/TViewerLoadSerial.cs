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
}
