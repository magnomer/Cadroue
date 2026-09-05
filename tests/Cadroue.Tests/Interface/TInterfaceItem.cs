using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.ShellEngine;

namespace Cadroue.Tests;

/// <summary>
/// Test-side boundary for the per-mode work item builders.
/// </summary>
internal static partial class TInterface
{
    internal static LWorkItem? TAudioItemCreate(
        LWorkPriority priority, string? source, LWorkAudio processing, LEncoding output, string tab,
        Action<string> infoLog, Action<string> errorLog, Func<string, TimeSpan> durationRead, Guid batchId = default) =>
        LAudio.LAudioItemCreate(priority, source, processing, output, tab, infoLog, errorLog, durationRead, batchId);

    internal static IReadOnlyList<LWorkItem> TConvertItemsCreate(
        LWorkPriority priority, LConvertWorkDescription description, string tab,
        Action<string> errorLog, Func<string, TimeSpan> durationRead) =>
        LConvert.LConvertItemsCreate(priority, description, tab, errorLog, durationRead);

    internal static IReadOnlyList<LWorkItem> TEditItemsCreate(
        LWorkPriority priority, LEditWorkDescription description, string tab,
        Action<string> infoLog, Action<string> errorLog, Guid batchId = default) =>
        LEdit.LEditItemsCreate(priority, description, tab, infoLog, errorLog, batchId);

    internal static IReadOnlyList<LWorkItem> TFixItemsCreate(
        LWorkPriority priority, LFixWorkDescription description, string tab,
        Action<string> errorLog, Func<string, TimeSpan> durationRead) =>
        LFix.LFixItemsCreate(priority, description, tab, errorLog, durationRead);

    internal static IReadOnlyList<LWorkItem> TMergeItemsCreate(
        LWorkPriority priority, IReadOnlyList<LWorkGroup> groups, LEncoding output, string tab,
        Action<string> infoLog, Action<string> errorLog, IReadOnlyDictionary<string, Guid>? relays = null) =>
        LMerge.LMergeItemsCreate(priority, groups, output, tab, infoLog, errorLog, relays);

    internal static IReadOnlyList<LWorkItem> TSplitItemsCreate(
        LWorkPriority priority, LSplitWorkDescription description, string tab,
        Action<string> infoLog, Action<string> errorLog, Guid batchId = default) =>
        LSplit.LSplitItemsCreate(priority, description, tab, infoLog, errorLog, batchId);
}
