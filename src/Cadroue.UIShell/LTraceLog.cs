namespace Cadroue.UIShell;

public static class LTraceLog
{
    public const string LTraceLogFolder = LTraceWriter.LTraceFolderName;

    public static string LTraceFolderFind() => LTraceWriter.LTraceFolderRead();

    public static string LTracePathFind() => LTraceWriter.LTracePathRead();

    public static void LTraceInfoRecord(string lMessage)
    {
        LTrace.LTraceRecord(LTraceKind.LTraceInfo, lMessage);
    }

    public static void LTraceInfoRecord(string lMessage, string? lDetail)
    {
        LTrace.LTraceRecord(LTraceKind.LTraceInfo, lMessage, lDetail);
    }

    public static void LTraceErrorRecord(string lMessage, Exception? lException = null)
    {
        LTrace.LTraceRecord(LTraceKind.LTraceError, lMessage, lException?.ToString());
    }

    public static string LTraceTextRead() => LTraceWriter.LTraceWriterRead();

    public static void LTraceClear()
    {
        LTraceWriter.LTraceWriterClear();
        LTrace.LTraceReset();
    }
}
