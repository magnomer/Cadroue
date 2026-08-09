using Cadroue.Core;

using Xunit;

using static Cadroue.Tests.ClassificationData;

namespace Cadroue.Tests;

public sealed class FilenameRuleTests
{
    [Fact]
    public void RuleWithoutNonBlankCondition_DoesNotMatch()
    {
        LSceneFunnelRule rule = Filename();
        Assert.False(LClassifier.LClassifierMatch(rule, "clip.mp4"));
    }

    [Fact]
    public void ContainsCondition_MatchesContainedText()
    {
        LSceneFunnelRule rule = Filename();
        rule.LSceneFunnelContains = Cond("lip");
        Assert.True(LClassifier.LClassifierMatch(rule, "clip.mp4"));
    }

    [Fact]
    public void ContainsCondition_RejectsMissingText()
    {
        LSceneFunnelRule rule = Filename();
        rule.LSceneFunnelContains = Cond("zzz");
        Assert.False(LClassifier.LClassifierMatch(rule, "clip.mp4"));
    }

    [Fact]
    public void AndChain_RequiresBothConditions()
    {
        LSceneFunnelRule rule = Filename();
        rule.LSceneFunnelContains = Cond("clip");
        rule.LSceneFunnelStart = Cond("cl", join: true);
        Assert.True(LClassifier.LClassifierMatch(rule, "clip.mp4"));

        rule.LSceneFunnelStart = Cond("zz", join: true);
        Assert.False(LClassifier.LClassifierMatch(rule, "clip.mp4"));
    }

    [Fact]
    public void OrChain_MatchesWhenEitherConditionMatches()
    {
        LSceneFunnelRule rule = Filename();
        rule.LSceneFunnelContains = Cond("zzz");
        rule.LSceneFunnelStart = Cond("cl", join: false);
        Assert.True(LClassifier.LClassifierMatch(rule, "clip.mp4"));
    }

    [Fact]
    public void MixedJoin_EvaluatesLeftAssociatively()
    {
        LSceneFunnelRule rule = Filename();
        rule.LSceneFunnelContains = Cond("zzz");
        rule.LSceneFunnelStart = Cond("cl", join: false);
        rule.LSceneFunnelEnd = Cond("mp4", join: true);
        Assert.True(LClassifier.LClassifierMatch(rule, "clip.mp4"));

        rule.LSceneFunnelEnd = Cond("avi", join: true);
        Assert.False(LClassifier.LClassifierMatch(rule, "clip.mp4"));
    }

    [Fact]
    public void CaseSensitiveCondition_DistinguishesLetterCase()
    {
        LSceneFunnelRule rule = Filename();
        rule.LSceneFunnelContains = Cond("CLIP", caseSensitive: true);
        Assert.False(LClassifier.LClassifierMatch(rule, "clip.mp4"));

        rule.LSceneFunnelContains = Cond("clip", caseSensitive: true);
        Assert.True(LClassifier.LClassifierMatch(rule, "clip.mp4"));
    }

    [Fact]
    public void ExtensionCondition_AcceptsLeadingDot()
    {
        LSceneFunnelRule rule = Filename();
        rule.LSceneFunnelExtension = Cond(".mp4");
        Assert.True(LClassifier.LClassifierMatch(rule, "clip.mp4"));
    }

    [Fact]
    public void ExtensionCondition_AcceptsValueWithoutLeadingDot()
    {
        LSceneFunnelRule rule = Filename();
        rule.LSceneFunnelExtension = Cond("mp4");
        Assert.True(LClassifier.LClassifierMatch(rule, "clip.mp4"));
    }

    [Fact]
    public void ExtensionCondition_RejectsDifferentExtension()
    {
        LSceneFunnelRule rule = Filename();
        rule.LSceneFunnelExtension = Cond("avi");
        Assert.False(LClassifier.LClassifierMatch(rule, "clip.mp4"));
    }
}
