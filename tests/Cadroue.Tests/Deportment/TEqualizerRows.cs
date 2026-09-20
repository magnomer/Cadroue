using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

public sealed class TEqualizerRows
{
    [Fact]
    public void Rows_MirrorBands_WithIndexAndText()
    {
        LEqualizer equalizer = TInterface.TEqualizerCreate();
        IReadOnlyList<LEqualizerBand> rows = equalizer.LEqualizerRows;

        Assert.Equal(TInterface.TEqualizerBandsRead(equalizer).Count, rows.Count);
        Assert.Equal(Enumerable.Range(0, rows.Count), rows.Select(row => row.LEqualizerBandIndex));
        Assert.Equal(TInterface.TEqualizerFrequencyRead(equalizer, 0), rows[0].LEqualizerBandFrequency);
    }

    [Fact]
    public void RowsChange_OnlyWhenCountChanges()
    {
        LEqualizer equalizer = TInterface.TEqualizerCreate();
        int rows = 0;
        int changes = 0;
        TInterface.TEqualizerRowsAttach(equalizer, () => rows++);
        TInterface.TEqualizerAttach(equalizer, () => changes++);

        TInterface.TEqualizerGainSet(equalizer, 0, 3);
        Assert.Equal(0, rows);
        Assert.Equal(1, changes);

        TInterface.TEqualizerBandAdd(equalizer);
        Assert.Equal(1, rows);
        TInterface.TEqualizerBandRemove(equalizer, 0);
        Assert.Equal(2, rows);
        Assert.Equal(3, changes);
    }

    [Fact]
    public void FrequencyCommit_ParsesAndEchoes_OneChange()
    {
        LEqualizer equalizer = TInterface.TEqualizerCreate();
        int changes = 0;
        TInterface.TEqualizerAttach(equalizer, () => changes++);

        string echo = TInterface.TEqualizerFrequencyCommit(equalizer, 1, "2500");
        Assert.Equal("2500", echo);
        Assert.Equal(2500, TInterface.TEqualizerBandsRead(equalizer)[1].LWorkBandFrequency);
        Assert.Equal(1, changes);

        string kept = TInterface.TEqualizerFrequencyCommit(equalizer, 1, "abc");
        Assert.Equal("2500", kept);
        Assert.Equal(1, changes);
        Assert.Equal(2500, TInterface.TEqualizerBandsRead(equalizer)[1].LWorkBandFrequency);
    }

    [Fact]
    public void Choice_SelectByIndex_ThenGainEditBreaksMatch()
    {
        LEqualizer equalizer = TInterface.TEqualizerCreate();
        IReadOnlyList<string> tokens = TInterface.TContourTokensRead();

        TInterface.TEqualizerChoiceSelect(equalizer, 1);
        Assert.Equal(tokens[1], TInterface.TEqualizerTokenRead(equalizer));
        Assert.Equal(1, TInterface.TEqualizerChoiceRead(equalizer).LInspectorChoiceIndex);

        TInterface.TEqualizerGainSet(equalizer, 0, TInterface.TEqualizerGainRead(equalizer, 0) + 1);
        LInspectorChoice choice = TInterface.TEqualizerChoiceRead(equalizer);
        Assert.Equal(choice.LInspectorChoiceCustom, choice.LInspectorChoiceIndex);
        Assert.Equal(tokens[1], TInterface.TEqualizerTokenRead(equalizer));
    }

    [Fact]
    public void ChoiceResolve_SkipsCustom_AndAlreadyMatched()
    {
        string[] tokens = { "A", "B" };

        Assert.Null(TInterface.TInspectorChoiceResolve(tokens, -1, "A", "A"));
        Assert.Null(TInterface.TInspectorChoiceResolve(tokens, 2, "A", "A"));
        Assert.Null(TInterface.TInspectorChoiceResolve(tokens, 0, "A", "A"));
        Assert.Equal("B", TInterface.TInspectorChoiceResolve(tokens, 1, "A", "A"));
        Assert.Equal("A", TInterface.TInspectorChoiceResolve(tokens, 0, "A", null));

        LInspectorChoice drifted = TInterface.TInspectorChoiceRead(tokens, token => token, "A", null);
        Assert.Equal(2, drifted.LInspectorChoiceIndex);
        Assert.Equal(3, drifted.LInspectorChoiceNames.Count);
        LInspectorChoice matched = TInterface.TInspectorChoiceRead(tokens, token => token, "A", "B");
        Assert.Equal(1, matched.LInspectorChoiceIndex);
    }
}
