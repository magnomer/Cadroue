using System.IO;

namespace Cadroue.Media;

public static class LMedia
{
    public static readonly IReadOnlyList<string> LMediaVideoExtensions =
        [".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv", ".webm", ".m4v", ".ts", ".mts", ".m2ts"];

    public static readonly IReadOnlyList<string> LMediaAudioExtensions =
        [".mp3", ".aac", ".flac", ".wav", ".ogg"];

    public static bool LMediaAudioCheck(string lMediaSourcePath) =>
        LMediaAudioExtensions.Contains(Path.GetExtension(lMediaSourcePath), StringComparer.OrdinalIgnoreCase);

    public static bool LMediaCheck(string lMediaSourcePath)
    {
        string lMediaExtension = Path.GetExtension(lMediaSourcePath);
        return LMediaVideoExtensions.Contains(lMediaExtension, StringComparer.OrdinalIgnoreCase)
            || LMediaAudioExtensions.Contains(lMediaExtension, StringComparer.OrdinalIgnoreCase);
    }

    public static IReadOnlyList<string> LMediaFileRead(string lMediaPath) =>
        File.Exists(lMediaPath) && LMediaCheck(lMediaPath) ? [lMediaPath] : [];
}
