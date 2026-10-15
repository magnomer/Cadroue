using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public sealed record QLogFile(string QLogFilePath, string QLogFileLabel);

public sealed class QLogRow
{
    private static readonly IReadOnlyDictionary<LTraceKind, string> QLogRowKeys = new Dictionary<LTraceKind, string>
    {
        [LTraceKind.LTraceLoading] = "Log.Category.Loading",
        [LTraceKind.LTraceWarning] = "Log.Category.Warning",
        [LTraceKind.LTraceError] = "Log.Category.Error",
        [LTraceKind.LTraceInteraction] = "Log.Category.Interaction",
        [LTraceKind.LTraceUi] = "Log.Category.Interface",
        [LTraceKind.LTraceWork] = "Log.Category.Work",
        [LTraceKind.LTraceFfmpeg] = "Log.Category.Ffmpeg",
    };

    private const string QLogInfoKey = "Log.Category.Info";

    public QLogRow(LTraceEntry lEntry)
    {
        QLogRowCategory = lEntry.LTraceEntryKind;
        QLogRowKey = QLogKeyRead(lEntry.LTraceEntryKind);
        QLogRowTime = lEntry.LTraceEntryTime;
        QLogRowKind = QLocalizationCatalog.QLocalizationTextRead(QLogRowKey);
        QLogRowSummary = lEntry.LTraceEntrySummary;
        QLogRowDetail = lEntry.LTraceEntryDetail ?? string.Empty;
        QLogRowDetailed = lEntry.LTraceEntryDetailed;
        QLogRowSpan = lEntry.LTraceEntrySpan is double lSpan ? LTraceEntry.LTraceSpanFormat(lSpan) : string.Empty;
    }

    public LTraceKind QLogRowCategory { get; }

    public string QLogRowKey { get; }

    public string QLogRowTime { get; }

    public string QLogRowKind { get; }

    public string QLogRowSummary { get; }

    public string QLogRowDetail { get; }

    public bool QLogRowDetailed { get; }

    public string QLogRowSpan { get; }

    public bool QLogRowExpanded { get; internal set; }

    public string QLogRowChip =>
        QLocalizationCatalog.QLocalizationTextRead("Log.Button.Details") + (QLogRowExpanded ? "  ▴" : "  ▾");

    public static string QLogKeyRead(LTraceKind lKind) => QLogRowKeys.GetValueOrDefault(lKind, QLogInfoKey);
}
