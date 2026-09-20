namespace Convention.Tests;

internal sealed record TAuditLineRow(
    string TAuditLinePath,
    int TAuditLineCount,
    int TAuditLineWidth,
    IReadOnlyList<(int TAuditNumber, int TAuditWidth)> TAuditLineWide);
