using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests.Windows;

public sealed class TViewerLoadSerial
{
    [Fact]
    public void Serial_Advances_StaleResultIgnored()
    {
        QViewer viewer = TInterfaceViewer.TViewerCreate();
        TInterfaceViewer.TViewerCommandSet(viewer, true);

        int first = TInterfaceViewer.TViewerSerialChange(viewer);
        Assert.True(TInterfaceViewer.TViewerSerialCheck(viewer, first));

        int second = TInterfaceViewer.TViewerSerialChange(viewer);

        Assert.Equal(first + 1, second);
        Assert.False(TInterfaceViewer.TViewerSerialCheck(viewer, first));
        Assert.True(TInterfaceViewer.TViewerSerialCheck(viewer, second));
    }

    [Fact]
    public void Serial_CommandInactive_Rejects()
    {
        QViewer viewer = TInterfaceViewer.TViewerCreate();
        int serial = TInterfaceViewer.TViewerSerialChange(viewer);

        Assert.False(TInterfaceViewer.TViewerSerialCheck(viewer, serial));

        TInterfaceViewer.TViewerCommandSet(viewer, true);

        Assert.True(TInterfaceViewer.TViewerSerialCheck(viewer, serial));
    }

    [Fact]
    public void Serial_Unloaded_RejectsForever()
    {
        QViewer viewer = TInterfaceViewer.TViewerCreate();
        TInterfaceViewer.TViewerCommandSet(viewer, true);
        int serial = TInterfaceViewer.TViewerSerialChange(viewer);

        TInterfaceViewer.TViewerUnloadSet(viewer);

        Assert.True(viewer.QViewerUnloaded);
        Assert.False(TInterfaceViewer.TViewerSerialCheck(viewer, serial));
        Assert.False(TInterfaceViewer.TViewerSerialCheck(viewer, TInterfaceViewer.TViewerSerialChange(viewer)));
    }

    [Fact]
    public void MediaClose_AdvancesSerial_ClearsIntent()
    {
        QViewer viewer = TInterfaceViewer.TViewerCreate();
        TInterfaceViewer.TViewerCommandSet(viewer, true);
        TInterfaceViewer.TViewerIntentSet(viewer, "clip.mp4", TimeSpan.FromSeconds(3), true);
        int serial = TInterfaceViewer.TViewerSerialChange(viewer);

        TInterfaceViewer.TViewerMediaClose(viewer);

        Assert.Null(viewer.QViewerIntent);
        Assert.Null(viewer.QViewerSourcePath);
        Assert.False(TInterfaceViewer.TViewerSerialCheck(viewer, serial));
    }
}
