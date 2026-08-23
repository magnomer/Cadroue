using System.Linq;

using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

[Collection("Sidecar")]
public sealed class SidecarParsingTests
{
    [Fact]
    public void ValidContent_ParsesKnownMetadata()
    {
        TSidecar.TSidecarData? parsed = TSidecar.CoreParse(
            """
            {
              "LSidecarVersion": 2,
              "LSidecarSource": {
                "LSidecarFileName": "clip.mp4",
                "LSidecarLength": 4096,
                "LSidecarDurationMilliseconds": 12500,
                "LSidecarPartialHash": "ABC"
              },
              "LSidecarLoudness": -16.75
            }
            """);

        Assert.NotNull(parsed);
        Assert.Equal(2, parsed.Version);
        Assert.Equal("clip.mp4", parsed.FileName);
        Assert.Equal(4096, parsed.SourceLength);
        Assert.Equal(12500, parsed.DurationMilliseconds);
        Assert.Equal(-16.75, parsed.Loudness);
    }

    [Fact]
    public void MalformedContent_IsRejected()
    {
        Assert.Null(TSidecar.CoreParse("{ definitely not json"));
    }

    [Fact]
    public void MissingOptionalCacheData_PreservesCoreMetadata()
    {
        TSidecar.TSidecarData? parsed = TSidecar.CoreParse(
            """{"LSidecarSource":{"LSidecarFileName":"voice.wav","LSidecarLength":81},"LSidecarLoudness":-12.5}""");

        Assert.NotNull(parsed);
        Assert.Equal("voice.wav", parsed.FileName);
        Assert.Equal(81, parsed.SourceLength);
        Assert.Equal(-12.5, parsed.Loudness);
        Assert.Empty(parsed.Keyframes);
        Assert.Empty(parsed.ScannedSpans);
        Assert.Null(parsed.Waveform);
    }

    [Fact]
    public void OverfilledSections_ClampToCeiling()
    {
        string sections = string.Join(",", Enumerable.Repeat("{}", 6000));
        int count = TSidecar.SectionCountParse($$"""{"LSidecarSections":[{{sections}}]}""");

        Assert.Equal(LPiece.LPieceCeiling, count);
    }
}
