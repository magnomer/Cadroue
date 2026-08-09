using Cadroue.Core;
using Cadroue.ShellEngine;

using Xunit;

namespace Cadroue.Tests;

[Collection("EncodeCommand")]
public sealed class MergeCommandTests
{
    [Fact]
    public void MergeManifest_PreservesInputOrderAndCommandPlacesOutputLast()
    {
        using var environment = new TEncodeCommand();
        string[] sources =
        {
            Path.Combine("first folder", "one.mov"),
            Path.Combine("second folder", "two's clip.mov"),
            Path.Combine("third folder", "three.mov")
        };
        string output = Path.Combine("merged output", "result file.mkv");
        LWorkItem work = TEncodeCommand.WorkCreate(
            LWorkKind.LWorkKindMerge, sources[0], output,
            TEncodeCommand.OutputCreate(container: "mkv", extension: "mkv"), mergeSources: sources);

        LEncodeStage stage = Assert.Single(TEncodeCommand.StagesBuild(work));
        IReadOnlyList<string> tokens = CommandTokens.Read(stage.LEncodeStageArguments);
        string manifest = CommandTokens.ValueAfter(tokens, "-i");

        Assert.Equal(new[]
        {
            "file 'first folder/one.mov'",
            "file 'second folder/two'\\''s clip.mov'",
            "file 'third folder/three.mov'"
        }, File.ReadAllLines(manifest));
        Assert.Equal(output, tokens[^1]);
        Assert.Equal(1, CommandTokens.Count(tokens, manifest));
    }
}
