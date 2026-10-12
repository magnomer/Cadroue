namespace Cadroue.UIDeportment;

public sealed class QDeck
{
    private readonly QStrip qStrip;
    private QStripTab? qDeckShown;

    internal QDeck(QStrip lStripOwner)
    {
        qStrip = lStripOwner;
        qStrip.QStripSelectChanged += QDeckSelectHandle;
    }

    public event Action<QStripTab>? QDeckHidden;
    public event Action<QStripTab>? QDeckRevealed;
    public event Action? QDeckEmptyChanged;

    public bool QDeckEmpty => qStrip.QStripSelected is null;

    public QStripTab? QDeckShown => qDeckShown;

    public void QDeckClose() => qStrip.QStripSelectChanged -= QDeckSelectHandle;

    private void QDeckSelectHandle(QStripTab? lTab)
    {
        if (qDeckShown is { } lPrevious && !ReferenceEquals(lPrevious, lTab))
        {
            QDeckHidden?.Invoke(lPrevious);
        }

        qDeckShown = lTab;
        if (lTab is not null)
        {
            QDeckRevealed?.Invoke(lTab);
        }

        QDeckEmptyChanged?.Invoke();
    }
}
