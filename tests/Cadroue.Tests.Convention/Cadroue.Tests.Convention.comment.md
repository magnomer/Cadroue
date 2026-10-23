# <project>.Tests.Convention

This project builds the tests that inspect source structure and enforce repository conventions.
It uses xUnit and Roslyn and references no runtime project.
The tests read project files and source text, and never invoke application assemblies.
It stands apart from the behaviour tests because every fact here is about the tree, not about running code.

## Counterpart scripts

Each audit here has a counterpart script in `scripts/`, listed in `scripts/principles.md`.
`TAuditRatchet` and `TAuditConvention` check the test settings themselves, so they have none.
A test and its counterpart never depend on each other, so either still tells the truth when the other breaks.
A test never reads a script or its configuration to learn a rule or a setting.
Each test carries its own settings, copied by hand, in its `TAudit*Setting.cs` file or ledger.
Both sides still reach the same verdict, count the same hits and word each hit the same way.
A rule, threshold, ceiling or ledger row changes on both sides in the same change.
The `Microsoft.CodeAnalysis.CSharp` version here is the one every script helper pins.

## Assay files

Every `TAssay*` file but the `TAssayTruth` helper holds small hand-written sources, each with an expected hit or no hit.
Each assay binds through `TAuditBinder`, directly or through `TAssayTruth`, and runs the real entry of the walker it checks.
Assays check the walker, not the tree, so they gate no tree result and touch no ceiling.
They guard rules the tracked tree never reaches, which the tree audit alone would let break unseen.
A failing assay exposes a walker bug, never a source to fix.
Most assays come as a hit beside a no-hit twin that differs by one thing.

## `TAuditConvention`

Holds the generation this checkout of the convention tests applies.
A generation names the set of checks the audits apply, not a count of edits.
Every audit and every settings file share one number, so a report from any of them compares across projects.
Raise `TAuditGeneration` only when a check is added, removed, or changed in what it reports.
The first line of every audit report is `<AUDIT> - AUDIT GENERATION <n>`, the form the scripts print on launch.
The generation of each generated base registry and of each hand-written setting that numbers one is compared with this one.
A mismatch means the tooling moved on while a file did not.
A registry is fixed by regenerating it, and a setting file is fixed by editing it.

## `TAuditHit`

One name a source names, with the ring of the file and the verdict a walker gave it.
The kind is the check the hit counts under and the target is the ring, namespace or row named.
The name is the type name, the namespace or the eavesdropping row the hit stands on.
The pair key is `kind:ring>target`.
An exempt row is a path, a colon and the name the hit stands on.
A path ending in `/*` covers every file under it.

## `TAuditLedger`

Reads a ledger and holds walk hits against it.
A ledger is found by name in any subfolder, so it sits beside the test that loads it.
A ledger keys a ceiling by kind and by file, so a hit is held where it lies.
A fix in one file never makes room for a regression in another.
A file the ledger does not name has a ceiling of zero, so new code starts clean.
Swapping one hit for another inside one file still passes, which is the ledger's grain.
The check fails when any file holds more hits of the kind than its ceiling, listing each such file's hits.
It returns the kind's total so the caller can print it.
When `enforced` is false the check passes and still returns the total.
The stale check fails when any ceiling sits above its count.
It also fails when the ledger names a kind the audit does not count.
It writes a lowered ledger under `temp/audit`, so it can replace the tracked one in one copy.
Each count is capped at its ceiling, so a named file over its ceiling is not admitted by the copy.
A file the ledger does not name enters the copy at its full count.

## `TAuditRatchet`

