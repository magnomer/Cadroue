# TAuditName.cs

## `public sealed class TAuditName`

Runs the name audit over every source the naming scope enumerates.
The scope comes from `TAuditNameSetting`, so the file set follows the naming configuration alone.

## `public void AuditName_AllSourceNames_ReportsNoViolation()`

Loads the registry, enumerates the sources, and hands both to `TAuditNameWalker`.
An empty source list fails, since the audit would otherwise pass vacuously.
Every violation is listed with its file, line, kind, name, and reason.

## `public void AuditName_Exempt_MatchesSource()`

Every registry exemption still spares a name in the sources, so a stale row is removed from the registry.

## `public void AuditName_Types_PrefixMatchesRing()`

Counts every type whose prefix lies outside the ring of its project.
A ring is a project folder, and `TAuditPrefixRings` lists the prefixes it allows.
The count must equal `TAuditPrefixCeiling`, so it fails above the ceiling and when the ceiling is stale.
The ceiling only falls as types move to their ring or take its prefix.

## `private static List<string> TAuditRingRead(string repoRoot, IReadOnlyList<string> sources)`

Parses every C# source inside a ring and reads the prefix of each type and delegate it declares.
A name without a prefix is left to the name fact, so no type counts twice.
A source outside every ring, such as a script helper, is skipped.
Each hit prints as `path:line [Kind] Name - reason`, sorted as the script sorts it.

## `private static string TViolationFormat(string repoRoot, IReadOnlyList<TViolation> violations)`

Sorts the violations by repo-relative path, then line, then name, as auditnames.ps1 sorts its hits.
Renders one `path:line [Kind] Name - reason` line per violation, the same line the script prints.
