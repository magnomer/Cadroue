using Cadroue.Application;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

public sealed class TEncoderRows
{
    [Fact]
    public void VideoSet_UnknownRate_ResolvesToFirstMode_RaisesOnce()
    {
        LSEncoder encoder = TInterface.TEncoderCreate(TInterface.TPresetCreate(), false);
        int rows = 0;
        TInterface.TEncoderVideoAttach(encoder, () => rows++);
        string codec = encoder.LSEncoderVideoEncoder;
        string[] modes = encoder.LSEncoderVideoCodec.LCapabilityModeLabels;
        bool several = modes.Length > 1;

        Assert.Equal(several, TInterface.TEncoderVideoSet(encoder, codec, modes[^1]));
        Assert.Equal(modes[^1], encoder.LSEncoderVideoRate);
        Assert.Equal(several, TInterface.TEncoderVideoSet(encoder, codec, "no such mode"));
        Assert.Equal(modes[0], encoder.LSEncoderVideoRate);
        Assert.False(TInterface.TEncoderVideoSet(encoder, codec, modes[0]));
        Assert.Equal(several ? 2 : 0, rows);
    }

    [Fact]
    public void VideoSet_EncoderChange_KeepsRateWhenOffered()
    {
        LSEncoder encoder = TInterface.TEncoderCreate(TInterface.TPresetCreate(), false);
        string other = TInterface.TRepertoireEncodersRead()
            .Select(candidate => candidate.LRepertoireText)
            .First(text => !string.Equals(text, encoder.LSEncoderVideoEncoder, StringComparison.Ordinal));
        string rate = encoder.LSEncoderVideoRate;

        Assert.True(TInterface.TEncoderVideoSet(encoder, other, rate));
        Assert.Equal(other, encoder.LSEncoderVideoEncoder);
        Assert.Contains(encoder.LSEncoderVideoRate, encoder.LSEncoderVideoCodec.LCapabilityModeLabels);
    }

    [Fact]
    public void Tier_SelectSetsDimensions_SizeSetMatchesTier()
    {
        LSEncoder encoder = TInterface.TEncoderCreate(TInterface.TPresetCreate(), false);
        int sizes = 0;
        TInterface.TEncoderSizeAttach(encoder, () => sizes++);
        Assert.Equal(0, encoder.LSEncoderSizeTier);
        Assert.Equal("Same as source", TInterface.TEncoderSizeFormat(encoder));

        Assert.True(TInterface.TEncoderTierSelect(encoder, 3));
        Assert.Equal(1920, encoder.LSEncoderWidth);
        Assert.Equal(1080, encoder.LSEncoderHeight);
        Assert.Equal("1920 × 1080", TInterface.TEncoderSizeFormat(encoder));
        Assert.False(TInterface.TEncoderTierSelect(encoder, 3));

        Assert.False(TInterface.TEncoderSizeSet(encoder, 1920, 1080));
        Assert.True(TInterface.TEncoderSizeSet(encoder, 1080, 1920));
        Assert.Equal(3, encoder.LSEncoderSizeTier);
        Assert.True(TInterface.TEncoderSizeSet(encoder, 1000, 0));
        Assert.Equal(-1, encoder.LSEncoderSizeTier);
        Assert.Equal("Same as source", TInterface.TEncoderSizeFormat(encoder));
        Assert.True(TInterface.TEncoderSizeSet(encoder, 0, 0));
        Assert.Equal(0, encoder.LSEncoderSizeTier);
        Assert.Equal(4, sizes);
    }

    [Fact]
    public void Smart_GatesVideoNotice()
    {
        LSEncoder plain = TInterface.TEncoderCreate(TInterface.TPresetCreate(), false);
        LSEncoder smart = TInterface.TEncoderCreate(TInterface.TPresetCreate(), true);

        Assert.Equal("Encoder.Video.Notice.SmartFull", TInterface.TEncoderNoticeResolve(plain, "Smart"));
        Assert.Equal("Encoder.Video.Notice.Smart", TInterface.TEncoderNoticeResolve(smart, "Smart"));
        Assert.Equal("Encoder.Video.Notice.Copied", TInterface.TEncoderNoticeResolve(smart, "Copy"));
    }

    [Fact]
    public void Suffix_SelectStoresShownUnderPreviousPolicy()
    {
        LSEncoder encoder = TInterface.TEncoderCreate(TInterface.TPresetCreate(), false);
        string first = TInterface.TEncoderSuffixSelect(encoder, "Rename output", "ignored");
        Assert.Equal(TInterface.TPresetSuffixRead(encoder.LSEncoderDraft, "Rename output"), first);

        TInterface.TEncoderSuffixSelect(encoder, "Rename existing", "_mine");
        Assert.Equal("_mine", TInterface.TPresetSuffixRead(encoder.LSEncoderDraft, "Rename output"));
        Assert.Equal("Rename existing", encoder.LSEncoderSuffixMode);
    }

    [Fact]
    public void Apply_CopiesDraftIntoSource()
    {
        LPreset source = TInterface.TPresetCreate();
        LSEncoder encoder = TInterface.TEncoderCreate(source, false);
        TInterface.TEncoderTierSelect(encoder, 2);
        encoder.LSEncoderDraft.LPresetDisplay = "  ";

        TInterface.TEncoderApply(encoder);

        Assert.Equal("1280 × 720", source.LPresetVideo.LPresetSize);
        Assert.Equal("{OriginalName}_export", source.LPresetDisplay);
        Assert.Equal("Include", source.LPresetVideo.LPresetStream);
    }
}
