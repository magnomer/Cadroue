using System.Linq;

using Cadroue.Core;
using Cadroue.Tests.Interface;

using Xunit;

namespace Cadroue.Tests.Engine;

[Collection("Sidecar")]
public sealed class TSidecarParsing
{
    [Fact]
    public void ValidContent_ParsesKnownMetadata()
    {
        TSidecar.TSidecarData? parsed = TSidecar.TSidecarCoreParse(
            """
            {
              "LSidecarVersion": 2,
              "LSidecarSource": {
                "LSidecarFileName": "clip.mp4",
                "LSidecarLength": 4096,
                "LSidecarDurationMilliseconds": 12500,
                "LSidecarPartialHash": "ABC"
              },
              "LSidecarEdit": { "LSidecarRotation": 90 }
            }
            """);

        Assert.NotNull(parsed);
        Assert.Equal(2, parsed.TSidecarVersion);
        Assert.Equal("clip.mp4", parsed.TSidecarFileName);
        Assert.Equal(4096, parsed.TSidecarSourceLength);
        Assert.Equal(12500, parsed.TSidecarDurationMilliseconds);
        Assert.Equal(90, parsed.TSidecarRotation);
    }

    [Fact]
    public void MalformedContent_IsRejected()
    {
        Assert.Null(TSidecar.TSidecarCoreParse("{ definitely not json"));
    }

    [Fact]
    public void MissingOptionalCacheData_PreservesCoreMetadata()
    {
        TSidecar.TSidecarData? parsed = TSidecar.TSidecarCoreParse(
            """
            {"LSidecarSource":{"LSidecarFileName":"voice.wav","LSidecarLength":81},
            "LSidecarEdit":{"LSidecarRotation":270}}
            """);

        Assert.NotNull(parsed);
        Assert.Equal("voice.wav", parsed.TSidecarFileName);
        Assert.Equal(81, parsed.TSidecarSourceLength);
        Assert.Equal(270, parsed.TSidecarRotation);
        Assert.Empty(parsed.TSidecarKeyframes);
        Assert.Empty(parsed.TSidecarScannedSpans);
        Assert.Null(parsed.TSidecarWave);
    }

    [Fact]
    public void OverfilledSections_ClampToCeiling()
    {
        string sections = string.Join(",", Enumerable.Repeat("{}", 6000));
        int count = TSidecar.TSectionCountParse($$"""{"LSidecarSections":[{{sections}}]}""");

        Assert.Equal(LPiece.LPieceCeiling, count);
    }
}
