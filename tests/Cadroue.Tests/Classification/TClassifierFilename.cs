using Cadroue.Core;

using Xunit;

using static Cadroue.Tests.TClassifierData;

namespace Cadroue.Tests;

public sealed class TClassifierFilename
{
    [Fact]
    public void RuleWithoutNonBlankCondition_DoesNotMatch()
    {
        LSceneFunnelRule rule = TClassifierFilenameCreate();
        Assert.False(TInterface.TClassifierMatch(rule, "clip.mp4"));
    }

    [Fact]
    public void ContainsCondition_MatchesContainedText()
    {
        LSceneFunnelRule rule = TClassifierFilenameCreate();
        rule.LSceneFunnelContains = TClassifierConditionCreate("lip");
        Assert.True(TInterface.TClassifierMatch(rule, "clip.mp4"));
    }

    [Fact]
    public void ContainsCondition_RejectsMissingText()
    {
        LSceneFunnelRule rule = TClassifierFilenameCreate();
        rule.LSceneFunnelContains = TClassifierConditionCreate("zzz");
        Assert.False(TInterface.TClassifierMatch(rule, "clip.mp4"));
    }

    [Fact]
    public void AndChain_RequiresBothConditions()
    {
        LSceneFunnelRule rule = TClassifierFilenameCreate();
        rule.LSceneFunnelContains = TClassifierConditionCreate("clip");
        rule.LSceneFunnelPrefix = TClassifierConditionCreate("cl", join: true);
        Assert.True(TInterface.TClassifierMatch(rule, "clip.mp4"));

        rule.LSceneFunnelPrefix = TClassifierConditionCreate("zz", join: true);
        Assert.False(TInterface.TClassifierMatch(rule, "clip.mp4"));
    }

    [Fact]
    public void OrChain_MatchesWhenEitherConditionMatches()
    {
        LSceneFunnelRule rule = TClassifierFilenameCreate();
        rule.LSceneFunnelContains = TClassifierConditionCreate("zzz");
        rule.LSceneFunnelPrefix = TClassifierConditionCreate("cl", join: false);
        Assert.True(TInterface.TClassifierMatch(rule, "clip.mp4"));
    }

    [Fact]
    public void MixedJoin_EvaluatesLeftAssociatively()
    {
        LSceneFunnelRule rule = TClassifierFilenameCreate();
        rule.LSceneFunnelContains = TClassifierConditionCreate("zzz");
        rule.LSceneFunnelPrefix = TClassifierConditionCreate("cl", join: false);
        rule.LSceneFunnelEnd = TClassifierConditionCreate("mp4", join: true);
        Assert.True(TInterface.TClassifierMatch(rule, "clip.mp4"));

        rule.LSceneFunnelEnd = TClassifierConditionCreate("avi", join: true);
        Assert.False(TInterface.TClassifierMatch(rule, "clip.mp4"));
    }

    [Fact]
    public void CaseSensitiveCondition_DistinguishesLetterCase()
    {
        LSceneFunnelRule rule = TClassifierFilenameCreate();
        rule.LSceneFunnelContains = TClassifierConditionCreate("CLIP", caseSensitive: true);
        Assert.False(TInterface.TClassifierMatch(rule, "clip.mp4"));

        rule.LSceneFunnelContains = TClassifierConditionCreate("clip", caseSensitive: true);
        Assert.True(TInterface.TClassifierMatch(rule, "clip.mp4"));
    }

    [Fact]
    public void ExtensionCondition_AcceptsLeadingDot()
    {
        LSceneFunnelRule rule = TClassifierFilenameCreate();
        rule.LSceneFunnelExtension = TClassifierConditionCreate(".mp4");
        Assert.True(TInterface.TClassifierMatch(rule, "clip.mp4"));
    }

    [Fact]
    public void ExtensionCondition_AcceptsValueWithoutLeadingDot()
    {
        LSceneFunnelRule rule = TClassifierFilenameCreate();
        rule.LSceneFunnelExtension = TClassifierConditionCreate("mp4");
        Assert.True(TInterface.TClassifierMatch(rule, "clip.mp4"));
    }

    [Fact]
    public void ExtensionCondition_RejectsDifferentExtension()
    {
        LSceneFunnelRule rule = TClassifierFilenameCreate();
        rule.LSceneFunnelExtension = TClassifierConditionCreate("avi");
        Assert.False(TInterface.TClassifierMatch(rule, "clip.mp4"));
    }
}
