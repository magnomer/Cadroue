using Cadroue.Core;
using Cadroue.ShellEngine;

using Xunit;

namespace Cadroue.Tests;

[Collection("EncodeCommand")]
public sealed class TEncodeSplit
{
    [Fact]
    public void TrimmedSplit_UsesExactSourceOnceAndEmitsStartAndDuration()
    {
        using var environment = new TEncodeCommand();
        string source = Path.Combine("input media", "source clip.mov");
        string output = Path.Combine("output media", "split clip.mp4");
        LEncoding encoding = TEncodeCommand.TOutputCreate(videoMode: "Copy", audioMode: "Copy");
        LWorkItem work = TEncodeCommand.TWorkCreate(
            LWorkKind.LWorkKindSplit, source, output, encoding,
            TimeSpan.FromSeconds(12.5), TimeSpan.FromSeconds(20.25));

        LEncodeStage stage = Assert.Single(TEncodeCommand.TEncodeStagesBuild(work));
        IReadOnlyList<string> tokens = TEncodeToken.TEncodeTokenRead(stage.LEncodeStageArguments);

        Assert.Equal(1, TEncodeToken.TEncodeCountRead(tokens, source));
        Assert.Equal(source, TEncodeToken.TEncodeOptionRead(tokens, "-i"));
        Assert.Equal("12.5", TEncodeToken.TEncodeOptionRead(tokens, "-ss"));
        Assert.Equal("7.75", TEncodeToken.TEncodeOptionRead(tokens, "-t"));
        Assert.Equal(output, tokens[^1]);
        Assert.Equal(1, TEncodeToken.TEncodeCountRead(tokens, output));
    }
}
