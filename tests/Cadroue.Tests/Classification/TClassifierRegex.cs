using Cadroue.Core;

using Xunit;

using static Cadroue.Tests.TClassifierData;

namespace Cadroue.Tests;

public sealed class TClassifierRegex
{
    [Fact]
    public void RegexWholeRule_MatchesFullNameWithExtension()
    {
        LSceneFunnelRule rule = TClassifierRegexCreate(@"\.mp4$", whole: true);
        Assert.True(TInterface.TClassifierMatch(rule, "clip.mp4"));
    }

    [Fact]
    public void RegexStemRule_IgnoresExtension()
    {
        LSceneFunnelRule rule = TClassifierRegexCreate(@"\.mp4$", whole: false);
        Assert.False(TInterface.TClassifierMatch(rule, "clip.mp4"));
    }

    [Fact]
    public void RegexStemRule_MatchesStem()
    {
        LSceneFunnelRule rule = TClassifierRegexCreate("^clip$", whole: false);
        Assert.True(TInterface.TClassifierMatch(rule, "clip.mp4"));
    }

    [Fact]
    public void RegexRule_IgnoresCase()
    {
        LSceneFunnelRule rule = TClassifierRegexCreate("CLIP", whole: true);
        Assert.True(TInterface.TClassifierMatch(rule, "clip.mp4"));
    }

    [Fact]
    public void BlankRegexRule_DoesNotMatch()
    {
        LSceneFunnelRule rule = TClassifierRegexCreate("   ", whole: true);
        Assert.False(TInterface.TClassifierMatch(rule, "clip.mp4"));
    }

    [Fact]
    public void InvalidRegexRule_DoesNotMatch()
    {
        LSceneFunnelRule rule = TClassifierRegexCreate("[unterminated", whole: true);
        Assert.False(TInterface.TClassifierMatch(rule, "clip.mp4"));
    }
}
