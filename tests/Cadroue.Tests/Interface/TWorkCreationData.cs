using Cadroue.Core;

namespace Cadroue.Tests;

internal static class WorkCreationOutput
{
    internal static LEncoding Create(
        string pattern = "{OriginalName}",
        string extension = "mp4",
        string? folder = null) => new(
        pattern,
        extension,
        extension,
        folder is null ? "Same as source" : "Custom location",
        folder ?? string.Empty,
        new LEncodingVideo(
            "Include", "Encode", "libx264", "Constant quality", "19", "slow",
            "1920x1080", true, "24", "yuv420p",
            new Dictionary<string, string> { ["-profile:v"] = "high" }),
        new LEncodingAudio(
            "Include", "Encode", "aac", "Bitrate", "192k", "Normal",
            new Dictionary<string, string> { ["-cutoff"] = "18000" }, "48000", "Stereo"),
        "Work creation test",
        "Rename",
        "_2");

    internal static LEncoding SplitCreate(string extension = "mp4", string? folder = null) => new(
        "{SectionName}",
        extension,
        extension,
        folder is null ? "Same as source" : "Custom location",
        folder ?? string.Empty,
        new LEncodingVideo(
            "Include", "Copy", string.Empty, string.Empty, string.Empty, string.Empty,
            "Same as source", false, "Same as source", "Auto", new Dictionary<string, string>()),
        new LEncodingAudio(
            "Include", "Copy", string.Empty, string.Empty, string.Empty, string.Empty,
            new Dictionary<string, string>(), "Same as source", "Same as source"),
        "Test",
        "Overwrite",
        "_1");
}
