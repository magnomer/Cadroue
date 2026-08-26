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

    [Fact]
    public void HevcAutoPixel_UsesDeliveryCompatible420Layout()
    {
        using var environment = new TEncodeCommand();
        LEncoding encoding = TEncodeCommand.OutputCreate(
            container: "MP4", extension: "mp4", videoEncoder: "libx265",
            videoPixelFormat: "Auto");
        LWorkItem work = TEncodeCommand.WorkCreate(
            LWorkKind.LWorkKindSplit, "prores.mov", "encoded.mp4", encoding,
            end: TimeSpan.FromMinutes(2));
        TEncodeCommand.SourcePixelApply(work, "yuv422p10le");

        IReadOnlyList<string> tokens = CommandTokens.Read(
            Assert.Single(TEncodeCommand.StagesBuild(work)).LEncodeStageArguments);

        Assert.Equal("yuv420p10le", CommandTokens.ValueAfter(tokens, "-pix_fmt"));
    }

    [Fact]
    public void HevcExplicitPixel_PreservesRequestedProfessionalLayout()
    {
        using var environment = new TEncodeCommand();
        LEncoding encoding = TEncodeCommand.OutputCreate(
            container: "MP4", extension: "mp4", videoEncoder: "libx265",
            videoPixelFormat: "yuv422p10le");
        LWorkItem work = TEncodeCommand.WorkCreate(
            LWorkKind.LWorkKindSplit, "prores.mov", "encoded.mp4", encoding,
            end: TimeSpan.FromMinutes(2));

        IReadOnlyList<string> tokens = CommandTokens.Read(
            Assert.Single(TEncodeCommand.StagesBuild(work)).LEncodeStageArguments);

        Assert.Equal("yuv422p10le", CommandTokens.ValueAfter(tokens, "-pix_fmt"));
    }

    [Fact]
    public void HevcQsvAutoPixel_UsesSupported420Layout()
    {
        using var environment = new TEncodeCommand();
        LEncoding encoding = TEncodeCommand.OutputCreate(
            container: "MP4", extension: "mp4", videoEncoder: "hevc_qsv",
            videoPixelFormat: "Auto");
        LWorkItem work = TEncodeCommand.WorkCreate(
            LWorkKind.LWorkKindSplit, "prores.mov", "encoded.mp4", encoding,
            end: TimeSpan.FromMinutes(2));
        TEncodeCommand.SourcePixelApply(work, "yuv422p10le");

        IReadOnlyList<string> tokens = CommandTokens.Read(
            Assert.Single(TEncodeCommand.StagesBuild(work)).LEncodeStageArguments);

        Assert.Equal("p010le", CommandTokens.ValueAfter(tokens, "-pix_fmt"));
    }

    [Theory]
    [InlineData("libx264", "yuv420p")]
    [InlineData("h264_mf", "yuv420p")]
    [InlineData("libopenh264", "yuv420p")]
    [InlineData("h264_qsv", "nv12")]
    [InlineData("h264_amf", "yuv420p")]
    [InlineData("h264_nvenc", "yuv420p")]
    [InlineData("libx265", "yuv420p")]
    [InlineData("hevc_qsv", "nv12")]
    [InlineData("hevc_amf", "yuv420p")]
    [InlineData("hevc_mf", "yuv420p")]
    [InlineData("hevc_nvenc", "yuv420p")]
    [InlineData("libvvenc", "yuv420p10le")]
    [InlineData("libaom-av1", "yuv420p")]
    [InlineData("libsvtav1", "yuv420p")]
    [InlineData("librav1e", "yuv420p")]
    [InlineData("av1_qsv", "nv12")]
    [InlineData("av1_amf", "yuv420p")]
    [InlineData("av1_nvenc", "yuv420p")]
    [InlineData("libvpx", "yuv420p")]
    [InlineData("libvpx-vp9", "yuv420p")]
    [InlineData("vp9_qsv", "nv12")]
    [InlineData("libxvid", "yuv420p")]
    [InlineData("mpeg4", "yuv420p")]
    [InlineData("libtheora", "yuv420p")]
    [InlineData("prores", "yuv422p10le")]
    [InlineData("prores_aw", "yuv422p10le")]
    [InlineData("prores_ks", "yuv422p10le")]
    [InlineData("ffv1", null)]
    [InlineData("mjpeg", "yuvj420p")]
    [InlineData("jpeg2000", null)]
    [InlineData("libopenjpeg", null)]
    [InlineData("libwebp", "yuv420p")]
    [InlineData("libwebp_anim", "yuv420p")]
    [InlineData("libxeve", "yuv420p")]
    [InlineData("libxavs2", "yuv420p")]
    [InlineData("liboapv", "yuv422p10le")]
    public void AutoPixel_CoversEveryCatalogEncoder(string encoder, string? expectedPixel)
    {
        using var environment = new TEncodeCommand();
        LEncoding encoding = TEncodeCommand.OutputCreate(
            container: "Matroska", extension: "mkv", videoEncoder: encoder,
            videoPixelFormat: "Auto");
        LWorkItem work = TEncodeCommand.WorkCreate(
            LWorkKind.LWorkKindConvert, "received.mkv", "encoded.mkv", encoding,
            end: TimeSpan.FromMinutes(2));
        TEncodeCommand.SourcePixelApply(work, "yuv420p");

        IReadOnlyList<string> tokens = CommandTokens.Read(
            Assert.Single(TEncodeCommand.StagesBuild(work)).LEncodeStageArguments);

        if (expectedPixel is null)
        {
            Assert.DoesNotContain("-pix_fmt", tokens);
        }
        else
        {
            Assert.Equal(expectedPixel, CommandTokens.ValueAfter(tokens, "-pix_fmt"));
        }
    }

    [Fact]
    public void AutoPixel_TableCoversEveryCatalogEncoder()
    {
        string[] expected =
        [
            "av1_amf", "av1_nvenc", "av1_qsv", "ffv1", "h264_amf", "h264_mf", "h264_nvenc", "h264_qsv",
            "hevc_amf", "hevc_mf", "hevc_nvenc", "hevc_qsv", "jpeg2000", "libaom-av1", "liboapv",
            "libopenh264", "libopenjpeg", "librav1e", "libsvtav1", "libtheora", "libvpx", "libvpx-vp9",
            "libvvenc", "libwebp", "libwebp_anim", "libx264", "libx265", "libxavs2", "libxeve", "libxvid",
            "mjpeg", "mpeg4", "prores", "prores_aw", "prores_ks", "vp9_qsv"
        ];

        Assert.Equal(expected, TEncodeCommand.VideoEncodersRead());
    }

    [Theory]
    [InlineData("libx265", "yuv420p10le")]
    [InlineData("hevc_amf", "p010le")]
    [InlineData("hevc_nvenc", "p010le")]
    [InlineData("libaom-av1", "yuv420p10le")]
    [InlineData("libsvtav1", "yuv420p10le")]
    [InlineData("librav1e", "yuv420p10le")]
    [InlineData("av1_amf", "p010le")]
    [InlineData("av1_nvenc", "p010le")]
    [InlineData("libvpx-vp9", "yuv420p10le")]
    [InlineData("libxeve", "yuv420p10le")]
    [InlineData("hevc_qsv", "p010le")]
    [InlineData("av1_qsv", "p010le")]
    [InlineData("vp9_qsv", "p010le")]
    public void AutoPixel_PreservesReceivedHighDepthWithinMainProfile(string encoder, string expectedPixel)
    {
        using var environment = new TEncodeCommand();
        LEncoding encoding = TEncodeCommand.OutputCreate(
            container: "Matroska", extension: "mkv", videoEncoder: encoder,
            videoPixelFormat: "Auto");
        LWorkItem work = TEncodeCommand.WorkCreate(
            LWorkKind.LWorkKindConvert, "received.mkv", "encoded.mkv", encoding,
            end: TimeSpan.FromMinutes(2));
        TEncodeCommand.SourcePixelApply(work, "yuv422p10le");

        IReadOnlyList<string> tokens = CommandTokens.Read(
            Assert.Single(TEncodeCommand.StagesBuild(work)).LEncodeStageArguments);

        Assert.Equal(expectedPixel, CommandTokens.ValueAfter(tokens, "-pix_fmt"));
    }

    [Theory]
    [InlineData("prores_ks", "yuva444p10le")]
    [InlineData("liboapv", "yuva444p10le")]
    [InlineData("libwebp", "yuva420p")]
    [InlineData("libwebp_anim", "yuva420p")]
    public void AutoPixel_PreservesReceivedAlphaForCapableEncoder(string encoder, string expectedPixel)
    {
        using var environment = new TEncodeCommand();
        LEncoding encoding = TEncodeCommand.OutputCreate(
            container: "Matroska", extension: "mkv", videoEncoder: encoder,
            videoPixelFormat: "Auto");
        LWorkItem work = TEncodeCommand.WorkCreate(
            LWorkKind.LWorkKindConvert, "received.mkv", "encoded.mkv", encoding,
            end: TimeSpan.FromMinutes(2));
        TEncodeCommand.SourcePixelApply(work, "yuva444p10le");

        IReadOnlyList<string> tokens = CommandTokens.Read(
            Assert.Single(TEncodeCommand.StagesBuild(work)).LEncodeStageArguments);

        Assert.Equal(expectedPixel, CommandTokens.ValueAfter(tokens, "-pix_fmt"));
    }

    [Theory]
    [InlineData("yuv444p12le", "yuv422p12le")]
    [InlineData("yuva444p12le", "yuva444p12le")]
    public void ApvAutoPixel_PreservesReceivedTwelveBitDepth(string sourcePixel, string expectedPixel)
    {
        using var environment = new TEncodeCommand();
        LEncoding encoding = TEncodeCommand.OutputCreate(
            container: "Matroska", extension: "mkv", videoEncoder: "liboapv",
            videoPixelFormat: "Auto");
        LWorkItem work = TEncodeCommand.WorkCreate(
            LWorkKind.LWorkKindConvert, "received.mkv", "encoded.mkv", encoding,
            end: TimeSpan.FromMinutes(2));
        TEncodeCommand.SourcePixelApply(work, sourcePixel);

        IReadOnlyList<string> tokens = CommandTokens.Read(
            Assert.Single(TEncodeCommand.StagesBuild(work)).LEncodeStageArguments);

        Assert.Equal(expectedPixel, CommandTokens.ValueAfter(tokens, "-pix_fmt"));
    }
}
