# TAuditRing.cs

## `public sealed class TAuditRing`

Keeps the project edges as written: every `ProjectReference` under `src` matches the ring table.
What a source may name across an edge is the chain's concern, not this one.
The project files are read as XML, so attribute order and quoting cannot hide an edge.
The same edges are audited by `auditstructure.ps1` from its own configuration, and neither reads the other.

## `private const string TAuditRingAudit = "AUDITRING";`

The audit label every message opens with.

## `private const string TAuditRingSource = "src/";`

The folder every project lives under.

## `private const string TAuditTransitiveKind = "Transitive";`

The ceiling key of the cut projects that still compile against rings past their neighbour.

## `private static readonly string[] TAuditImportNames`

The files MSBuild imports on its own from every folder above a project.

## `private static readonly string[] TAuditItemNames`

The items that bring code or a reference into a build.

## `public void AuditRing_Projects_ReferenceInward()`

Reads every `.csproj` under `src` and holds its edges to the ring table exactly.
An edge added or dropped without an edit to the table fails.

## `public void AuditRing_Projects_HideNoReference()`

No project reaches code the ring table cannot see.
A `Reference` with a `HintPath` or a `Compile` linked from outside the project folder fails.
Any reference or source item in a `Directory.Build` import fails too, since it applies to every project.

## `public void AuditRing_CutProjects_CompileAgainstNeighbour()`

The cut projects that do not disable transitive project references stay within their ceiling.
Until they do, a UI project compiles against every ring below its neighbour, and only the tests hold the cut.
Disabling them breaks the build until the `cross` ceilings reach zero, so it is the cut's last step.

## `public void AuditRing_Ceiling_MatchesHits()`

The transitive ceiling equals its count, so a project that closes its references lowers it.

## `private static List<string> TAuditOpenRead()`

The cut projects whose file does not set `DisableTransitiveProjectReferences` to true.

## `private static IReadOnlyList<string> TAuditProjectRead()`

The tracked project files under `src`.

## `private static string[] TAuditEdgeRead(string csproj)`

The project names one `.csproj` references, sorted.
