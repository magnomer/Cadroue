# TAuditTruth.cs
Hash: `bb581b1016b16e37`

## `public sealed class TAuditTruth`

The driver rules say a driver gathers input, calls a Conduct gate and shows the verdict.
It never builds, checks or decides what a gate owns, and it reaches the engine only through Conduct.
The rules hold Deportment and Demeanor alike.
The walk runs once and every fact reads the same hits.
While `TAuditTruthEnforced` is false every fact passes and only prints its count.
Enforced, a kind fails in any file whose count stands above its ledger ceiling.
The ledger keys each ceiling by file, so each medium's lag is its own.
A new file starts at zero.
`TAssayTruth` and its `TAssay` siblings assay the walker on hand-written sources.
Assays check the walker, not the tree, so they gate no tree result and no ledger ceiling.

## `private const string TAuditTruthAudit = "AUDITTRUTH";`

The audit label every message opens with.

## `private static readonly Lazy<(IReadOnlyList<TViolation> TAuditHits, IReadOnlyList<string> TAuditSources)>`

The hits of every driver walk and the driver sources they read, computed once.

## `private static readonly Lazy<string> TAuditTruthWritten = new(TAuditReportSave);`

The report is written once, on the first fact that runs.

## `private static readonly string[] TAuditTruthKinds`

The hit kinds in report order, and the kinds the ledger may hold.

## `private static readonly string[] TAuditLineKinds`

The kinds whose hits name a line rather than a field, left out of the field table.

## `public TAuditTruth(ITestOutputHelper output)`

Keeps the runner's output so a passing fact can still print its count.

## `public void AuditTruth_DriverFields_ReachNoRequest()`

A driver value that reaches a request, as an argument or as the condition around one.
A request is a call to a Conduct gate, to the engine or to a driver relay of either.
Gatekeeping counts every condition: `if`, `?:`, `switch`, loops, `when` clauses, catch filters, `?.`, `&&`, `||` and `??`.
Gatekeeping through a getter property counts as a read of the field it returns.
An engine answer or a dialog answer deciding a request is gatekeeping too, since the gate owns that decision.
Zeroing is a literal `0`, `0L` or `default` sent toward a gate, where it means none, new, silence or append.
That meaning belongs to the gate, so a driver that spells it out decides for the gate.
A literal compared with a member that reaches a gate counts too.
Zeroing reads a wider hot map than Replaying, with constructors and stored parameters added.

## `public void AuditTruth_DriverFields_KeepOneWriter()`

A driver field written from an engine result in one place and from the driver in another.
Two writers make a copy that drifts from what the engine holds.

## `public void AuditTruth_DriverFields_HoldNoEngine()`

A driver field or settable property holding a type from below Conduct.
Also a field named as a state, typed `object`, or caching the answer to a request.
Also a driver type deriving from or implementing a type from below Conduct.
A Conduct type is held freely, and a Conduct port is implemented freely.

## `public void AuditTruth_DriverSources_MutateNoLogic()`

A driver line that assigns a logic member, reorders a collection or overrides a built request.

## `public void AuditTruth_DriverRequests_HoldNoMisfiring()`

A control or console input deciding a request, a clock driving one, or an observer that never reads its bulletin.
Also a member sending a second request, since one user action is one gate.

## `public void AuditTruth_DriverSources_HoldNoMoonlighting()`

A driver line that compares, computes or queries over a value from below Conduct.
Reshaping a Conduct verdict is the driver's work and is not moonlighting.

## `public void AuditTruth_DriverNames_HoldNoHomoglyph()`

A driver identifier holding a character outside ASCII.
A look-alike glyph would let a name pass every prefix rule while reading as another.

## `public void AuditTruth_DriverLocals_CarryNoData()`

A driver line that computes over a carried engine value, or over the input of a control or the console.

## `public void AuditTruth_DriverSources_HoldNoSpoonfeeding()`

A driver line handing its surface a type from below the cut, Conduct's included.
A surface binding reads the members of what it is fed, so spoonfeeding is where the cut is crossed.

## `public void AuditTruth_DriverGates_HoldNoMismatching()`

A Conduct member one driver reaches and the other never does.
Also a Conduct port a driver does not implement.
Both drivers call the same gate for the same action, so an asymmetry is medium work or a leak.
It only informs while Demeanor holds no source, as `TAuditTruthInformative` sets.

## `public void AuditTruth_ControlTypes_HoldNoPuppeteering()`

A member of a control type that requests logic, directly, through a relay or through a lambda.
A control type is a driver type whose name starts with its ring's prefix in `TAuditControlPrefix`.
A control shows what its driver hands it, so the request belongs in the driver.
Reading a Conduct record property is no request and stays legal.

## `public void AuditTruth_Ledger_MatchesHits()`

Every ledger ceiling equals its count, so a ceiling left above the count is stale.
The lowered ledger is written under `temp/audit` to copy over the tracked one.

## `public void AuditTruth_Sources_WalkEveryFile()`

Every tracked driver source reached the walkers.
An empty driver passes, but a file the binder dropped does not pass unseen.

## `private void TAuditTruthCheck(string kind, string summary)`

Holds one kind against the ledger and prints the count and the report path.
An informative kind waiting on an empty driver prints its count and fails nothing.

## `private static bool TAuditEnforceCheck(string kind)`

True when the kind gates now.
A kind outside `TAuditTruthInformative` always gates.
A listed kind gates once every folder it waits on holds a walked source.

## `private static int TAuditKindRead(string kind)`

The hits of one kind.

## `private static string TAuditReportSave()`

Writes the markdown report under `temp/audit` and returns its repo-relative path.
The report opens with the counts, then the fields by hit count, then every hit by kind.

## `private static (IReadOnlyList<TViolation> TAuditHits, IReadOnlyList<string> TAuditSources) TAuditTruthRead()`

Enumerates every driver source with Git and runs the driver, moonlighting and laundering walkers once.
Paths are made repo-relative.
