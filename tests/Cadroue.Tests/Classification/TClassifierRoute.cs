using System;

using Cadroue.Core;

using Xunit;

using static Cadroue.Tests.TClassifierData;

namespace Cadroue.Tests;

public sealed class TClassifierRoute
{
    [Fact]
    public void FirstMatchingRule_DeterminesRouteIndex()
    {
        LSceneFunnelRule first = TClassifierFilenameCreate();
        first.LSceneFunnelContains = TClassifierConditionCreate("zzz");
        LSceneFunnelRule second = TClassifierFilenameCreate();
        second.LSceneFunnelExtension = TClassifierConditionCreate("mp4");
        LSceneFunnelRule third = TClassifierFilenameCreate();
        third.LSceneFunnelContains = TClassifierConditionCreate("clip");

        Assert.Equal(1, TInterface.TClassifierRouteRead(new[] { first, second, third }, "clip.mp4"));
    }

    [Fact]
    public void NoMatchingRule_ReturnsMinusOne()
    {
        LSceneFunnelRule rule = TClassifierFilenameCreate();
        rule.LSceneFunnelContains = TClassifierConditionCreate("zzz");
        Assert.Equal(-1, TInterface.TClassifierRouteRead(new[] { rule }, "clip.mp4"));
    }

    [Fact]
    public void EmptyRuleSet_ReturnsMinusOne()
    {
        Assert.Equal(-1, TInterface.TClassifierRouteRead(Array.Empty<LSceneFunnelRule>(), "clip.mp4"));
    }

    [Fact]
    public void UnmatchedInput_RoutesToRemainder()
    {
        LSceneFunnelRule rule = TClassifierFilenameCreate();
        rule.LSceneFunnelContains = TClassifierConditionCreate("zzz");
        LSceneFunnelRule remainder = TClassifierRemainderCreate();

        Assert.Equal(1, TInterface.TClassifierRouteRead(new[] { rule, remainder }, "clip.mp4"));
    }

    [Fact]
    public void MatchingRule_WinsOverRemainder()
    {
        LSceneFunnelRule remainder = TClassifierRemainderCreate();
        LSceneFunnelRule rule = TClassifierFilenameCreate();
        rule.LSceneFunnelContains = TClassifierConditionCreate("clip");

        Assert.Equal(1, TInterface.TClassifierRouteRead(new[] { remainder, rule }, "clip.mp4"));
    }

    [Fact]
    public void RemainderCard_NeverMatchedByConditions()
    {
        Assert.False(TInterface.TClassifierMatch(TClassifierRemainderCreate(), "clip.mp4"));
    }

    [Fact]
    public void UnusableMatchingRule_DoesNotShadowLaterUsableRule()
    {
        LSceneFunnelRule first = TClassifierFilenameCreate();
        first.LSceneFunnelContains = TClassifierConditionCreate("clip");
        LSceneFunnelRule second = TClassifierFilenameCreate();
        second.LSceneFunnelContains = TClassifierConditionCreate("clip");

        Assert.Equal(1, TInterface.TClassifierRouteRead(
            new[] { first, second }, "clip.mp4", index => index != 0));
    }

    [Fact]
    public void UnusableMatchingRule_StillFallsBackToRemainder()
    {
        LSceneFunnelRule rule = TClassifierFilenameCreate();
        rule.LSceneFunnelContains = TClassifierConditionCreate("clip");
        LSceneFunnelRule remainder = TClassifierRemainderCreate();

        Assert.Equal(1, TInterface.TClassifierRouteRead(
            new[] { rule, remainder }, "clip.mp4", index => index != 0));
    }

    [Fact]
    public void UnusableRemainder_DoesNotShadowLaterUsableRemainder()
    {
        LSceneFunnelRule first = TClassifierRemainderCreate();
        LSceneFunnelRule second = TClassifierRemainderCreate();

        Assert.Equal(1, TInterface.TClassifierRouteRead(
            new[] { first, second }, "clip.mp4", index => index != 0));
    }

    [Fact]
    public void EveryRuleUnusable_ReturnsMinusOne()
    {
        LSceneFunnelRule rule = TClassifierFilenameCreate();
        rule.LSceneFunnelContains = TClassifierConditionCreate("clip");
        LSceneFunnelRule remainder = TClassifierRemainderCreate();

        Assert.Equal(-1, TInterface.TClassifierRouteRead(
            new[] { rule, remainder }, "clip.mp4", _ => false));
    }
}
