using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

public sealed class TTokenParse
{
    [Fact]
    public void Parse_SplitsRunsTokensAndOperators()
    {
        IReadOnlyList<LTokenPart> parts = TInterface.TTokenParse("clip_{OriginalName}-{Backspace:3}{Custom} end");

        Assert.Equal(
            [
                LTokenKind.LTokenPlain,
                LTokenKind.LTokenChip,
                LTokenKind.LTokenPlain,
                LTokenKind.LTokenOperator,
                LTokenKind.LTokenChip,
                LTokenKind.LTokenPlain,
            ],
            parts.Select(part => part.LTokenPartKind));
        Assert.Equal("clip_", parts[0].LTokenPartText);
        Assert.Equal("{OriginalName}", parts[1].LTokenPartText);
        Assert.Equal("{Backspace:3}", parts[3].LTokenPartText);
        Assert.Equal("3", parts[3].LTokenPartCount);
        Assert.EndsWith("3", parts[3].LTokenPartLabel);
        Assert.Equal("/PAsset/PPanel/PTokenBackspace.svg", parts[3].LTokenPartIcon);
        Assert.Equal("Custom", parts[4].LTokenPartLabel);
        Assert.Equal(" end", parts[5].LTokenPartText);
    }

    [Fact]
    public void Parse_KeepsAnUnclosedBraceAsRun()
    {
        IReadOnlyList<LTokenPart> parts = TInterface.TTokenParse("a{b");

        Assert.Equal(2, parts.Count);
        Assert.Equal(LTokenKind.LTokenPlain, parts[1].LTokenPartKind);
        Assert.Equal("{b", parts[1].LTokenPartText);
    }

    [Fact]
    public void PartResolve_DefaultsOperatorCountToOne()
    {
        LTokenPart part = TInterface.TTokenPartResolve("{delete}");

        Assert.Equal(LTokenKind.LTokenOperator, part.LTokenPartKind);
        Assert.Equal("1", part.LTokenPartCount);
        Assert.Equal("/PAsset/PPanel/PTokenDelete.svg", part.LTokenPartIcon);
    }

    [Fact]
    public void TextRead_JoinsRunsAndTagsAndTrimsLineEnds()
    {
        string text = TInterface.TTokenTextRead([("ab", null), (null, "{Date}"), (null, null), ("\r\n", null)]);

        Assert.Equal("ab{Date}", text);
    }

    [Fact]
    public void Drop_RaisesInsertOnlyForAToken()
    {
        LToken token = TInterface.TTokenCreate();
        List<string> inserts = [];
        TInterface.TTokenInsertAttach(token, inserts.Add);

        Assert.False(TInterface.TTokenDropHandle(token, null));
        Assert.True(TInterface.TTokenDropHandle(token, "{Suffix}"));
        Assert.Equal(["{Suffix}"], inserts);
    }
}
