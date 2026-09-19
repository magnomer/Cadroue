using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

public sealed class TDeckEvict
{
    private static LStrip TStripBuild() =>
        TInterface.TStripCreate(key => key, (name, ordinal) => $"{name} {ordinal}");

    private static LStripTab TStripTabAdd(LStrip strip, string key)
    {
        LStripTab tab = TInterface.TStripTabCreate(key);
        TInterface.TStripAdd(strip, tab);
        return tab;
    }

    [Fact]
    public void Select_HidesPreviousAndShowsCurrent()
    {
        LStrip strip = TStripBuild();
        LDeck deck = TInterface.TDeckCreate(strip);
        List<string> log = [];
        TInterface.TDeckAttach(
            deck,
            tab => log.Add("hide " + tab.LStripTabKey),
            tab => log.Add("show " + tab.LStripTabKey),
            () => log.Add("empty " + deck.LDeckEmpty));
        Assert.True(deck.LDeckEmpty);

        LStripTab first = TStripTabAdd(strip, "Split");
        Assert.Equal(["show Split", "empty False"], log);
        Assert.Same(first, deck.LDeckShown);

        log.Clear();
        LStripTab second = TStripTabAdd(strip, "Edit");
        TInterface.TStripSelect(strip, second);
        Assert.Equal(["hide Split", "show Edit", "empty False"], log);

        log.Clear();
        TInterface.TStripSelect(strip, second);
        Assert.Empty(log);

        log.Clear();
        TInterface.TStripRemove(strip, second);
        Assert.Equal(["hide Edit", "show Split", "empty False"], log);

        log.Clear();
        TInterface.TStripRemove(strip, first);
        Assert.Equal(["hide Split", "empty True"], log);
        Assert.True(deck.LDeckEmpty);
        Assert.Null(deck.LDeckShown);
    }

    [Fact]
    public void Close_StopsListening()
    {
        LStrip strip = TStripBuild();
        LDeck deck = TInterface.TDeckCreate(strip);
        int shows = 0;
        TInterface.TDeckAttach(deck, _ => { }, _ => shows++, () => { });

        TInterface.TDeckClose(deck);
        TStripTabAdd(strip, "Split");

        Assert.Equal(0, shows);
    }
}
