namespace Convention.Tests;

internal sealed record TAuditLineRow(
    string TAuditLinePath,
    int TAuditLineTotal,
    int TAuditLineWidth,
    IReadOnlyList<(int TAuditNumber, int TAuditWidth)> TAuditLineWide);
