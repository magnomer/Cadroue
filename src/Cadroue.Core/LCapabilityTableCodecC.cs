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
            new LCapabilitySpeed("Speed preset", "-preset", "medium",
                [new("default", "Default"), new("fast", "Fast"), new("medium", "Medium"), new("slow", "Slow"), new("placebo", "Placebo (slowest)")]),
            [
                new LCapabilityExtra("Tune", "-tune", "none",
                    [new("none", "None"), new("zerolatency", "Zero latency"), new("psnr", "PSNR")]),
                new LCapabilityExtra("Profile", "-profile", "baseline", [new("baseline", "Baseline"), new("main", "Main")])
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
            new LCapabilitySpeed("Speed preset", "-preset", "medium",
                [new("fastest", "Fastest"), new("fast", "Fast"), new("medium", "Medium"), new("slow", "Slow"), new("placebo", "Placebo (slowest)")]),
            null,
            "APV is an intra-only professional codec. Quantizer range is 0-63; there is no CRF."));
    }
}
