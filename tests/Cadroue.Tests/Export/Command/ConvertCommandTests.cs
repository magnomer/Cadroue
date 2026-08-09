using Cadroue.Core;
using Cadroue.ShellEngine;

using Xunit;

namespace Cadroue.Tests;

[Collection("EncodeCommand")]
public sealed class ConvertCommandTests
{
    [Fact]
    public void Convert_AppliesRequestedCodecAndContainerDestination()
    {
        using var environment = new TEncodeCommand();
        string output = Path.Combine("exports", "converted movie.mkv");
        LEncoding encoding = TEncodeCommand.OutputCreate(
            container: "mkv", extension: "mkv", videoEncoder: "libx265",
            videoRateControl: "CRF (constant quality)", videoQuality: "24", videoSpeed: "medium",
            audioEncoder: "FLAC", audioRateControl: "Compression level", audioQuality: "8");
        LWorkItem work = TEncodeCommand.WorkCreate(
            LWorkKind.LWorkKindConvert, "source.mov", output, encoding, end: TimeSpan.FromMinutes(2));

        IReadOnlyList<string> tokens = CommandTokens.Read(
            Assert.Single(TEncodeCommand.StagesBuild(work)).LEncodeStageArguments);

        Assert.Equal("libx265", CommandTokens.ValueAfter(tokens, "-c:v"));
        Assert.Equal("24", CommandTokens.ValueAfter(tokens, "-crf"));
        Assert.Equal("medium", CommandTokens.ValueAfter(tokens, "-preset"));
        Assert.Equal("flac", CommandTokens.ValueAfter(tokens, "-c:a"));
        Assert.Equal(output, tokens[^1]);
        Assert.Equal(".mkv", Path.GetExtension(tokens[^1]));
    }

    [Fact]
    public void StreamCopy_DoesNotIncludeEncodeOnlyFilters()
    {
        using var environment = new TEncodeCommand();
        LEncoding encoding = TEncodeCommand.OutputCreate(videoMode: "Copy", audioMode: "Copy");
        LWorkItem work = TEncodeCommand.WorkCreate(
            LWorkKind.LWorkKindConvert, "source.mov", "copy.mp4", encoding,
            end: TimeSpan.FromMinutes(2));

        IReadOnlyList<string> tokens = CommandTokens.Read(
            Assert.Single(TEncodeCommand.StagesBuild(work)).LEncodeStageArguments);

        Assert.Equal("copy", CommandTokens.ValueAfter(tokens, "-c:v"));
        Assert.Equal("copy", CommandTokens.ValueAfter(tokens, "-c:a"));
        Assert.DoesNotContain("-vf", tokens);
        Assert.DoesNotContain("-af", tokens);
    }
}
