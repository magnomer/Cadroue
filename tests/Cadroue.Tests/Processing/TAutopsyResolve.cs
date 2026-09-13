using Cadroue.ShellEngine;

using Xunit;

namespace Cadroue.Tests;

public sealed class TAutopsyResolve
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

        LAutopsyResult lResult = TAutopsy.TAutopsyProseResolve(-22, lProse);

        Assert.Equal("One of the settings for this job is invalid.", lResult.LAutopsyResultSimple);
        Assert.Equal("AVERROR(EINVAL). Configuration error.", lResult.LAutopsyResultTechnical);
        Assert.Equal("Review the encoding settings for this job.", lResult.LAutopsyResultAction);
    }

    [Fact]
    public void Resolve_NoProseReader_YieldsEmptyProse()
    {
        LAutopsyResult lResult = TAutopsy.TAutopsyPlainResolve(-22);

        Assert.Equal(string.Empty, lResult.LAutopsyResultSimple);
        Assert.Equal(string.Empty, lResult.LAutopsyResultTechnical);
        Assert.Null(lResult.LAutopsyResultAction);
    }

    [Fact]
    public void Resolve_SignedKnownCode_MatchesSpineEntry()
    {
        LAutopsyResult lResult = TAutopsy.TAutopsyResolve(-22);

        Assert.True(lResult.LAutopsyResultMatched);
        Assert.Equal(-22, lResult.LAutopsyResultCode);
        Assert.Equal("EINVAL", lResult.LAutopsyResultSymbol);
    }

    [Fact]
    public void Resolve_UnsignedDwordForm_NormalizesToSignedCode()
    {
        LAutopsyResult lSigned = TAutopsy.TAutopsyResolve(-22);
        LAutopsyResult lDword = TAutopsy.TAutopsyResolve(unchecked((int)0xFFFFFFEAu));

        Assert.Equal(lSigned.LAutopsyResultCode, lDword.LAutopsyResultCode);
        Assert.Equal(lSigned.LAutopsyResultMatched, lDword.LAutopsyResultMatched);
        Assert.Equal(lSigned.LAutopsyResultSymbol, lDword.LAutopsyResultSymbol);
    }

    [Fact]
    public void Resolve_NegativeMiss_FallsBackToNegative()
    {
        LAutopsyResult lResult = TAutopsy.TAutopsyResolve(-777777);

        Assert.False(lResult.LAutopsyResultMatched);
        Assert.Equal(-777777, lResult.LAutopsyResultCode);
        Assert.Null(lResult.LAutopsyResultSymbol);
    }

    [Fact]
    public void Resolve_PositiveMiss_FallsBackToPositive()
    {
        LAutopsyResult lResult = TAutopsy.TAutopsyResolve(123456);

        Assert.False(lResult.LAutopsyResultMatched);
        Assert.Equal(123456, lResult.LAutopsyResultCode);
        Assert.Null(lResult.LAutopsyResultSymbol);
    }
}
