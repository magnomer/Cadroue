using System;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

// The viewer's overlay decision: whether the overlay shows, and the record of each change. CViewer makes one per
// viewer.
public sealed class CViewerOverlay
{
    private readonly LViewerPort _cViewerOverlayPort;

    private bool _cViewerOverlayShown;

    internal CViewerOverlay(LViewerPort port)
    {
        ArgumentNullException.ThrowIfNull(port);
        _cViewerOverlayPort = port;
    }

    // The overlay shows while mpv is shown over a visible host with a size. Only a change of the shown state is
    // written: opened when it shows, closed when it hides.
    public bool CViewerOverlayCheck(bool hostVisible, bool mpvShown, double width, double height)
    {
        bool shown = mpvShown && hostVisible && width > 0 && height > 0;
        if (shown && !_cViewerOverlayShown)
        {
            _cViewerOverlayPort.LViewerShownRecord(width, height);
        }
        else if (!shown && _cViewerOverlayShown)
        {
            _cViewerOverlayPort.LViewerHiddenRecord(hostVisible, mpvShown, width, height);
        }

        _cViewerOverlayShown = shown;
        return shown;
    }
}
