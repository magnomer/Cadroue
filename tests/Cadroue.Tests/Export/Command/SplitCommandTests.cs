using Cadroue.Core;
using Cadroue.ShellEngine;

using Xunit;

namespace Cadroue.Tests;

[Collection("EncodeCommand")]
public sealed class SplitCommandTests
{
    [Fact]
    public void TrimmedSplit_UsesExactSourceOnceAndEmitsStartAndDuration()
    {
        using var environment = new TEncodeCommand();
        string source = Path.Combine("input media", "source clip.mov");
        string output = Path.Combine("output media", "split clip.mp4");
        LEncoding encoding = TEncodeCommand.OutputCreate(videoMode: "Copy", audioMode: "Copy");
        LWorkItem work = TEncodeCommand.WorkCreate(
            LWorkKind.LWorkKindSplit, source, output, encoding,
            TimeSpan.FromSeconds(12.5), TimeSpan.FromSeconds(20.25));

        LEncodeStage stage = Assert.Single(TEncodeCommand.StagesBuild(work));
        IReadOnlyList<string> tokens = CommandTokens.Read(stage.LEncodeStageArguments);

        Assert.Equal(1, CommandTokens.Count(tokens, source));
        Assert.Equal(source, CommandTokens.ValueAfter(tokens, "-i"));
        Assert.Equal("12.5", CommandTokens.ValueAfter(tokens, "-ss"));
        Assert.Equal("7.75", CommandTokens.ValueAfter(tokens, "-t"));
        Assert.Equal(output, tokens[^1]);
        Assert.Equal(1, CommandTokens.Count(tokens, output));
    }
}
