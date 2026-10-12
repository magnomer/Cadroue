namespace Cadroue.UIDeportment;

public sealed class QWindowPart
{
    private const double QWindowPartBorder = 8;

    public QWindowPart(LWindow lOwner)
    {
        QWindowPartDrop = new LWindowDrop(lOwner);
        QWindowPartShortcut = new LWindowShortcut(lOwner);
        lOwner.LWindowStrip.LStripRelayAttach();
        QWindowPartChrome = new LChrome();
        QWindowPartSash = new LSash(QWindowPartBorder);
        QWindowPartConsole = new LConsole();
        QWindowPartConsole.LConsoleWindowAttach(lOwner, lOwner.LWindowStrip);
    }

    public LWindowDrop QWindowPartDrop { get; }

    public LWindowShortcut QWindowPartShortcut { get; }

    public LChrome QWindowPartChrome { get; }

    public LSash QWindowPartSash { get; }

    public LConsole QWindowPartConsole { get; }
}
