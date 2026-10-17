using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.Media;

namespace Cadroue.UIDeportment;

// Watches the viewer's cargo and the media availability answer, and tells the face when the panel changes.
// What each notice means is the gate's.
public sealed class QInfo
{
    public event Action? QInfoChanged;

    private readonly CInfo qInfoGate;

    public QInfo(QViewer lViewer, CInfo lInfo)
    {
        qInfoGate = lInfo;
        QInfoViewer = lViewer;
        QInfoAttach();
    }

    public QViewer QInfoViewer { get; }

    private void QInfoAttach()
    {
        QInfoViewer.QViewerMediaChanged += QInfoMediaHandle;
        LMediaProbe.LMediaAvailabilityReady += QInfoAvailabilityHandle;
    }

    public void QInfoDetach() => LMediaProbe.LMediaAvailabilityReady -= QInfoAvailabilityHandle;

    private void QInfoMediaHandle(LCargo lCargo)
    {
        qInfoGate.CInfoProbeDefer(
            lCargo.LCargoSourcePath,
            LMediaProbe.LMediaAvailabilityCurrent,
            () => LMediaProbe.LMediaAvailabilityDefer());
        QInfoChanged?.Invoke();
    }

    private void QInfoAvailabilityHandle(bool lReady) =>
        qInfoGate.CInfoAvailabilityPropagate(
            QInfoViewer.QViewerCargo?.LCargoSourcePath, () => QInfoChanged?.Invoke());
}
