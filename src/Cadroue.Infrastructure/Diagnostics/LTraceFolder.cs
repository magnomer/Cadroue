using System.IO;

namespace Cadroue.Infrastructure;

public static class LTraceFolder
{
    public const string LTraceFolderName = "log";

    internal static readonly object LTraceFolderLock = new();

    private static readonly string lTraceFileName =
        $"Cadroue-{DateTimeOffset.Now:yyyyMMdd-HHmmss}-{Environment.ProcessId}.log";

    public static string LTraceFolderRead() => Path.Combine(LDepot.LDepotRootRead(), LTraceFolderName);

    public static string LTracePathRead() => Path.Combine(LTraceFolderRead(), lTraceFileName);
}
