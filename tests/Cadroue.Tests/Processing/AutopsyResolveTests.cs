using Cadroue.ShellEngine;

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

        LAutopsyProseReader? lPrevious = LAutopsy.LAutopsyProse;
        try
        {
            LAutopsy.LAutopsyProse = (string lKey, out string lValue) => lProse.TryGetValue(lKey, out lValue!);

            LAutopsyResult lResult = LAutopsy.LAutopsyResolve(-22, string.Empty);

            Assert.Equal("One of the settings for this job is invalid.", lResult.LAutopsyResultSimple);
            Assert.Equal("AVERROR(EINVAL). Configuration error.", lResult.LAutopsyResultTechnical);
            Assert.Equal("Review the encoding settings for this job.", lResult.LAutopsyResultAction);
        }
        finally
        {
            LAutopsy.LAutopsyProse = lPrevious;
        }
    }

    [Fact]
    public void Resolve_NoProseReader_YieldsEmptyProse()
    {
        LAutopsyProseReader? lPrevious = LAutopsy.LAutopsyProse;
        try
        {
            LAutopsy.LAutopsyProse = null;

            LAutopsyResult lResult = LAutopsy.LAutopsyResolve(-22, string.Empty);

            Assert.Equal(string.Empty, lResult.LAutopsyResultSimple);
            Assert.Equal(string.Empty, lResult.LAutopsyResultTechnical);
            Assert.Null(lResult.LAutopsyResultAction);
        }
        finally
        {
            LAutopsy.LAutopsyProse = lPrevious;
        }
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
