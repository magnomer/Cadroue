using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.Media;

namespace Cadroue.UIDeportment;

// Watches the viewer's cargo and the media availability answer, and tells the face when the panel changes.
// What each notice means is the gate's.
public sealed class QInfo
{
    public event Action? QInfoChanged;

    public QInfo(QViewer lViewer)
    {
        QInfoViewer = lViewer;
        lViewer.QViewerMediaChanged += QInfoMediaHandle;
        LMediaProbe.LMediaAvailabilityReady += QInfoAvailabilityHandle;
    }

    public static CInfo QInfoGateSeam { get; set; } = null!;

    public QViewer QInfoViewer { get; }

    public void QInfoDetach() => LMediaProbe.LMediaAvailabilityReady -= QInfoAvailabilityHandle;

    private void QInfoMediaHandle(LCargo lCargo)
    {
        QInfoGateSeam.CInfoProbeDefer(
            lCargo.LCargoSourcePath,
            LMediaProbe.LMediaAvailabilityCurrent,
            () => LMediaProbe.LMediaAvailabilityDefer());
        QInfoChanged?.Invoke();
    }

    private void QInfoAvailabilityHandle(bool lReady) =>
        QInfoGateSeam.CInfoAvailabilityPropagate(
            QInfoViewer.QViewerCargo?.LCargoSourcePath, () => QInfoChanged?.Invoke());
}
