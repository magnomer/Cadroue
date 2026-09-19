namespace Cadroue.UIDeportment;

public sealed class LDeck
{
    private readonly LStrip lStrip;
    private LStripTab? lDeckShown;

    public LDeck(LStrip lStripOwner)
    {
        lStrip = lStripOwner;
        lStrip.LStripSelectChange += LDeckSelectHandle;
    }

    public event Action<LStripTab>? LDeckHide;
    public event Action<LStripTab>? LDeckShow;
    public event Action? LDeckEmptyChange;

    public bool LDeckEmpty => lStrip.LStripSelected is null;

    public LStripTab? LDeckShown => lDeckShown;

    public void LDeckClose() => lStrip.LStripSelectChange -= LDeckSelectHandle;

    private void LDeckSelectHandle(LStripTab? lTab)
    {
        if (lDeckShown is { } lPrevious && !ReferenceEquals(lPrevious, lTab))
        {
            LDeckHide?.Invoke(lPrevious);
        }

        lDeckShown = lTab;
        if (lTab is not null)
        {
            LDeckShow?.Invoke(lTab);
        }

        LDeckEmptyChange?.Invoke();
    }
}
