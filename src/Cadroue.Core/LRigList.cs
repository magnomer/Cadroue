namespace Cadroue.Core;

// A list's folder scan as callables, with the trace kinds its adds record. The scanner answers the media paths it
// found and each folder it skipped with the reason; the info and warning kinds take an optional detail line.
public sealed record LRigList(
    Func<IReadOnlyList<string>, CancellationToken,
        Task<(IReadOnlyList<string> LRigListPaths, IReadOnlyList<(string, string)> LRigListSkips)>> LRigListScanner,
    Action<string, string?> LRigListInfo,
    Action<string, string?> LRigListWarning,
    Action<string, Exception?> LRigListError);
