using Cadroue.Tests.Interface;

using Xunit;

namespace Cadroue.Tests.Engine;

public sealed class TAutopsyResolution
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

        TAutopsyResult lResult = TAutopsy.TAutopsyProseResolve(-22, lProse);

        Assert.Equal("One of the settings for this job is invalid.", lResult.TAutopsyResultSimple);
        Assert.Equal("AVERROR(EINVAL). Configuration error.", lResult.TAutopsyResultTechnical);
        Assert.Equal("Review the encoding settings for this job.", lResult.TAutopsyResultAction);
    }

    [Fact]
    public void Resolve_NoProseReader_YieldsEmptyProse()
    {
        TAutopsyResult lResult = TAutopsy.TAutopsyPlainResolve(-22);

        Assert.Equal(string.Empty, lResult.TAutopsyResultSimple);
        Assert.Equal(string.Empty, lResult.TAutopsyResultTechnical);
        Assert.Null(lResult.TAutopsyResultAction);
    }

    [Fact]
    public void Resolve_SignedKnownCode_MatchesSpineEntry()
    {
        TAutopsyResult lResult = TAutopsy.TAutopsyResolve(-22);

        Assert.True(lResult.TAutopsyResultMatched);
        Assert.Equal(-22, lResult.TAutopsyResultCode);
        Assert.Equal("EINVAL", lResult.TAutopsyResultSymbol);
    }

    [Fact]
    public void Resolve_UnsignedDwordForm_NormalizesToSignedCode()
    {
        TAutopsyResult lSigned = TAutopsy.TAutopsyResolve(-22);
        TAutopsyResult lDword = TAutopsy.TAutopsyResolve(unchecked((int)0xFFFFFFEAu));

        Assert.Equal(lSigned.TAutopsyResultCode, lDword.TAutopsyResultCode);
        Assert.Equal(lSigned.TAutopsyResultMatched, lDword.TAutopsyResultMatched);
        Assert.Equal(lSigned.TAutopsyResultSymbol, lDword.TAutopsyResultSymbol);
    }

    [Fact]
    public void Resolve_NegativeMiss_FallsBackToNegative()
    {
        TAutopsyResult lResult = TAutopsy.TAutopsyResolve(-777777);

        Assert.False(lResult.TAutopsyResultMatched);
        Assert.Equal(-777777, lResult.TAutopsyResultCode);
        Assert.Null(lResult.TAutopsyResultSymbol);
    }

    [Fact]
    public void Resolve_PositiveMiss_FallsBackToPositive()
    {
        TAutopsyResult lResult = TAutopsy.TAutopsyResolve(123456);

        Assert.False(lResult.TAutopsyResultMatched);
        Assert.Equal(123456, lResult.TAutopsyResultCode);
        Assert.Null(lResult.TAutopsyResultSymbol);
    }
}
