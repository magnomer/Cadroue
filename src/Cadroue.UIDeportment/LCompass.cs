namespace Cadroue.UIDeportment;

public sealed class LCompass
{
    private readonly LViewer lCompassViewer;

    public LCompass(LViewer lViewer)
    {
        lCompassViewer = lViewer;
    }

    public bool LCompassPlaying => lCompassViewer.LViewerPlaying;

    public double LCompassVolume => lCompassViewer.LViewerVolume;
}
