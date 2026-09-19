using Xunit;

namespace Cadroue.Tests;

public sealed class TSectionHex
{
    [Fact]
    public void Normalize_AcceptsSixOrEightDigits_AndUppercases()
    {
        Assert.Equal("#4A90D9", TInterface.TSectionHexNormalize("#4a90d9"));
        Assert.Equal("#4A90D9", TInterface.TSectionHexNormalize("#FF4A90D9"));
        Assert.Equal("#123456", TInterface.TSectionHexNormalize(" #123456 "));
        Assert.Null(TInterface.TSectionHexNormalize("red"));
        Assert.Null(TInterface.TSectionHexNormalize("#12345"));
        Assert.Null(TInterface.TSectionHexNormalize("#12345G"));
        Assert.Null(TInterface.TSectionHexNormalize(string.Empty));
    }

    [Fact]
    public void ActiveRead_WrapsTheIndex_AndFallsBackToTheDefaultSet()
    {
        Assert.Equal(10, TInterface.TSectionCountRead());
        Assert.Equal("#4A90D9", TInterface.TSectionColorRead(0));
        Assert.Equal("#4A90D9", TInterface.TSectionColorRead(10));
        Assert.Equal("#7F8C8D", TInterface.TSectionColorRead(-1));
        Assert.Equal(TInterface.TSectionColorsRead("Cadroue"), TInterface.TSectionColorsRead("Nope"));
        Assert.Equal("#5B7C99", TInterface.TSectionColorsRead("Muted")[0]);
        Assert.Equal("Cadroue", TInterface.TSectionNamesRead()[0]);
        Assert.True(TInterface.TSectionNativeCheck("Tableau"));
        Assert.False(TInterface.TSectionNativeCheck("Nope"));
    }
}
