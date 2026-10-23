# Platform

This folder holds the platform audit, which holds every project under the sources its settings name to the platform table.
It also holds the audit's analyzer check, file reader, portable scan, project reader and setting.

## `TAuditPlatform`

Holds every project of the sources its settings name to the platform table.
A layer splits into a portable half and a Windows twin, and this fact keeps that split.
The layer chain itself is the concern of the charter and border facts.
`scripts/AuditPlatform.ps1` reports the same kinds from its own configuration.
The two never read each other, yet they give the same counts, hits and wording.
Project names, frameworks, packages, property names and project-file rule codes compare without case, as MSBuild does.
The walk runs once and both facts read the same hits.
Every kind counts no more hits than its ceiling, and every file was read.
An unreadable file fails the fact even when the ceilings are not enforced.
Hits are ordered by project, path, line and text, as the script prints them.
Every ceiling equals its count, so a ceiling left above the count is stale.
Two project files that share one name stop the audit, since no table row could tell them apart.
A hit carries the project as its ring and the kind's subject as its target.
An unmapped project is reported once and is then read only for implicit usings.
The host may reference every project, so its edges are never read.
A twin may also reference the capsule project the capsule table names for it.
A property and a package are reported once each, however often the file names them.
A portable half is also read for suppressions of the platform rule.
The analyzer hit names the first failing file by path, ordered without case and then exactly.
Every project file and import is read for implicit usings, which the binder would miss.
A twin only maps a port call to a Windows API and back.
So a twin outside the shell projects that holds more holds domain logic, a `Domain` hit.
That is a twin holding a top-level type that implements nothing of its portable half.
The shell projects are left to the driver and surface audits.
A project counts as Windows when the table maps it to another project's column.
It also counts when its file targets a Windows framework.

## `TAuditPlatformAnalyzer`

Decides whether the platform rule is an error for one source file of a portable half.
It resolves analyzer configuration files and project properties the way the compiler does.
Global configurations apply first, then editor configurations from the outermost to the nearest.
An editor configuration above a `root = true` one is not read.
Only a section whose glob matches the file applies, and the last matching severity wins.
A global configuration's keys before its first section apply to every file.
A glob without a slash matches the file in any folder below.
The rule escalates when a project file lists it as an error.
It also escalates when every warning is an error and the rule is not spared.
A severity of error always holds, and a severity of warning holds only when escalated.
A lower severity never holds.
Without any severity, the rule holds only when escalated, since a warning is its default.

## `TAuditPlatformFile`

Finds and reads the files the platform audit inspects, for `TAuditPlatform` and its helpers.
A text file that cannot be read is recorded rather than thrown, so the ceiling fact can list every one.
A file read many times is still one unreadable file.
The imports are every `Directory.Build.props` and `Directory.Build.targets` file Git lists, whatever the folder.
A chain read returns the named files from the project's folder up to the repository root, nearest first.

## `TAuditPlatformPortable`

Scans one portable half for what would let Windows code into it unseen.
`TAuditPlatform` calls it for every project the table names as portable.
Every way a portable half silences the platform rule is a hit.
That covers `NoWarn` naming it, an analyzer switched off, a `#pragma` disabling it, and a `SuppressMessage` naming it.
A `#pragma` disabling every rule is a hit too, since a bare disable silences the platform rule.
A source line naming a Windows API is reported once even when several patterns match it.

## `TAuditPlatformProject`

Reads properties and items out of a loaded project file for the platform audit.
Element names and values compare without case, as MSBuild does.
Code lists split at semicolons, commas and white space as the compiler reads them.
A framework is read whether written singly or as a list.

## `TAuditPlatformSetting`

Hand-written and tracked.
It holds the platform table, the frameworks, the platform rule, the Windows markers and the ceilings.
`AuditPlatform.ps1` reads its own AuditPlatform.json and never writes this file.
The enforced switch false lets the ceiling fact pass above its ceilings.
Unreadable files and stale ceilings fail the facts either way.
A portable half targets one framework, matched exactly.
A twin targets a Windows framework, matched as a prefix so a Windows version may follow.
The platform analyzer rule must be an error in every portable half.
With it, a Windows API in a portable half fails the build.
The shell list names the projects the domain rule leaves to the driver and surface audits.
They match `domain.exempt` in scripts/AuditPlatform.json.
The silencers map project properties to the value that switches the platform analyzer off.
The column table maps each project to the portable half of its column.
A portable half is its own column, and a twin names its portable half.
A project mapped to an empty column is the host, because it names every project.
A twin may reference the one capsule project the capsule table names for it, outside its column.
The capsule keeps its own portable column, and the capsule table spares that reference a `Column` hit.
A kind without a ceiling has a ceiling of zero.
A count above fails the fact, and a ceiling above the count is stale and fails too.
Lower a ceiling when a project sheds a hit, never raise one to admit a new one.

## Stamp

Commit: `90a13c9590ba`
- `TAuditPlatform.cs` `238370aaf071caf6`
- `TAuditPlatformAnalyzer.cs` `398f4a2286265cce`
- `TAuditPlatformFile.cs` `0470f513ed276337`
- `TAuditPlatformPortable.cs` `691f001f8ce04da3`
- `TAuditPlatformProject.cs` `69626c7ae8f733ad`
- `TAuditPlatformSetting.cs` `05bdb0294562957b`
