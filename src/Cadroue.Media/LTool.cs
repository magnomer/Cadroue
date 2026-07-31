namespace Cadroue.Media;

public static class LTool
{
    public static Func<string?>? LToolFolderSource { get; set; }

    public static string LToolFfmpegRead() => LToolResolve("ffmpeg.exe", "ffmpeg");

    public static string LToolFfprobeRead() => LToolResolve("ffprobe.exe", "ffprobe");

    private static string LToolResolve(string lToolExecutable, string lToolFallback)
    {
        string? lToolFolder = LToolFolderSource?.Invoke();
        if (string.IsNullOrWhiteSpace(lToolFolder))
        {
            return lToolFallback;
        }

        string lToolPath = Path.Combine(lToolFolder, lToolExecutable);
        return File.Exists(lToolPath) ? lToolPath : lToolFallback;
    }
}
