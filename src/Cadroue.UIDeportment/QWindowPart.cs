namespace Cadroue.UIDeportment;

public sealed class QWindowPart
{
    private const double QWindowPartBorder = 8;

    public QWindowPart(QWindow lOwner)
    {
        QWindowPartDrop = new QWindowDrop(lOwner);
        QWindowPartShortcut = new QWindowShortcut(lOwner);
        lOwner.QWindowStrip.QStripRelayAttach();
        QWindowPartChrome = new QChrome();
        QWindowPartSash = new QSash(QWindowPartBorder);
        QWindowPartConsole = new QConsole();
        QWindowPartConsole.QConsoleWindowAttach(lOwner, lOwner.QWindowStrip);
    }

    public QWindowDrop QWindowPartDrop { get; }

    public QWindowShortcut QWindowPartShortcut { get; }

    public QChrome QWindowPartChrome { get; }

    public QSash QWindowPartSash { get; }

    public QConsole QWindowPartConsole { get; }
}