Holds every audit setting against its committed copy, so a gate can only tighten inside a working tree.
Every static or const field of every `TAudit*Setting.cs` file is held, and so is every `TAudit*Ledger.json` file.
A loosened field fails until the user commits it.
Only the user commits, so the diff that loosens a gate is always seen.
The hold runs within one generation only.
A new generation changes what the walk reports, so every setting is baselined afresh.
The committed convention file decides the generation.
The held files are found and read by `TAuditRatchetFile`.
Their fields are parsed into slots of rows by `TAuditRatchetValue`.
A field whose name ends in a ceiling suffix holds numbers that may only fall.
A slot missing from the working tree reads as zero, so dropping a slot tightens.
A slot new in the working tree is compared with zero, so it may not admit a hit.
A field whose name ends in a shrink suffix holds rows that may only be removed.
A `Floor` field may only rise.
So a `Floor` is only a minimum the code must keep.
A threshold whose rise spares more code ends in `Limit` instead, so it may only fall.
An `Enforced` flag may only switch on.
Every other field is fixed and may not change at all.
A settings file or field added, removed or unreadable at HEAD fails too.
So renaming a file or a field cannot free its ceilings.
The hold fails outright when HEAD cannot be read, rather than passing on nothing.
Every field of a settings file must read the same as its value at run time.
An entry the parser cannot read, such as an expression that is not constant, fails there.
Without this, a misread entry would let every later raise of it pass.
A fixed field compares slots as sorted lists, and a slot on one side only counts as different.

## `TAuditRatchetFile`

Finds and reads the files `TAuditRatchet` holds, in the working tree and at HEAD through Git.
The settings folder is named from the project, so this file is the same in every project.
A held file is matched by name in any subfolder, so moving it between subfolders frees no ceiling.
The HEAD listing fails when Git cannot list the files.
The committed copy is the file as `git show HEAD:` prints it, or null when it is not in HEAD.

## `TAuditRatchetValue`

Parses the held settings files and ledgers into slots of rows, keyed `Type.Field`.
The same shape comes from a run-time value, so `TAuditRatchet` compares the two directly.
The files are compiled together, so a constant naming another settings file resolves.
A ledger is keyed by its file name stem, repeated as type and field.
A field that cannot be read maps to null.
A constant is one row in the empty slot, and a list is its rows in the empty slot.
A dictionary is one slot per key, in the indexer form or the pair form.
A list with any non-constant row is unreadable.
Anything else is unreadable too.
A ledger whose JSON has another shape is unreadable.
Scalars format as invariant text, so the parsed and the run-time forms compare equal.
The runtime's own assemblies are enough to compile the settings files alone.

## `TAuditScope`

The file set one audit enumerates, built by that audit from its own settings.
Roots keep only paths under the listed folders, or every path when empty.
Include lists the `git ls-files` patterns.
Segments, Suffixes, and Prefixes drop directory names, file-name endings, and file-name starts.
Files drops exact file names, which is how an audit leaves its own tooling out.

## `TAuditSource`

Shared file enumeration for every convention test.
It carries no settings of its own, and each test hands in the `TAuditScope` its settings describe.
The repo root is the first folder above the test binary that holds `.git`.
The current version, for naming a report, is read from `version.json` at the root.
Git is asked for the tracked and untracked files matching the scope's patterns without case.
Git's output is read as UTF-8, so a path with non-ASCII letters is found, not skipped.
Ignored files never appear, and a file deleted on disk is skipped.
A path Git lists twice, as an unmerged path is, is kept once, compared without case.
The rest is filtered by the scope and sorted for a stable report.
The scope applies roots, segments, files, suffixes, and prefixes in that order.
Roots, segments, files and suffixes compare without case, as the audit scripts do.
Prefixes compare exactly.

## Stamp

Commit: `90a13c9590ba`
- `Pathwright.Tests.Convention.csproj` `3e7bd0dc428b9a8d`
- `TAuditConvention.cs` `0c3cf82c9fed7e99`
- `TAuditHit.cs` `695336cb3b584873`
- `TAuditLedger.cs` `3bf02bd2a94ba410`
- `TAuditRatchet.cs` `5c3a1d13f6f273e7`
- `TAuditRatchetFile.cs` `37e313884cb78a2c`
- `TAuditRatchetValue.cs` `ea836dbf301a0a7c`
- `TAuditScope.cs` `142bda2f4e38438a`
- `TAuditSource.cs` `bff410c4e1268601`
