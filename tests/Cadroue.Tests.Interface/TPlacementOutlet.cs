using Cadroue.ShellEngine;
using Xunit;

namespace Cadroue.Tests.Interface;

public sealed class TPlacementOutlet
{
    [Fact]
    public void PlacementPreferenceRead_Entries_HandsThemToTheHostRead()
    {
        LPlacementOutlet outlet = new(entries => [(entries[0].Item2, 1, 2, 3, 4)]);

        Assert.Equal([("loupe", 1d, 2d, 3d, 4d)], outlet.LPlacementPreferenceRead([("Loupe", "loupe")]));
    }

    [Fact]
    public void PlacementRecords_EachFailure_WritesItsTraceText()
    {
        LPlacementOutlet outlet = new(_ => []);
        List<string> traces = [];
        InvalidOperationException failure = new("broken");
        Exception? carried = null;
        Action<string, Exception?> errorPrevious = LTraceEngine.LTraceErrorSeam;
        Action<string> infoPrevious = LTraceEngine.LTraceInfoSeam;
        LTraceEngine.LTraceErrorSeam = (message, exception) =>
        {
            traces.Add("error:" + message);
            carried ??= exception;
        };
        LTraceEngine.LTraceInfoSeam = message => traces.Add("info:" + message);
        try
        {
            outlet.LPlacementUnreadableRecord("bad");
            outlet.LPlacementBlockedRecord("loupe");
            outlet.LPlacementUnsavedRecord("loupe");
            outlet.LPlacementCarriedRecord("loupe");
            outlet.LPlacementUncarriedRecord(failure);
        }
        finally
        {
            LTraceEngine.LTraceErrorSeam = errorPrevious;
            LTraceEngine.LTraceInfoSeam = infoPrevious;
        }

        Assert.Contains("error:Subwindow placement catalogue is unreadable: bad", traces);
        Assert.Contains("error:Subwindow placement not saved because its catalogue is unreadable: loupe", traces);
        Assert.Contains("error:Subwindow placement could not be saved: loupe", traces);
        Assert.Contains("info:Subwindow placement carried from preferences: loupe", traces);
        Assert.Contains("error:Subwindow placement could not be carried from preferences", traces);
        Assert.Same(failure, carried);
    }
}
