using Xunit;

namespace Cadroue.Tests;

public sealed class AutopsyResolveTests
{
    [Fact]
    public void Resolve_WiredProse_FlowsSimpleTechnicalAction()
    {
        var lProse = new Dictionary<string, string>
        {
            ["-22.simple"] = "One of the settings for this job is invalid.",
            ["-22.technical"] = "AVERROR(EINVAL). Configuration error.",
            ["-22.action"] = "Review the encoding settings for this job.",
        };

        (string Simple, string Technical, string? Action) lResult = TAutopsy.ResolveWithProse(-22, lProse);

        Assert.Equal("One of the settings for this job is invalid.", lResult.Simple);
        Assert.Equal("AVERROR(EINVAL). Configuration error.", lResult.Technical);
        Assert.Equal("Review the encoding settings for this job.", lResult.Action);
    }

    [Fact]
    public void Resolve_NoProseReader_YieldsEmptyProse()
    {
        (string Simple, string Technical, string? Action) lResult = TAutopsy.ResolveWithoutProse(-22);

        Assert.Equal(string.Empty, lResult.Simple);
        Assert.Equal(string.Empty, lResult.Technical);
        Assert.Null(lResult.Action);
    }

    [Fact]
    public void Resolve_SignedKnownCode_MatchesSpineEntry()
    {
        (int code, bool matched, string? symbol) = TAutopsy.Resolve(-22);

        Assert.True(matched);
        Assert.Equal(-22, code);
        Assert.Equal("EINVAL", symbol);
    }

    [Fact]
    public void Resolve_UnsignedDwordForm_NormalizesToSignedCode()
    {
        (int code, bool matched, string? symbol) signed = TAutopsy.Resolve(-22);
        (int code, bool matched, string? symbol) dword = TAutopsy.Resolve(unchecked((int)0xFFFFFFEAu));

        Assert.Equal(signed.code, dword.code);
        Assert.Equal(signed.matched, dword.matched);
        Assert.Equal(signed.symbol, dword.symbol);
    }

    [Fact]
    public void Resolve_NegativeMiss_FallsBackToNegative()
    {
        (int code, bool matched, string? symbol) = TAutopsy.Resolve(-777777);

        Assert.False(matched);
        Assert.Equal(-777777, code);
        Assert.Null(symbol);
    }

    [Fact]
    public void Resolve_PositiveMiss_FallsBackToPositive()
    {
        (int code, bool matched, string? symbol) = TAutopsy.Resolve(123456);

        Assert.False(matched);
        Assert.Equal(123456, code);
        Assert.Null(symbol);
    }
}
