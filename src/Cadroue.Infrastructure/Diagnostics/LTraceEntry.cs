namespace Cadroue.Infrastructure;

public sealed record LTraceEntry(
    string LTraceEntryTime,
    string LTraceEntryDelta,
    LTraceKind LTraceEntryKind,
    string LTraceEntrySummary,
    string? LTraceEntryDetail,
    double? LTraceEntrySpan)
{
    public bool LTraceEntryDetailed => !string.IsNullOrWhiteSpace(LTraceEntryDetail);
}
