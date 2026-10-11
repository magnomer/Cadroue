# TAuditStrictSetting.cs
Hash: `677d36beb396f4b4`

## `internal static class TAuditStrictSetting`

Hand-written and tracked.
The surface-audit switch, the report path and the classifiers live here.
The per-file ceilings live in the ledger named by `TAuditLedgerFile`.
No script writes this file.

## `public const bool TAuditStrictEnforced = true;`

False makes every strict fact a warning that passes.
True fails a fact on any file whose count of a kind stands above its ledger ceiling.

## `public const string TAuditStrictReport = "temp/audit/Strict-{0}.md";`

Where the report lands, with the version in the name.
`temp` is ignored by Git, so a run never dirties the tree.

## `public const string TAuditLedgerFile = "TAuditStrictLedger";`

The ledger holding the ceiling of every kind in every surface, driver and host file.

## `public static readonly string[] TAuditReachInclude`

The `git ls-files` patterns of the surface markup.

## `public static readonly string[] TAuditVeneerInclude`

The `git ls-files` patterns of the surface sources.
The Veneer matches, while the `Cadroue.UITerminal` pattern names a folder Cadroue lacks.
A type declared under one of these is a surface type, whatever its base.

## `public static readonly string[] TAuditDeportmentInclude`

The `git ls-files` patterns of the driver sources, scanned for a reach into the file system.
Deportment matches, while the `Cadroue.UIDemeanor` pattern names a folder Cadroue lacks.

## `public static readonly string[] TAuditHostInclude`

The `git ls-files` patterns of the `Cadroue.Host` sources, held to construction and wiring.

## `public const string TAuditDeportmentNamespace = "Cadroue.UIDeportment";`

The namespace whose types and members a markup binding may name, since the driver is the surface's path.
A name a type below the cut also declares is left out.
So a deeper binding cannot pass by coincidence.

## `public const string TAuditVeneerNamespace = "Cadroue.UIVeneer";`

The namespace of the surface types.
An `x:Class` in the surface markup naming a type outside it is tethering.

## `public const string TAuditContractType = "QContract";`

The name of a contract type whose string arguments are contract IDs.
No Cadroue type bears this name, so no call counts as a contract call.

## `public static readonly string[] TAuditHardwiringMarkers`

A driver line holding one of these texts names the Veneer.
They match as plain text, string literals included, since a pack URI is a literal.

## `public static readonly string[] TAuditMasqueradingTypes`

The WPF types the Veneer derives from.
A driver type with one of them among its base types is masquerading.

## `public static readonly string[] TAuditContractIds`

The markup attributes that declare a contract ID, in any XML namespace.
`Name` covers `x:Name` and the framework's own `Name`.

## `public static readonly string[] TAuditQueryTypes`

The types whose extension methods are queries, each a decision a surface may not make.

## `public static readonly string[] TAuditCatalogPatterns`

A line matching one of these does file, JSON, regex or process work, or starts a task, in the surface.
That work belongs below the UI, in the engine or in an Infrastructure owner such as `LUsher`.

## `public static readonly string[] TAuditCatalogExempt`

The surface files whose stream use is the framework's own, such as a resource stream or a bitmap decode.
It is empty, so every catalog line in the Veneer counts.

## `public static readonly string[] TAuditDiskPatterns`

A driver line matching one of these names a file system type.
The patterns name the types, so a plain `TextReader` or `TextWriter` passes.

## `public static readonly string[] TAuditDiskExempt`

The driver files exempt from the disk scan by name.
It is empty, so every disk reach in a driver counts.

## `public static readonly string[] TAuditOverreachingNamespaces`

The namespaces below the driver that a markup file may not map, since a mapping lets a binding reach them.

## `public static readonly string[] TAuditBranchingElements`

The markup elements that branch on a condition or switch between visual states.

## `public static readonly string[] TAuditBranchingSlots`

The binding slots that convert, format, fall back, select a template or validate.
Each is a computation in the markup, whether set as an attribute, inside an extension or as a property element.

## `public static readonly string[] TAuditTetheringElements`

The markup elements that bind a value, a command or an input to logic.

## `public static readonly string[] TAuditTetheringSlots`

The attributes that tether logic, whether set as an attribute, as a property element or through a `Setter`.
The command slots, `DisplayMemberPath` and `SelectedValuePath` all name what code reads.

## `public static readonly string[] TAuditTetheringLiterals`

The attributes that tether only when set to a plain literal, directly or through a `Setter`.
`Tag` is one, since code branches on a literal tag.
A value opening a markup extension is left to the extension rules.
The script reads the same list from `tetheringLiterals` in `AuditUI.json`.

## `public static readonly string[] TAuditTetheringExtensions`

The markup extensions that bind to logic or read a code member.
A markup extension whose prefix maps a code namespace is tethering without being listed.

## `public static readonly string[] TAuditTetheringTypes`

The converter and selector types a markup element may not be.
An element is one when its type or a base type is listed or implements a listed interface.
