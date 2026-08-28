namespace Cadroue.Application;

public sealed partial class LPreset
{
    public const string LPresetSplitDefault = "Split (default)";
    public const string LPresetMergeDefault = "Merge (default)";

    private static readonly Dictionary<string, string[]> LPresetExtensionTable = new(StringComparer.Ordinal)
    {
        ["MP4"] = ["mp4", "m4v"],
        ["Matroska"] = ["mkv"],
        ["MOV"] = ["mov"],
        ["WebM"] = ["webm"],
        ["AVI"] = ["avi"],
        ["MPEG-TS"] = ["ts", "m2ts", "mts"],
        ["FLV"] = ["flv", "f4v"],
        ["Ogg"] = ["ogv"]
    };
}
