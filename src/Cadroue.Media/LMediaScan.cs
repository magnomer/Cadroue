namespace Cadroue.Media;

public sealed record LMediaScanNotice(string LMediaScanFolder, string LMediaScanReason);

public sealed record LMediaScanResult(
    IReadOnlyList<string> LMediaScanPaths,
    IReadOnlyList<LMediaScanNotice> LMediaScanNotices);
