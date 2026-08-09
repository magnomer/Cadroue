using Cadroue.Core;
using Cadroue.ShellEngine;

using Xunit;

namespace Cadroue.Tests;

[Collection("EncodeCommand")]
public sealed class CommandEscapingTests
{
    [Fact]
    public void PathsContainingSpaces_RemainSingleInputAndOutputArguments()
    {
        using var environment = new TEncodeCommand();
        string source = Path.Combine("incoming files", "family video source.mov");
        string output = Path.Combine("finished files", "family video result.mp4");
        LWorkItem work = TEncodeCommand.WorkCreate(
            LWorkKind.LWorkKindConvert, source, output,
            TEncodeCommand.OutputCreate(videoMode: "Copy", audioMode: "Copy"),
            end: TimeSpan.FromMinutes(1));

        LEncodeStage stage = Assert.Single(TEncodeCommand.StagesBuild(work));
        IReadOnlyList<string> tokens = CommandTokens.Read(stage.LEncodeStageArguments);

        Assert.Equal(source, CommandTokens.ValueAfter(tokens, "-i"));
        Assert.Equal(1, CommandTokens.Count(tokens, source));
        Assert.Equal(output, tokens[^1]);
        Assert.Equal(1, CommandTokens.Count(tokens, output));
    }
}
