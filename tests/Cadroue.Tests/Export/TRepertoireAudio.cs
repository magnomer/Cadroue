using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class TRepertoireAudio
{
    [Fact]
    public void AudioCatalog_IsNotEmpty_AndStartsWithNativeAac()
    {
        IReadOnlyList<LRepertoireAudio> candidates = TInterface.TRepertoireAudioRead();

        Assert.NotEmpty(candidates);
        Assert.Equal("aac", candidates[0].LRepertoireName);
        Assert.Equal("AAC, native / aac", candidates[0].LRepertoireText);
    }

    [Fact]
    public void AudioCatalog_NamesAndTexts_AreUnique()
    {
        IReadOnlyList<LRepertoireAudio> candidates = TInterface.TRepertoireAudioRead();

        Assert.Equal(candidates.Count, candidates.Select(candidate => candidate.LRepertoireName).Distinct().Count());
        Assert.Equal(candidates.Count, candidates.Select(candidate => candidate.LRepertoireText).Distinct().Count());
    }

    [Fact]
    public void AudioCatalog_EveryText_ResolvesToItsOwnName()
    {
        foreach (LRepertoireAudio candidate in TInterface.TRepertoireAudioRead())
        {
            Assert.Equal(candidate.LRepertoireName, TInterface.TRepertoireAudioResolve(candidate.LRepertoireText));
        }

        Assert.Null(TInterface.TRepertoireAudioResolve("Unknown encoder / nope"));
    }

    [Theory]
    [InlineData("AAC")]
    [InlineData("MP3")]
    [InlineData("MP2")]
    [InlineData("Opus")]
    [InlineData("Vorbis")]
    [InlineData("FLAC")]
    [InlineData("AC-3")]
    [InlineData("E-AC-3")]
    [InlineData("ALAC")]
    [InlineData("PCM")]
    [InlineData("WMA")]
    [InlineData("DTS")]
    public void AudioCatalog_CoversEachContainerFamily(string family)
    {
        IReadOnlyList<LRepertoireAudio> candidates = TInterface.TRepertoireAudioRead();

        Assert.Contains(
            candidates,
            candidate => TInterface.TRepertoireAudioFind(candidate.LRepertoireName) == family);
    }
}
