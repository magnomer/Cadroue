namespace Cadroue.Infrastructure;

public static class LRendererLog
{
    public static Action<bool>? LRendererVerboseSeam { get; set; }

    public static string LRendererLogResolve(bool lRendererVerbose) =>
        lRendererVerbose
            ? Path.Combine(LRendererLogCreate(), $"flyleaf-debug-{Environment.ProcessId}.log")
            : string.Empty;

    private static string LRendererLogCreate()
    {
        string lRendererLogFolder = LFlyleafFolder.LFlyleafRootRead();
        Directory.CreateDirectory(lRendererLogFolder);
        return lRendererLogFolder;
    }

    internal static void LRendererVerboseApply(bool lRendererVerbose)
    {
        try
        {
            LRendererVerboseSeam?.Invoke(lRendererVerbose);
        }
        catch (Exception lException)
        {
            LTraceLog.LTraceErrorRecord("Renderer log switch failed", lException);
        }
    }
}
