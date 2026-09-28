namespace Convention.Tests;

internal sealed class TAuditFakeMember(string key, string name, string path, int line, bool stored)
{
    public string TAuditMemberKey { get; } = key;

    public string TAuditMemberName { get; } = name;

    public string TAuditMemberPath { get; } = path;

    public int TAuditMemberLine { get; } = line;

    public bool TAuditMemberStored { get; } = stored;

    public HashSet<string> TAuditMemberReaders { get; } = new(StringComparer.Ordinal);

    public HashSet<string> TAuditMemberTesters { get; } = new(StringComparer.Ordinal);

    public bool TAuditMemberLive { get; set; }
}
