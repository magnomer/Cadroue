namespace Cadroue.Core;

public static partial class LCapabilityTable
{
    private static IEnumerable<KeyValuePair<string, LCapabilityCodec>> LCapabilityThirdRead()
    {
        yield return new("libxeve", new(
            "libxeve",
            [
                new("CQP (constant quantizer)", new("QP", "-qp", "32", 0, 51)),
                new("CRF (constant rate factor)", new("CRF", "-crf", "32", 10, 49)),
                new("ABR (target bitrate)", LCapabilityBitrateCreate("4M"))
            ],
            new LCapabilitySpeed("Speed preset", "-preset", "medium", ["default", "fast", "medium", "slow", "placebo"]),
            [
                new LCapabilityExtra("Tune", "-tune", "none", ["none", "zerolatency", "psnr"]),
                new LCapabilityExtra("Profile", "-profile", "baseline", ["baseline", "main"])
            ],
            "XEVE CRF range is 10-49, not the 0-51 of x264/x265."));
        yield return new("libxavs2", new(
            "libxavs2",
            [
                new("Constant QP", new("QP", "-qp", "34", 1, 63)),
                new("Target bitrate", LCapabilityBitrateCreate("4M"))
            ],
            new LCapabilitySpeed("Speed level", "-speed_level", "0", LCapabilityNumbersCreate(0, 9)),
            null,
            "xavs2 has no CRF and no named preset. Higher -speed_level is slower and better."));
        yield return new("liboapv", new(
            "liboapv",
            [
                new("CQP (constant quantizer)", new("QP", "-qp", "32", 0, 63)),
                new("Target bitrate", LCapabilityBitrateCreate("50M"))
            ],
            new LCapabilitySpeed("Speed preset", "-preset", "medium", ["fastest", "fast", "medium", "slow", "placebo"]),
            null,
            "APV is an intra-only professional codec. Quantizer range is 0-63; there is no CRF."));
    }
}
