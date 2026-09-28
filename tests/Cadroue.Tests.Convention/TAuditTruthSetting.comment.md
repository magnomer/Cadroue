# TAuditTruthSetting.cs

## `internal static class TAuditTruthSetting`

Hand-written and tracked: the deportment-audit scope, the binder inputs and the ceilings live here.
There is no waiver, since the binder says what is logic.
A ceiling is the only tolerance.
No script writes this file.

## `public const bool TAuditTruthEnforced = true;`

False makes every custody fact a warning that passes.
True fails a fact on any hit that is not waived and on any kind above its ceiling.

## `public const string TAuditTruthReport = "temp/audit/Custody-{0}.md";`

Where the report lands, with the version in the name.
`temp` is ignored by Git, so a run never dirties the tree.

## `public const string TAuditStateSuffix = "State";`

A field or property whose name ends in this is a state the engine should own.

## `public const string TAuditBulletinType = "LNotice";`

The engine's notice type, so a handler taking one and never reading it is a deaf handler.
No such type exists here yet, so the deaf-handler shape reports nothing until one does.

## `public const string TAuditConfiguration = "Debug";`

The build configuration whose output and generated files the binder reads.

## `public const string TAuditReferenceRoot = "src/Cadroue.UIVeneer";`

The project whose build output carries every package the source references.

## `public const string TAuditConductRoot = "src/Cadroue.Conduct";`

The folder whose types are Conduct's, which a deportment may hold and reshape.
No Conduct project exists here, so every type below the UI counts as the engine's.

## `public const string TAuditLedgerFile = "TAuditTruthLedger";`

The ledger holding the ceiling of every kind in every deportment file.
The ledger stood at the generation 16 counts on the day the suite came from Llyn.
## `public static readonly string[] TAuditShellInclude`

The `git ls-files` patterns of every UI source, the veneer and the deportment both.
A type declared under one of these is UI, and a type declared under any other source is logic.

## `public static readonly string[] TAuditTruthInclude`

The patterns of the sources the deportment walks audit.
The veneer is left to the veneer audit, where holding anything is already a hit.

## `public static readonly string[] TAuditFrameworkPacks`

The shared frameworks referenced beside the test runtime, so framework types resolve.

## `public static readonly string[] TAuditControlBases`

A type deriving from one of these is a control, whose state may not decide a request.

## `public static readonly string[] TAuditOrderVerbs`

The collection calls that change an order, which only the engine may decide.

## `public static readonly string[] TAuditFillVerbs`

The calls that write into a collection in place.
A `readonly` field filled through one is not a fixture.

## `public const string TAuditRequestPrefix`

The prefix every request record carries.

## `public static readonly string[] TAuditSendRoots`

The messenger calls a request finally goes through.
A member that reaches one, or builds a request, is a sender.

## `public static readonly string[] TAuditClockTypes`

The types whose events fire on time rather than on a user act.

## `public static readonly string[] TAuditConsoleInput`

The console calls that carry what the user typed.
Each is written as the full name of its declaring type and member.

## `public static readonly string[] TAuditDialogTypes`

The types whose answer confirms a request.
A dialog answer deciding a request is a guard.

## `public static readonly string[] TAuditDelayMembers`

The waits that turn a loop into a clock.
## `public static readonly string[] TAuditInputMembers`

The members of a control that carry what the user typed or chose.

## `public static readonly string[] TAuditTruthHandles`

Types a deportment field may hold as a handle to the engine rather than as a value.
A handle is called on and passed back, so the field is skipped before any rule reads it.
Each is a live logic owner a tab hands its deportment: docket, selections, schedule, station, segment.
`LMpv` is the embedded player library and `LRelay` the cross-instance channel.
A type is matched as the binder shows it, with any nullable mark dropped.

## `public static readonly string[] TAuditTreatVerbs`

Query methods that, applied to a logic value, are data treatment.


