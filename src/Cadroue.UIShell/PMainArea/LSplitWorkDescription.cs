using Cadroue.Core;

namespace Cadroue.UIShell.PMainArea;

public sealed record LSplitSectionDescription(
    TimeSpan LSplitSectionStart,
    TimeSpan LSplitSectionEnd,
    string LSplitSectionName,
    string LSplitSectionPrefix = "",
    string LSplitSectionSuffix = "");

public sealed record LSplitWorkDescription(
    string? LSplitSourcePath,
    IReadOnlyList<LSplitSectionDescription> LSplitSections,
    LWorkOutput LSplitOutput);
