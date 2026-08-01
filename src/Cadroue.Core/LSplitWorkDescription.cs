namespace Cadroue.Core;

public sealed record LSplitSectionDescription(
    TimeSpan LSplitSectionStart,
    TimeSpan LSplitSectionEnd,
    string LSplitSectionName,
    string LSplitSectionPrefix = "",
    string LSplitSectionSuffix = "",
    bool LSplitSectionHidden = false);

public sealed record LSplitPlanRecord(
    string LSplitPlanSourcePath,
    IReadOnlyList<LSplitSectionDescription> LSplitPlanSections);

public sealed record LSplitWorkDescription(
    string? LSplitSourcePath,
    IReadOnlyList<LSplitSectionDescription> LSplitSections,
    LWorkOutput LSplitOutput);
