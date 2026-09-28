# TAuditStrict.cs

## `public sealed class TAuditStrict`

The surface rules: a surface is what the user sees, and a surface member only calls a function.
A surface holds no state, never branches or computes, and names only its driver.
The rules hold the Veneer and UITerminal alike, and Host is held to construction and wiring.
A surface type holds no member but a constructor, and its markup holds no hook into logic.
The Veneer audit is the `Surface` facts, and it gates every batch of the Great Purge.
While `TAuditStrictEnforced` is false every fact passes and reports as a warning.
Enforced, a kind fails in any file whose count stands above its ledger ceiling.
The ledger keys each ceiling by file, so each medium's lag is its own.
A new file starts at zero.

## `private const string TAuditStrictAudit = "AUDITSTRICT";`

The audit label every message opens with.

## `private static readonly Lazy<(IReadOnlyList<TViolation> TAuditHits, IReadOnlyList<string> TAuditVeneers,`

The walk runs once and every fact reads the same hits, surface types and walked sources.

## `private static readonly Lazy<string> TAuditStrictWritten = new(TAuditReportSave);`

The report is written once, on the first fact that runs.

## `private static readonly string[] TAuditStrictKinds`

The hit kinds in report order, and the kinds the ledger may hold.

## `private static readonly Regex TAuditLiteralPattern`

A string literal or a line comment, the text the source scan reads past.

## `public TAuditStrict(ITestOutputHelper output)`

Keeps the runner's output so a passing fact can still print its count.

## `public void AuditStrict_SurfaceFields_HoldNothing()`

A field, event field, auto-property or primary constructor parameter in a surface type.
A constant is no state and is left out.
A `DependencyProperty` or `RoutedEvent` static counts, since the surface allows no exception.

## `public void AuditStrict_DriverStatics_HoldNothing()`

A mutable static field in a driver type, state shared past any one gate.

## `public void AuditStrict_SurfaceMembers_CallOnly()`

A surface line that is not a plain call: a branch, a loop, an operator, an assignment or a declaration.
A LINQ query is a breach too, since a filter decides what is drawn.
Every line inside a breaching statement counts, so logic added inside one moves the count.

## `public void AuditStrict_SurfaceSources_NameOnlyDriver()`

A surface line naming a type from below the driver, Conduct's included.
It overlaps the chain audit's `cross` count, which counts the same names per ring pair.

## `public void AuditStrict_SurfaceMarkup_NameOnlyDriver()`

A markup line that maps a namespace below the driver, reads a constant from there, or names a member there.

## `public void AuditStrict_SurfaceMarkup_BranchNever()`

A markup trigger or visual state, or a binding slot that converts, formats, selects or validates.
Each is a branch or a computation standing in the surface.

## `public void AuditStrict_SurfaceMarkup_HookNever()`

A markup line through which logic reaches the markup: a binding, a command, an input binding or a code reach.
A code reach is `x:Static` or a markup extension whose prefix maps a code namespace.
A command parameter or target, a display member path or a selected value path is a hook too.
So is a literal `Tag`, since code reads it as a value to branch on.
Each counts as an attribute or as the property a `Setter` names.
A converter or a template or style selector declared in the markup is a hook too.
Each line counts once, however many hooks it holds.
`{StaticResource}`, `{DynamicResource}` and `{x:Type}` stay allowed.

## `public void AuditStrict_SurfaceTypes_ShellOnly()`

A surface member that is not a constructor: a method, property, event, indexer or operator.
The shell only constructs, so every handler or helper belongs in Deportment.
An event attribute in markup therefore shows as the missing handler it needs.
Generated code is left out, as for every surface fact.

## `public void AuditStrict_SurfaceNames_HoldNoGlyph()`

A surface identifier holding a character outside ASCII.
A look-alike glyph would let a name pass every prefix rule while reading as another.

## `public void AuditStrict_HostSources_WireOnly()`

A Host line that does more than construct and wire: a branch, a choice, an operator or an early return.
Host holds no behaviour, so each such line belongs in the layer that owns the decision.

## `public void AuditStrict_DriverSource_PackNever()`

A driver line holding a pack URI or naming the Veneer assembly.
Deportment pulls every part by contract ID and never names the Veneer.
The line counts once, under the first marker it holds.

## `public void AuditStrict_DriverTypes_ScaffoldNever()`

A driver type deriving from a scaffold type, such as a window, a page or a resource dictionary.
The Veneer holds the scaffold, so a driver type deriving from one is scaffold standing in the wrong layer.
Each partial part counts in its own file.

## `public void AuditStrict_SurfaceMarkup_ContractHeld()`

A contract ID passed to `QContract` that no `x:Name` or `x:Key` in the surface markup declares.
An ID that is not a constant counts too, since no markup can be checked against it.
Every string argument of a `QContract` call is an ID.

## `public void AuditStrict_DriverSources_TouchNoDisk()`

A driver line that names a file system type, outside the exempt files.
A driver holds only what its medium needs, and disk work stays behind the engine's ports.
The console's reader and writer live in `System.IO` too, so the namespace alone is no hit.

## `public void AuditStrict_SurfaceSources_HoldNoCatalog()`

A surface line that reads a file, parses JSON, runs a regex, starts a process or starts a task.
Each of those is work the engine or `LUsher` does, and the surface only asks for the answer.
A file whose stream use is the framework's own may be exempt by name.

## `public void AuditStrict_Exempt_MatchesSource()`

Every exempt file still holds a line its exemption spares, so a stale exemption is removed.

## `public void AuditStrict_Ledger_MatchesHits()`

Every ledger ceiling equals its count, so a ceiling left above the count is stale.
The lowered ledger is written under `temp/audit` to copy over the tracked one.

## `public void AuditStrict_Sources_WalkEveryFile()`

Every tracked surface, driver and host source reached the walkers.
An empty surface passes, but a file the binder dropped does not pass unseen.

## `private void TAuditStrictCheck(string kind, string summary)`

Holds one kind against the ledger and prints the count and the report path.

## `private static IEnumerable<string> TAuditExemptRead(`

The exempt file names that hold no line the forbidden patterns match.

## `private static List<string> TAuditSourceScan(`

Every line of every included source that matches a forbidden pattern, outside the exempt files.
A string literal and a line comment are blanked first, so a word inside a message is not a hit.
No hit count is kept for these, since both start at zero and stay there.

## `private static (IReadOnlyList<TViolation> TAuditHits, IReadOnlyList<string> TAuditVeneers,`

Enumerates the UI sources, the host sources and the surface markup with Git.
It fails when the UI or host set is empty, since the audit would then pass vacuously.
An empty markup set passes, since the Veneer gets its markup back one batch at a time.
It then runs the four walkers once, with paths made repo-relative.
The contract walker reads the driver sources and the surface markup together.

## `private static IReadOnlyList<string> TAuditScopeRead(string repoRoot, IReadOnlyList<string> include)`

The tracked files matching the patterns, with the usual exclusions.

## `private static string TAuditReportSave()`

Writes the markdown report under `temp/audit` and returns its repo-relative path.
The report opens with the counts, then a per-type table, then every hit by kind.

## `private static int TAuditClassRead(IReadOnlyList<TViolation> hits, string type, string kind)`

The hits of one kind whose name starts with the type.
