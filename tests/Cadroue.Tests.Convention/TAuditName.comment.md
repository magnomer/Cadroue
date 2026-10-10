# TAuditName.cs
Hash: `250cd3968e9a7bc9`

## `public sealed class TAuditName`

Runs the name audit over every source the naming scope enumerates.
The scope comes from `TAuditNameSetting`, so the file set follows the naming configuration alone.

## `public void AuditName_AllSourceNames_ReportsNoViolation()`

Loads the registry, enumerates the sources, and hands both to `TAuditNameWalker`.
An empty source list fails, since the audit would otherwise pass vacuously.
Every violation is listed with its file, line, kind, name, and reason.

## `public void AuditName_Exempt_MatchesSource()`

Every registry exemption still spares a name in the sources, so a stale row is removed from the registry.

## `public void AuditName_Types_PrefixMatchesTurf()`

Counts every type whose prefix lies outside the turf of its project.
A turf is a project folder, and `TAuditPrefixTurfs` lists the prefixes it allows.
A public type in a sealed turf is left to its own fact, so it never hides under the ceiling.
The count must equal `TAuditPrefixCeiling`, so it fails above the ceiling and when the ceiling is stale.
The ceiling only falls as types move to their turf or take its prefix.

## `public void AuditName_SealedTurfs_HoldNoPublic()`

No out-of-turf type in a sealed turf is public on itself and on every containing type.
A sealed turf such as Conduct exposes only its own prefix, so a stray public type leaks its turf.
The hit is hard, with no ceiling, and the script reports it as the `Sealed turf` gate.

## `public void AuditName_PublicSealedTurf_ReportsOneHit()`

Proves a parsed `public sealed class LDisplay` under a Conduct path gives one sealed hit.

## `public void AuditName_InternalSealedTurf_AllowsTheType()`

Proves the same class declared `internal` gives no sealed hit, so it stays in the turf count.

## `internal static IEnumerable<(int TAuditLine, string TAuditType, string TAuditText, bool TAuditSealed)> TAuditTurfScan(string relative, SyntaxNode root)`

Yields each type and delegate of one parsed source whose prefix lies outside the turf of its repo-relative path.
It binds nothing, so a specimen can feed it a hand-written source.
A name without a prefix is left to the name fact, so no type counts twice.
A source outside every turf, such as a script helper, gives nothing.
A hit is sealed when its turf is sealed and the syntax marks it public up the whole chain.
A sealed hit adds `and public in a sealed turf` to the line the script prints too.

## `private static bool TAuditPublicCheck(SyntaxNode node)`

Whether the declaration and every type declaration around it carry the `public` keyword.
Accessibility is read from syntax, as the script reads it, since neither side binds the turf scan.

## `private static List<(string TAuditText, bool TAuditSealed)> TAuditTurfRead()`

Parses every C# source inside a turf and scans it with `TAuditTurfScan`.
Each hit prints as `path:line [Kind] Name - reason`, sorted as the script sorts it.

## `private static string TViolationFormat(string repoRoot, IReadOnlyList<TViolation> violations)`

Sorts the violations by repo-relative path, then line, then name, as AuditNames.ps1 sorts its hits.
Renders one `path:line [Kind] Name - reason` line per violation, the same line the script prints.
