using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class LClassifierTests
{
    private static LSceneFunnelMatch Cond(string text, bool caseSensitive = false, bool join = true) =>
        new() { LSceneFunnelText = text, LSceneFunnelCase = caseSensitive, LSceneFunnelJoin = join };

    private static LSceneFunnelRule Regex(string pattern, bool whole) =>
        new()
        {
            LSceneFunnelType = (int)LSceneFunnelForm.LSceneFunnelRegex,
            LSceneFunnelRegex = pattern,
            LSceneFunnelWhole = whole
        };

    private static LSceneFunnelRule Filename() =>
        new() { LSceneFunnelType = (int)LSceneFunnelForm.LSceneFunnelFilename };

    // ---- Regex form ----

    [Fact]
    public void Match_RegexWhole_MatchesFullNameWithExtension()
    {
        LSceneFunnelRule rule = Regex(@"\.mp4$", whole: true);
        Assert.True(LClassifier.LClassifierMatch(rule, "clip.mp4"));
    }

    [Fact]
    public void Match_RegexStem_IgnoresExtension()
    {
        LSceneFunnelRule rule = Regex(@"\.mp4$", whole: false);
        Assert.False(LClassifier.LClassifierMatch(rule, "clip.mp4"));
    }

    [Fact]
    public void Match_RegexStem_MatchesStem()
    {
        LSceneFunnelRule rule = Regex("^clip$", whole: false);
        Assert.True(LClassifier.LClassifierMatch(rule, "clip.mp4"));
    }

    [Fact]
    public void Match_RegexIgnoresCase()
    {
        LSceneFunnelRule rule = Regex("CLIP", whole: true);
        Assert.True(LClassifier.LClassifierMatch(rule, "clip.mp4"));
    }

    [Fact]
    public void Match_RegexBlank_ReturnsFalse()
    {
        LSceneFunnelRule rule = Regex("   ", whole: true);
        Assert.False(LClassifier.LClassifierMatch(rule, "clip.mp4"));
    }

    [Fact]
    public void Match_RegexInvalid_ReturnsFalse()
    {
        LSceneFunnelRule rule = Regex("[unterminated", whole: true);
        Assert.False(LClassifier.LClassifierMatch(rule, "clip.mp4"));
    }

    // ---- Filename form ----

    [Fact]
    public void Match_NoNonBlankCondition_ReturnsFalse()
    {
        LSceneFunnelRule rule = Filename();
        Assert.False(LClassifier.LClassifierMatch(rule, "clip.mp4"));
    }

    [Fact]
    public void Match_SingleContains_True()
    {
        LSceneFunnelRule rule = Filename();
        rule.LSceneFunnelContains = Cond("lip");
        Assert.True(LClassifier.LClassifierMatch(rule, "clip.mp4"));
    }

    [Fact]
    public void Match_SingleContains_False()
    {
        LSceneFunnelRule rule = Filename();
        rule.LSceneFunnelContains = Cond("zzz");
        Assert.False(LClassifier.LClassifierMatch(rule, "clip.mp4"));
    }

    [Fact]
    public void Match_AndChain_BothRequired()
    {
        LSceneFunnelRule rule = Filename();
        rule.LSceneFunnelContains = Cond("clip");
        rule.LSceneFunnelStart = Cond("cl", join: true);
        Assert.True(LClassifier.LClassifierMatch(rule, "clip.mp4"));

        rule.LSceneFunnelStart = Cond("zz", join: true);
        Assert.False(LClassifier.LClassifierMatch(rule, "clip.mp4"));
    }

    [Fact]
    public void Match_OrChain_EitherSuffices()
    {
        LSceneFunnelRule rule = Filename();
        rule.LSceneFunnelContains = Cond("zzz");
        rule.LSceneFunnelStart = Cond("cl", join: false);
        Assert.True(LClassifier.LClassifierMatch(rule, "clip.mp4"));
    }

    [Fact]
    public void Match_MixedJoin_LeftAssociative()
    {
        // contains(a) OR start(b) AND end(c): first sets result, then || b, then && c
        LSceneFunnelRule rule = Filename();
        rule.LSceneFunnelContains = Cond("zzz");
        rule.LSceneFunnelStart = Cond("cl", join: false);
        rule.LSceneFunnelEnd = Cond("mp4", join: true);
        // (false || true) && true = true
        Assert.True(LClassifier.LClassifierMatch(rule, "clip.mp4"));

        rule.LSceneFunnelEnd = Cond("avi", join: true);
        // (false || true) && false = false
        Assert.False(LClassifier.LClassifierMatch(rule, "clip.mp4"));
    }

    [Fact]
    public void Match_CaseSensitive_Distinguishes()
    {
        LSceneFunnelRule rule = Filename();
        rule.LSceneFunnelContains = Cond("CLIP", caseSensitive: true);
        Assert.False(LClassifier.LClassifierMatch(rule, "clip.mp4"));

        rule.LSceneFunnelContains = Cond("clip", caseSensitive: true);
        Assert.True(LClassifier.LClassifierMatch(rule, "clip.mp4"));
    }

    [Fact]
    public void Match_Extension_WithLeadingDot()
    {
        LSceneFunnelRule rule = Filename();
        rule.LSceneFunnelExtension = Cond(".mp4");
        Assert.True(LClassifier.LClassifierMatch(rule, "clip.mp4"));
    }

    [Fact]
    public void Match_Extension_WithoutLeadingDot()
    {
        LSceneFunnelRule rule = Filename();
        rule.LSceneFunnelExtension = Cond("mp4");
        Assert.True(LClassifier.LClassifierMatch(rule, "clip.mp4"));
    }

    [Fact]
    public void Match_Extension_Mismatch()
    {
        LSceneFunnelRule rule = Filename();
        rule.LSceneFunnelExtension = Cond("avi");
        Assert.False(LClassifier.LClassifierMatch(rule, "clip.mp4"));
    }

    // ---- Route read ----

    [Fact]
    public void RouteRead_ReturnsFirstMatchIndex()
    {
        LSceneFunnelRule first = Filename();
        first.LSceneFunnelContains = Cond("zzz");
        LSceneFunnelRule second = Filename();
        second.LSceneFunnelExtension = Cond("mp4");
        LSceneFunnelRule third = Filename();
        third.LSceneFunnelContains = Cond("clip");

        Assert.Equal(1, LClassifier.LClassifierRouteRead(new[] { first, second, third }, "clip.mp4"));
    }

    [Fact]
    public void RouteRead_NoMatch_ReturnsMinusOne()
    {
        LSceneFunnelRule rule = Filename();
        rule.LSceneFunnelContains = Cond("zzz");
        Assert.Equal(-1, LClassifier.LClassifierRouteRead(new[] { rule }, "clip.mp4"));
    }

    [Fact]
    public void RouteRead_Empty_ReturnsMinusOne()
    {
        Assert.Equal(-1, LClassifier.LClassifierRouteRead(System.Array.Empty<LSceneFunnelRule>(), "clip.mp4"));
    }
}
