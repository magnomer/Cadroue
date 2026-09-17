namespace Cadroue.UIDeportment;

public sealed class LCompass
{
    private readonly LViewer lCompassViewer;
    private bool lCompassProgramValue;

    public LCompass(LViewer lViewer)
    {
        lCompassViewer = lViewer;
    }

    public bool LCompassPlaying => lCompassViewer.LViewerPlaying;

    public double LCompassVolume => lCompassViewer.LViewerVolume;

    public bool LCompassProgramValue => lCompassProgramValue;

    public void LCompassProgramSet(bool lProgramValue) => lCompassProgramValue = lProgramValue;
}
