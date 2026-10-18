using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

public sealed class QWindowPart
{
    private const double QWindowPartBorder = 8;

    public QWindowPart(QWindow lOwner, CConsole lConsole)
    {
        QWindowPartEnvoy = new QEnvoy();
        QWindowPartDrop = new QWindowDrop(lOwner);
        QWindowPartShortcut = new QWindowShortcut(lOwner);
        lOwner.QWindowStrip.QStripRelay.QStripRelayAttach();
        QWindowPartChrome = new QChrome();
        QWindowPartSash = new QSash(QWindowPartBorder);
        QWindowPartConsole = new QConsole(lConsole, QWindowPartEnvoy, lOwner.QWindowScene, lOwner.QWindowStrip);
    }

    public QEnvoy QWindowPartEnvoy { get; }

    public QWindowDrop QWindowPartDrop { get; }

    public QWindowShortcut QWindowPartShortcut { get; }

    public QChrome QWindowPartChrome { get; }

    public QSash QWindowPartSash { get; }

    public QConsole QWindowPartConsole { get; }
}
