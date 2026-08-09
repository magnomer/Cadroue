using Cadroue.Core;

using Xunit;

using static Cadroue.Tests.ClassificationData;

namespace Cadroue.Tests;

public sealed class RegexRuleTests
{
    [Fact]
    public void RegexWholeRule_MatchesFullNameWithExtension()
    {
        LSceneFunnelRule rule = Regex(@"\.mp4$", whole: true);
        Assert.True(LClassifier.LClassifierMatch(rule, "clip.mp4"));
    }

    [Fact]
    public void RegexStemRule_IgnoresExtension()
    {
        LSceneFunnelRule rule = Regex(@"\.mp4$", whole: false);
        Assert.False(LClassifier.LClassifierMatch(rule, "clip.mp4"));
    }

    [Fact]
    public void RegexStemRule_MatchesStem()
    {
        LSceneFunnelRule rule = Regex("^clip$", whole: false);
        Assert.True(LClassifier.LClassifierMatch(rule, "clip.mp4"));
    }

    [Fact]
    public void RegexRule_IgnoresCase()
    {
        LSceneFunnelRule rule = Regex("CLIP", whole: true);
        Assert.True(LClassifier.LClassifierMatch(rule, "clip.mp4"));
    }

    [Fact]
    public void BlankRegexRule_DoesNotMatch()
    {
        LSceneFunnelRule rule = Regex("   ", whole: true);
        Assert.False(LClassifier.LClassifierMatch(rule, "clip.mp4"));
    }

    [Fact]
    public void InvalidRegexRule_DoesNotMatch()
    {
        LSceneFunnelRule rule = Regex("[unterminated", whole: true);
        Assert.False(LClassifier.LClassifierMatch(rule, "clip.mp4"));
    }
}
