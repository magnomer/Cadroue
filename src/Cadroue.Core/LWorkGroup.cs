namespace Cadroue.Core;

public sealed record LWorkGroup(
    string LWorkGroupName,
    IReadOnlyList<string> LWorkGroupPaths);
