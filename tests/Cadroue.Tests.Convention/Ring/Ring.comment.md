# Ring

This folder holds the ring shape audits of purity, census, boundary and charter, each with its setting.
The purity and census audits also hold a walker.

## `TAuditBoundary`

Keeps the shell on its side of the state rule.
The shell sends what was written and shows what the engine holds.
It never turns text or a chosen id into a stored state itself.
Every name and file the facts hold to lives in `TAuditBoundarySetting`.
Every shell source is scanned for the calls that read text or an id into a state.
The request that should carry the raw field instead is easy to find from the hit.
Every shell source and markup but the listed converters is scanned for a state name.
A panel that needs to know whether a value is unknown asks a verdict rather than reading the state.
Every shell source and markup is scanned for a construct that hides code from the walkers.
Every shell source but the loaders is scanned for the reflection namespace.
Every tracked file under `src` and `tests` that the audits would skip by segment, suffix or prefix is listed.
A markdown suffix is not counted.
A source named like a generated file would otherwise never be walked.
Every source outside the shell roots is scanned for a type that matches the panel pattern.
A panel declared in another project would sit outside every shell audit.
Every shell file whose name a hold pattern matches is scanned for a timer of its own.
A second wait in a panel would write twice or out of order.
An owner already waits out the typing.
Every listed converter must still compare a state.
Every loader must still reflect.
Every hold pattern must name a file.
A row that spares or names nothing is stale and must be removed.
The facts share one scan method, and the shell is whatever the shell, capsule and reach includes name.
So a new shell assembly joins without an edit here.
An empty enumeration fails rather than passing vacuously.
Each scan hit is one line naming the file, the line and the pattern found.
The chooser sees the file name and the patterns are regular expressions.

## `TAuditBoundarySetting`

Hand-written and tracked, with no script writing it.
It holds the names and files the boundary facts hold the shell to.
The forbidden calls read text or an id into a state, plus a `using static` hiding a logic name.
Each is a regular expression, so a space before the parenthesis does not slip past.
The state names are matched bare, so a qualified read, markup and a `using static` are caught alike.
A bare name followed by `:` or `=` is a declaration or a named argument and is not a read.
The converter, loader and hold lists name the files the facts spare, require to reflect, or inspect for a timer.
A converter is still a surface type, so the strict audit counts every branch in it.
The hidden constructs keep code out of a syntax walk.
They are a preprocessor branch, reflection, `dynamic` and inline markup code.
They also include an enum parsed from text, a `using` alias and an `extern alias`.
Each gives a logic name a second spelling.
The walkers parse without symbols, so a branch would be skipped, and reflection names nothing the walk can follow.
No file outside the loader list may name reflection.
The timer pattern is the construct a panel must not use for its own wait.
A file whose name a hold pattern matches is scanned for it.
The panel pattern declares a panel type by its prefix, and only the shell may hold one.

## `TAuditCensus`

Keeps the census of every ring.
It counts the files and types a ring holds, and bans the words listed for a folder.
A ring holds no word listed for its folder, enough source files and no squatting type.
The same census is audited by `AuditStructure.ps1` from its own configuration, and neither reads the other.
No bound source under a folder names a word listed for it.
A word listed for a folder marks a duty of another ring.
So the folder may not name it.
Every ring with a floor holds at least that many source files.
A ring emptied by mistake would otherwise pass every border fact vacuously.
No ring declares a type whose name matches a squatting pattern written for it.

## `TAuditCensusSetting`

Hand-written and tracked, with no script writing it and no script configuration read.
`AuditStructure.json` holds its own copy, kept in step by hand as scripts/principles.md asks.
The smuggling words match `smugglingWords` in scripts/AuditStructure.json.
The floor is the fewest source files a ring may hold.
Raise a floor when a plan lifts more code into the ring, never lower it.
A squatting pattern is a type name a ring may not declare.
Such a type belongs to another ring, so one declared here is misplaced.

## `TAuditCensusWalker`

Reads what each ring holds, meaning its declared types and its source file count.
A ring is any project the border names, read from the bound trees.
Every class, struct, record, interface and enum declared inside a ring is a `declared` hit.
The hit names the ring and the type.

## `TAuditCharter`

