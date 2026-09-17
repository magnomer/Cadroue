using Cadroue.Core;
using Cadroue.Infrastructure;

using Xunit;

namespace Cadroue.Tests;

public sealed class TTrialSet
{
    private static Task<LTrialResult> TTrialRun(string encoder, string available) =>
        Task.FromResult(TInterface.TTrialResultCreate(
            string.Equals(encoder, available, StringComparison.Ordinal),
            string.Empty));

    [Fact]
    public async Task ProbeRun_PublishesPassingEncoder_AndOmitsFailing()
    {
        TInterface.TTrialSetReset();

        await TInterface.TTrialSetStart(encoder => TTrialRun(encoder, "libx264"));

        IReadOnlySet<string>? available = TInterface.TTrialSetRead();
        Assert.NotNull(available);
        Assert.Contains("libx264", available);
        Assert.DoesNotContain("libx265", available);
    }

    [Fact]
    public async Task ProbeRun_WithNoPassingEncoder_LeavesSetUnpublished()
    {
        TInterface.TTrialSetReset();

        await TInterface.TTrialSetStart(_ => Task.FromResult(TInterface.TTrialResultCreate(false, "exit 1")));

        Assert.Null(TInterface.TTrialSetRead());
    }

    [Fact]
    public async Task CandidateCheck_FailsOpenBeforeProbe_ThenFollowsSet()
    {
        TInterface.TTrialSetReset();
        LRepertoireEncoder x264 = TInterface.TRepertoireEncodersRead()
            .Single(candidate => candidate.LRepertoireTokens[0] == "libx264");
        LRepertoireEncoder x265 = TInterface.TRepertoireEncodersRead()
            .Single(candidate => candidate.LRepertoireTokens[0] == "libx265");

        Assert.True(TInterface.TTrialSetCheck(x264));
        Assert.True(TInterface.TTrialSetCheck(x265));

        await TInterface.TTrialSetStart(encoder => TTrialRun(encoder, "libx264"));

        Assert.True(TInterface.TTrialSetCheck(x264));
        Assert.False(TInterface.TTrialSetCheck(x265));
    }

    [Fact]
    public void Apply_WithEmptySet_KeepsPreviousResult()
    {
        TInterface.TTrialSetReset();
        TInterface.TTrialSetApply(["libx264"]);

        TInterface.TTrialSetApply([]);

        IReadOnlySet<string>? available = TInterface.TTrialSetRead();
        Assert.NotNull(available);
        Assert.Contains("libx264", available);
    }
}
