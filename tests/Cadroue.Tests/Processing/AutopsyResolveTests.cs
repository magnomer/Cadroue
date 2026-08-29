using Xunit;

namespace Cadroue.Tests;

public sealed class AutopsyResolveTests
{
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
