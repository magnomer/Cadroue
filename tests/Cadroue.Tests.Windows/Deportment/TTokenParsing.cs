using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests.Windows;

public sealed class TTokenParsing
{
    [Fact]
    public void Parse_SplitsRunsTokensAndOperators()
    {
        IReadOnlyList<QTokenPart> parts = TInterface.TTokenParse("clip_{OriginalName}-{Backspace:3}{Custom} end");

        Assert.Equal(
            [
                QTokenKind.QTokenPlain,
                QTokenKind.QTokenChip,
                QTokenKind.QTokenPlain,
                QTokenKind.QTokenOperator,
                QTokenKind.QTokenChip,
                QTokenKind.QTokenPlain,
            ],
            parts.Select(part => part.QTokenPartKind));
        Assert.Equal("clip_", parts[0].QTokenPartText);
        Assert.Equal("{OriginalName}", parts[1].QTokenPartText);
        Assert.Equal("{Backspace:3}", parts[3].QTokenPartText);
        Assert.Equal("3", parts[3].QTokenPartCount);
        Assert.EndsWith("3", parts[3].QTokenPartLabel);
        Assert.Equal("/PAsset/PPanel/PTokenBackspace.svg", parts[3].QTokenPartIcon);
        Assert.Equal("Custom", parts[4].QTokenPartLabel);
        Assert.Equal(" end", parts[5].QTokenPartText);
    }

    [Fact]
    public void Parse_KeepsAnUnclosedBraceAsRun()
    {
        IReadOnlyList<QTokenPart> parts = TInterface.TTokenParse("a{b");

        Assert.Equal(2, parts.Count);
        Assert.Equal(QTokenKind.QTokenPlain, parts[1].QTokenPartKind);
        Assert.Equal("{b", parts[1].QTokenPartText);
    }

    [Fact]
    public void PartResolve_DefaultsOperatorCountToOne()
    {
        QTokenPart part = TInterface.TTokenPartResolve("{delete}");

        Assert.Equal(QTokenKind.QTokenOperator, part.QTokenPartKind);
        Assert.Equal("1", part.QTokenPartCount);
        Assert.Equal("/PAsset/PPanel/PTokenDelete.svg", part.QTokenPartIcon);
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
        QToken token = TInterface.TTokenCreate();
        List<string> inserts = [];
        TInterface.TTokenInsertAttach(token, inserts.Add);

        Assert.False(TInterface.TTokenDropHandle(token, null));
        Assert.True(TInterface.TTokenDropHandle(token, "{Suffix}"));
        Assert.Equal(["{Suffix}"], inserts);
    }
}
