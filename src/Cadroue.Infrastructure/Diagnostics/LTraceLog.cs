namespace Cadroue.Infrastructure;

public static class LTraceLog
{
    public static void LTraceInfoRecord(string lMessage)
    {
        LTrace.LTraceRecord(LTraceKind.LTraceInfo, lMessage);
    }

    public static void LTraceInfoRecord(string lMessage, string? lDetail)
    {
        LTrace.LTraceRecord(LTraceKind.LTraceInfo, lMessage, lDetail);
    }

    public static void LTraceLoadingRecord(string lMessage, string? lDetail = null)
    {
        LTrace.LTraceRecord(LTraceKind.LTraceLoading, lMessage, lDetail);
    }

    public static void LTraceWarningRecord(string lMessage, string? lDetail = null)
    {
        LTrace.LTraceRecord(LTraceKind.LTraceWarning, lMessage, lDetail);
    }

    public static void LTraceErrorRecord(string lMessage, Exception? lException = null)
    {
        LTrace.LTraceRecord(LTraceKind.LTraceError, lMessage, lException?.ToString());
    }

    public static void LTraceFfmpegRecord(string lMessage, string? lDetail = null)
    {
        LTrace.LTraceRecord(LTraceKind.LTraceFfmpeg, lMessage, lDetail);
    }

    public static void LTraceInteractionRecord(string lMessage, string? lDetail = null)
    {
        LTrace.LTraceRecord(LTraceKind.LTraceInteraction, lMessage, lDetail);
    }

    public static void LTraceClear()
    {
        LTraceWriter.LTraceWriterClear();
        LTrace.LTraceReset();
    }
}
