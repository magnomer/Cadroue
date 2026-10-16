using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Media;

namespace Cadroue.Infrastructure;

public static class LRigFactoryTrace
{
    public static void LRigTraceAttach()
    {
        LLocalization.LLocalizationTraceSeam = LTraceLog.LTraceErrorRecord;
        LMediaTrace.LMediaErrorSeam = LTraceLog.LTraceErrorRecord;
        LMediaTrace.LMediaOpenSeam = LTraceLog.LTraceInfoRecord;
        LClassifier.LClassifierFaultSource = lClassifierFault => LTraceLog.LTraceWarningRecord(lClassifierFault);
    }

    public static LRigTrace LRigTraceCreate()
    {
        return new LRigTrace(
            LTraceLog.LTraceInfoRecord,
            message => LTraceLog.LTraceWarningRecord(message),
            LTraceLog.LTraceErrorRecord,
            LTraceLog.LTraceFfmpegRecord,
            () => LTrace.LTraceVerbose,
            LTraceDetailRecord);
    }

    private static void LTraceDetailRecord(bool lUi, string lSummary, string? lDetail)
    {
        if (lUi)
        {
            LTrace.LTraceRecord(LTraceKind.LTraceUi, lSummary, lDetail);
            return;
        }

        LTraceLog.LTraceInfoRecord(lSummary, lDetail);
    }
}
