using System;

using Cadroue.Core;

using Xunit;

using static Cadroue.Tests.ClassificationData;

namespace Cadroue.Tests;

public sealed class RouteSelectionTests
{
    [Fact]
    public void FirstMatchingRule_DeterminesRouteIndex()
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
    public void NoMatchingRule_ReturnsMinusOne()
    {
        LSceneFunnelRule rule = Filename();
        rule.LSceneFunnelContains = Cond("zzz");
        Assert.Equal(-1, LClassifier.LClassifierRouteRead(new[] { rule }, "clip.mp4"));
    }

    [Fact]
    public void EmptyRuleSet_ReturnsMinusOne()
    {
        Assert.Equal(-1, LClassifier.LClassifierRouteRead(Array.Empty<LSceneFunnelRule>(), "clip.mp4"));
    }
}
