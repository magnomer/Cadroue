namespace Cadroue.ShellEngine;

internal static class LRunnerTrace
{
    internal static void LRunnerRecord(string lRunnerMessage, Exception? lRunnerException = null)
    {
        if (lRunnerException is null)
        {
            LTraceEngine.LTraceInfoSeam(lRunnerMessage);
            return;
        }

        LTraceEngine.LTraceErrorSeam(lRunnerMessage, lRunnerException);
    }

    internal static bool LRunnerVerboseCheck() => LTraceEngine.LTraceVerboseSeam();

    internal static void LRunnerFfmpegRecord(string lRunnerSummary, string? lRunnerDetail = null)
        => LTraceEngine.LTraceFfmpegSeam(lRunnerSummary, lRunnerDetail);
}
