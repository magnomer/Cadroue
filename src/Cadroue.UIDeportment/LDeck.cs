namespace Cadroue.UIDeportment;

public sealed class LDeck
{
    private readonly LStrip lStrip;
    private LStripTab? lDeckShown;

    internal LDeck(LStrip lStripOwner)
    {
        lStrip = lStripOwner;
        lStrip.LStripSelectChanged += LDeckSelectHandle;
    }

    public event Action<LStripTab>? LDeckHidden;
    public event Action<LStripTab>? LDeckRevealed;
    public event Action? LDeckEmptyChanged;

    public bool LDeckEmpty => lStrip.LStripSelected is null;

    public LStripTab? LDeckShown => lDeckShown;

    public void LDeckClose() => lStrip.LStripSelectChanged -= LDeckSelectHandle;

    private void LDeckSelectHandle(LStripTab? lTab)
    {
        if (lDeckShown is { } lPrevious && !ReferenceEquals(lPrevious, lTab))
        {
            LDeckHidden?.Invoke(lPrevious);
        }

        lDeckShown = lTab;
        if (lTab is not null)
        {
            LDeckRevealed?.Invoke(lTab);
        }

        LDeckEmptyChanged?.Invoke();
    }
}
