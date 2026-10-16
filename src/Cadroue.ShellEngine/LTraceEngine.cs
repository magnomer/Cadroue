namespace Cadroue.ShellEngine;

public static class LTraceEngine
{
    public static Action<string> LTraceInfoSeam { get; set; } = _ => { };

    // A record with its detail line: a UI record when the flag is set, kept only while verbose tracing is on,
    // else an info record.
    public static Action<bool, string, string?> LTraceDetailSeam { get; set; } = (_, _, _) => { };

    public static Action<string> LTraceWarningSeam { get; set; } = _ => { };

    public static Action<string, Exception?> LTraceErrorSeam { get; set; } = (_, _) => { };

    public static Action<string, string?> LTraceFfmpegSeam { get; set; } = (_, _) => { };

    public static Func<bool> LTraceVerboseSeam { get; set; } = () => false;
}
