using System.Globalization;

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

    [Fact]
    public void InvalidRegexRule_ReportsFault()
    {
        var faults = new List<string>();
        using var faultSink = new TClassifierFault(faults.Add);
        LSceneFunnelRule rule = TClassifierRegexCreate("[unterminated-" + Guid.NewGuid().ToString("N"), whole: true);

        Assert.False(TInterface.TClassifierMatch(rule, "clip.mp4"));
        Assert.Single(faults);
    }

    [Fact]
    public void BacktrackingRegexRule_GivesUpAndReportsFault()
    {
        var faults = new List<string>();
        using var faultSink = new TClassifierFault(faults.Add);
        LSceneFunnelRule rule = TClassifierRegexCreate("^(a+)+$", whole: true);

        Assert.False(TInterface.TClassifierMatch(rule, new string('a', 40) + "!"));
        Assert.Single(faults);
    }

    [Fact]
    public void RegexRule_IgnoresCaseInvariantOfCulture()
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
        try
        {
            LSceneFunnelRule rule = TClassifierRegexCreate("INTRO", whole: true);
            Assert.True(TInterface.TClassifierMatch(rule, "intro.mp4"));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}
