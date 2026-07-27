using Cadroue.Core;

namespace Cadroue.UIShell.PMainArea;

public sealed record LSplitSectionDescription(
    TimeSpan LSplitSectionStart,
    TimeSpan LSplitSectionEnd,
    string LSplitSectionName);

/// <summary>
/// Everything split mode knows at the moment Add List was pressed: which source,
/// which sections, and the export settings snapshot to produce them with.
/// </summary>
public sealed record LSplitWorkDescription(
    string? LSplitSourcePath,
    IReadOnlyList<LSplitSectionDescription> LSplitSections,
    LWorkOutput LSplitOutput);
