namespace Cadroue.Core;

public sealed record LSplitSectionDescription(
    TimeSpan LSplitSectionOrigin,
    TimeSpan LSplitSectionEnd,
    string LSplitSectionName,
    string LSplitSectionPrefix = "",
    string LSplitSectionSuffix = "",
    bool LSplitSectionHidden = false);

public sealed record LSplitPlanRecord(
    string LSplitSourcePath,
    IReadOnlyList<LSplitSectionDescription> LSplitPlanSections);

public sealed record LSplitWorkDescription(
    string? LSplitSourcePath,
    IReadOnlyList<LSplitSectionDescription> LSplitSections,
    LEncoding LSplitOutput);
