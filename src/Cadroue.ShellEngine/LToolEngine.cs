namespace Cadroue.ShellEngine;

public static class LToolEngine
{
    public static Func<string> LToolFfmpegSeam { get; set; } = () => "ffmpeg";

    public static Func<string> LToolFfprobeSeam { get; set; } = () => "ffprobe";
}
