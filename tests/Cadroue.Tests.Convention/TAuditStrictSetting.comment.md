# TAuditStrictSetting.cs

## `internal static class TAuditStrictSetting`

Hand-written and tracked: the veneer-audit switch, the report path and the classifiers live here.
The per-file ceilings live in the ledger named by `TAuditLedgerFile`.
No script writes this file.

## `public const bool TAuditStrictEnforced = true;`

False makes every strict fact a warning that passes.
True fails a fact on any file whose count of a kind stands above its ledger ceiling.

## `public const string TAuditStrictReport = "temp/audit/Strict-{0}.md";`

Where the report lands, with the version in the name.
`temp` is ignored by Git, so a run never dirties the tree.

## `public const string TAuditLedgerFile = "TAuditStrictLedger";`

The ledger holding the ceiling of every kind in every veneer, deportment and host file.
The ledger stood at the generation 16 counts on the day the suite came from Llyn.

## `public static readonly string[] TAuditVeneerInclude`

The `git ls-files` patterns of the veneer sources.
A type declared under one of these is a veneer type, whatever its base.

## `public static readonly string[] TAuditReachInclude`

The `git ls-files` patterns of the veneer markup.

## `public static readonly string[] TAuditDeportmentInclude`

The `git ls-files` patterns of the deportment sources, scanned for a reach into the file system.

## `public static readonly string[] TAuditHostInclude`

The `git ls-files` patterns of the host sources, held to construction and wiring.
There is no host project, so the composition root `App.xaml.cs` stands in.

## `public const string TAuditDeportmentNamespace = "Cadroue.UIDeportment";`

The namespace whose types and members a markup binding may name, since Deportment is shell state, not logic.

## `public const string TAuditVeneerNamespace = "Cadroue.UIVeneer";`

The namespace of the veneer types.
An `x:Class` in the veneer markup naming a type outside it is a hook.

## `public const string TAuditContractType = "QContract";`

The name of the deportment's only door to the scaffold.
Every string argument of a call on it is a contract ID.
No such type exists here yet, so no call is read as a contract.

## `public static readonly string[] TAuditPackMarkers`

A deportment line holding one of these texts names the Veneer.
They match as plain text, string literals included, since a pack URI is a literal.

## `public static readonly string[] TAuditScaffoldTypes`

The WPF types the scaffold derives from.
A deportment type with one of them among its base types is scaffold.

## `public static readonly string[] TAuditContractIds`

The markup attributes that declare a contract ID, in any XML namespace.
`Name` covers `x:Name` and the framework's own `Name`.

## `public static readonly string[] TAuditQueryTypes`

The types whose extension methods are queries, each a decision the veneer may not make.

## `public static readonly string[] TAuditCatalogPatterns`

A line matching one of these does file, JSON, regex or process work, or starts a task, in the veneer.

## `public static readonly string[] TAuditCatalogExempt`

The veneer files exempt from the catalog scan by name, each a debt a plan removes.
The list rows and the group card cut a file name the deportment should hand them.
The about and workspace pages join a path the usher should build.
The icon reader picks its reader by extension.
The log window prints through `Debug` and the Flyleaf wrapper times its open with a `Stopwatch`.

## `public static readonly string[] TAuditDiskPatterns`

A deportment line matching one of these names a file system type.

## `public static readonly string[] TAuditDiskExempt`

The deportment files exempt from the disk scan by name, each a debt a plan removes.
Both cut a file name with `Path` into a trace message, work the trace writer should do below the shell.

## `public static readonly string[] TAuditReachNamespaces`

Namespaces a markup file may not map, since mapping one lets a binding reach logic.

## `public static readonly string[] TAuditTriggerElements`

The markup elements that branch on a condition.

## `public static readonly string[] TAuditTriggerSlots`

The binding slots that convert, format or fall back, each a computation in the markup.

## `public static readonly string[] TAuditHookElements`

The markup elements that hook code into the view: bindings, command and input bindings.

## `public static readonly string[] TAuditHookSlots`

The attributes that name a command or a member path, each a hook in the markup.

## `public static readonly string[] TAuditHookLiterals`

The attributes whose literal value code reads back, so a literal there is a hook.

## `public static readonly string[] TAuditHookExtensions`

The markup extensions that reach code from an attribute value.

## `public static readonly string[] TAuditHookTypes`

The types whose subclasses run code the markup selects: selectors and converters.
