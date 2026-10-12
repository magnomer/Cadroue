using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests.Windows;

public sealed class TDeckEvict
{
    private static QStrip TStripBuild() =>
        TInterface.TStripCreate(key => key, (name, ordinal) => $"{name} {ordinal}");

    private static QStripTab TStripTabAdd(QStrip strip, string key)
    {
        QStripTab tab = TInterface.TStripTabCreate(key);
        TInterface.TStripAdd(strip, tab);
        return tab;
    }

    [Fact]
    public void Select_HidesPreviousAndShowsCurrent()
    {
        QStrip strip = TStripBuild();
        QDeck deck = TInterface.TDeckRead(strip);
        List<string> log = [];
        TInterface.TDeckAttach(
            deck,
            tab => log.Add("hide " + tab.QStripTabKey),
            tab => log.Add("show " + tab.QStripTabKey),
            () => log.Add("empty " + deck.QDeckEmpty));
        Assert.True(deck.QDeckEmpty);

        QStripTab first = TStripTabAdd(strip, "Split");
        Assert.Equal(["show Split", "empty False"], log);
        Assert.Same(first, deck.QDeckShown);

        log.Clear();
        QStripTab second = TStripTabAdd(strip, "Edit");
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
        Assert.True(deck.QDeckEmpty);
        Assert.Null(deck.QDeckShown);
    }

    [Fact]
    public void Close_StopsListening()
    {
        QStrip strip = TStripBuild();
        QDeck deck = TInterface.TDeckRead(strip);
        int shows = 0;
        TInterface.TDeckAttach(deck, _ => { }, _ => shows++, () => { });

        TInterface.TDeckClose(deck);
        TStripTabAdd(strip, "Split");

        Assert.Equal(0, shows);
    }
}
