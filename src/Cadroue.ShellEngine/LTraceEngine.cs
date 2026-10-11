namespace Cadroue.ShellEngine;

public static class LTraceEngine
{
    public static Action<string> LTraceInfoSeam { get; set; } = _ => { };

    public static Action<string> LTraceWarningSeam { get; set; } = _ => { };

    public static Action<string, Exception?> LTraceErrorSeam { get; set; } = (_, _) => { };

    public static Action<string, string?> LTraceFfmpegSeam { get; set; } = (_, _) => { };

    public static Func<bool> LTraceVerboseSeam { get; set; } = () => false;
}
