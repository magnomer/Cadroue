namespace Cadroue.UIShell.PMainArea;

public sealed record LSplitSectionDescription(
    TimeSpan LSplitSectionStart,
    TimeSpan LSplitSectionEnd,
    string LSplitSectionName);

public sealed record LSplitWorkDescription(
    string? LSplitSourcePath,
    IReadOnlyList<LSplitSectionDescription> LSplitSections);
