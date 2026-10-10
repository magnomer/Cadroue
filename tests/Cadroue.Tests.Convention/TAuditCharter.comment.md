# TAuditCharter.cs
Hash: `9640ab92d9a80db5`

## `public sealed class TAuditCharter`

Keeps the project edges as written.
Every `ProjectReference` under `src` matches the charter.
What a source may name across an edge is the border's concern, not this one.
The project files are read as XML, so attribute order and quoting cannot hide an edge.
The same edges are audited by `AuditStructure.ps1` from its own configuration, and neither reads the other.

## `private const string TAuditCharterAudit = "AUDITCHARTER";`

The audit label every message opens with.

## `private const string TAuditCharterSource = "src/";`

The folder every project lives under.

## `private const string TAuditPiggybackingKind = "Piggybacking";`

The ceiling key of the cut projects that still compile against rings past their neighbour.

## `private static readonly string[] TAuditImportNames`

The files MSBuild imports on its own from every folder above a project.

## `private static readonly string[] TAuditItemNames`

The items that bring code or a reference into a build.

## `public void AuditCharter_Projects_HoldNoRerouting()`

Reads every `.csproj` under `src` and holds its edges to the charter exactly.
An edge added or dropped without an edit to the charter is `Rerouting` and fails.

## `public void AuditCharter_Neighbours_MatchProjects()`

Every ring's neighbour plus its capsule equals its project references, and every walked ring is a project.
The host ring is skipped, since it references every project.
A ring dropped from the border's neighbour table would stop being walked, so the table is tied to the projects.

## `public void AuditCharter_Projects_HoldNoBackdooring()`

No project reaches code the charter cannot see, which would be `Backdooring`.
A `Reference` with a `HintPath` or a `Compile` linked from outside the project folder fails.
Any reference or source item in a `Directory.Build` import fails too, since it applies to every project.

## `public void AuditCharter_CutProjects_HoldNoPiggybacking()`

The cut projects that do not disable transitive project references stay within their `Piggybacking` ceiling.
Until they do, a UI project compiles against every ring below its neighbour, and only the tests hold the cut.
Disabling them breaks the build until the `Undercutting` ceilings reach zero, so it is the cut's last step.

## `public void AuditCharter_Ceiling_MatchesHits()`

The `Piggybacking` ceiling equals its count, so a project that closes its references lowers it.

## `private static List<string> TAuditOpenRead()`

The cut projects whose file does not set `DisableTransitiveProjectReferences` to true.

## `private static IReadOnlyList<string> TAuditProjectRead()`

The tracked project files under `src`.

## `private static string[] TAuditEdgeRead(string csproj)`

The project names one `.csproj` references, sorted.
