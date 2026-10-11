# LToolEngine.cs
Hash: `4054af2bc728c6bb`

## `public static class LToolEngine`

The single ShellEngine owner of where the ffmpeg and ffprobe programs are found.
The engine never names the Media tool resolver, so the host assigns each seam at startup.
Each seam is read at every spawn, so a changed tool folder applies to the next run.

## `public static Func<string> LToolFfmpegSeam { get; set; } = () => "ffmpeg";`

Unattached, it answers the bare program name, so the system path resolves it.

## `public static Func<string> LToolFfprobeSeam { get; set; } = () => "ffprobe";`

Unattached, it answers the bare program name, matching the resolver's own fallback.
