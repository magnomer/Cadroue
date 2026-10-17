using Cadroue.Core;
using Cadroue.Media;

namespace Cadroue.Infrastructure;

public static class LRigFactoryList
{
    // The lists' folder scan over the media scan, with the trace kinds their adds record.
    public static LRigList LRigListCreate() =>
        new(
            LRigListScan,
            LTraceLog.LTraceInfoRecord,
            LTraceLog.LTraceWarningRecord,
            LTraceLog.LTraceErrorRecord);

    private static async Task<(IReadOnlyList<string>, IReadOnlyList<(string, string)>)> LRigListScan(
        IReadOnlyList<string> lPaths, CancellationToken lToken)
    {
        LMediaScanResult lScan = await LMediaPath.LMediaPathScan(lPaths, lToken);
        return (lScan.LMediaScanPaths,
            lScan.LMediaScanNotices.Select(lNotice => (lNotice.LMediaScanFolder, lNotice.LMediaScanReason)).ToArray());
    }
}
