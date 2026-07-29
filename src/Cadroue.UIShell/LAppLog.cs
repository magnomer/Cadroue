namespace Cadroue.UIShell;

public static class LAppLog
{
    public const string LLogFolderName = LTraceWriter.LTraceFolderName;

    public static string LLogFolderRead() => LTraceWriter.LTraceFolderRead();

    public static string LLogPathRead() => LTraceWriter.LTracePathRead();

    public static void LInfo(string lMessage)
    {
        LTrace.LTraceRecord(LTraceKind.LTraceInfo, lMessage);
    }

    public static void LInfo(string lMessage, string? lDetail)
    {
        LTrace.LTraceRecord(LTraceKind.LTraceInfo, lMessage, lDetail);
    }

    public static void LError(string lMessage, Exception? lException = null)
    {
        LTrace.LTraceRecord(LTraceKind.LTraceError, lMessage, lException?.ToString());
    }

    public static string LTextRead() => LTraceWriter.LTraceWriterRead();

    public static void LClear()
    {
        LTraceWriter.LTraceWriterClear();
        LTrace.LTraceReset();
    }
}
