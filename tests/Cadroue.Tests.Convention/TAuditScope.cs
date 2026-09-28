namespace Convention.Tests;

internal sealed record TAuditScope(
    IReadOnlyList<string> TAuditScopeRoots,
    IReadOnlyList<string> TAuditScopeInclude,
    IReadOnlyList<string> TAuditScopeSegments,
    IReadOnlyList<string> TAuditScopeSuffixes,
    IReadOnlyList<string> TAuditScopePrefixes,
    IReadOnlyList<string> TAuditScopeFiles);