Keeps the project edges as written.
Every `ProjectReference` under `src` matches the charter.
What a source may name across an edge is the border's concern, not this one.
The project files are read as XML, so attribute order and quoting cannot hide an edge.
The same edges are audited by `AuditStructure.ps1` from its own configuration, and neither reads the other.
An edge added or dropped without an edit to the charter is `Rerouting` and fails.
Every ring's neighbour plus its capsule equals its project references, and every walked ring is a project.
The host ring is skipped, since it references every project.
A ring dropped from the border's neighbour table would stop being walked, so the table is tied to the projects.
No project reaches code the charter cannot see, which would be `Backdooring`.
A `Reference` with a `HintPath` or a `Compile` linked from outside the project folder fails.
Any reference or source item in a `Directory.Build` import fails too, since it applies to every project.
The imports checked are every `Directory.Build.props` and `Directory.Build.targets` file Git lists, whatever the folder.
The items checked are those that bring code or a reference into a build.
Cut projects that do not disable transitive project references stay within their `Piggybacking` ceiling.
The `Piggybacking` ceiling equals its count, so a project that closes its references lowers it.

## `TAuditCharterSetting`

Hand-written and tracked, with no script writing it.
The project edge table lives here, as project name to referenced project names.
The fact holds the `.csproj` files to this table exactly, so a new edge is an edit here first.
Each ring's edges but the host's equal its border neighbour plus its capsule.
The `Piggybacking` ceiling counts the cut projects that still compile against rings past their neighbour.
A count above fails the fact, and a ceiling above the count is stale and fails too.

## `TAuditPurity`

Keeps every pure ring inside its frame, which limits what a ring may name outside the project.
A pure ring names only the framework namespaces the frame lists and touches no eavesdropping member.
The eavesdropping members are project-specific values in the project's settings files.
The same purity is audited by `AuditStructure.ps1` from its own configuration, and neither reads the other.
The hits are bound once and shared by every fact.
No pair holds more `Foraging` hits, naming a namespace outside the frame, than its ceiling.
No pair holds more `Eavesdropping` hits, touching an eavesdropping member, than its ceiling.
No ceiling sits above its count, so a shed hit lowers its ceiling.
Every exempt row still matches a hit, so a fixed break deletes its row.

## `TAuditPuritySetting`

Hand-written and tracked, with no script writing it and no script configuration read.
Both copies change together, as scripts/principles.md asks.
`AuditStructure.json` holds its own copy of the ring frames for the structure script.
The pure rings are held to the frame and kept from every eavesdropping member.
The rings outside the purity name what they need.
The frame lists the framework namespaces a pure ring may name, and any other namespace belongs behind a port.
The extra namespaces one pure ring adds match the ring frames of scripts/AuditStructure.json.
A member matches when its full name equals a row or sits under it.
The ceiling table keeps its declaration even when empty, so the next break has a row to fail.
Lower a ceiling when a ring sheds a name, never raise one to admit a new one.
Exempt rows are `path:name` and break the purity, one row per break.
A path ending in `/*` exempts a whole folder for one name.
Every row must still match a hit, so a fixed break deletes its row.

## `TAuditPurityWalker`

Reads every framework namespace and eavesdropping member a pure ring names.
A namespace is read twice, from the using directive and from the symbol a name binds to.
A package the project references beyond the runtime stays unbound, so its using directive is what is read.
The trees of the pure rings are scanned in parallel.
Hits come back ordered by file and line.
A namespace outside the frame is a `Foraging` hit and a member under an eavesdropping row an `Eavesdropping` hit.
One hit is kept per line, kind and name, so a name used twice on a line counts once.
A namespace is outside only when it is in neither the shared frame nor the ring's extra frame.
It matches whole, so `System` never admits `System.IO`.
A member appends its own name, so a member is told apart from its type.

## Stamp

Commit: `90a13c9590ba`
- `TAuditBoundary.cs` `51ad50d413ce0aab`
- `TAuditBoundarySetting.cs` `927a29bfdba67c3f`
- `TAuditCensus.cs` `1927cbc5ad714acf`
- `TAuditCensusSetting.cs` `a08172799eac3a52`
- `TAuditCensusWalker.cs` `301bfbc58a822e20`
- `TAuditCharter.cs` `9640ab92d9a80db5`
- `TAuditCharterSetting.cs` `174df8f90e8921ad`
- `TAuditPurity.cs` `9acbad32aaa34359`
- `TAuditPuritySetting.cs` `20a659b55370b1f9`
- `TAuditPurityWalker.cs` `77f32b04ab93854e`
