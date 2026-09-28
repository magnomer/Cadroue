namespace Convention.Tests;

internal sealed class TAuditObjectPart(string path, int lines)
{
    public string TAuditPartPath { get; } = path;
    public int TAuditPartLines { get; } = lines;
}
