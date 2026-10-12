# TAuditTruthSetting.cs
Hash: `ea9658301511f227`

## `internal static class TAuditTruthSetting`

Hand-written and tracked.
The driver-audit scope, the binder inputs and the handle list live here.
The per-file ceilings live in the ledger named by `TAuditLedgerFile`.
No script writes this file.

## `public const int TAuditGeneration = 23;`

Numbers the revision of the driver-audit settings this file holds.

## `public const bool TAuditTruthEnforced = true;`

False makes every driver fact a warning that passes.
True fails a fact on any file whose count of a kind stands above its ledger ceiling.

## `public const string TAuditTruthReport = "temp/audit/Custody-{0}.md";`

Where the report lands, with the version in the name.
`temp` is ignored by Git, so a run never dirties the tree.

## `public const string TAuditStateSuffix = "State";`

A field or property whose name ends in this is a state the engine should own.

## `public const string TAuditBulletinType = "LBulletin";`

The engine notice type a handler must read.
A handler or lambda taking one and never reading it is a deaf observer.
No Cadroue type bears this name, so the check finds nothing yet.

## `public const string TAuditConfiguration = "Debug";`

The build configuration whose output and generated files the binder reads.

## `public const string TAuditReferenceRoot = "src/Cadroue.Host";`

The project whose build output carries every package any source references.
Host names every project, so its output holds the packages of the UI, Media and Infrastructure.

## `public const string TAuditConductRoot = "src/Cadroue.Conduct";`

The folder whose types are Conduct's.
A driver may hold, implement and reshape them, since Conduct's types stop at the driver.
Cadroue's Conduct holds no source, so no type is Conduct's yet.

## `public const string TAuditLedgerFile = "TAuditTruthLedger";`

The ledger holding the ceiling of every kind in every driver file.

## `public static readonly string[] TAuditCapsuleInclude`

The `git ls-files` patterns of a capsule project storing Deportment's own state.
A call into it answers a shell value, so Contesting does not search its arguments.
Every reader of `TAuditShellInclude` joins this list to it, so each path is written once.
Cadroue has no capsule project, so the pattern matches nothing.

## `public static readonly string[] TAuditShellInclude`

The `git ls-files` patterns of every UI source, surfaces and drivers alike.
Only the Veneer and Deportment exist, so the UITerminal and UIDemeanor patterns match nothing.
A type declared under one of these, or under `TAuditCapsuleInclude`, is UI.
A type declared in Conduct is a gate type, and a type declared under any other source is engine.

## `public static readonly string[] TAuditTruthInclude`

The patterns of the driver sources the driver walks audit.
Deportment matches, while the UIDemeanor pattern names a folder Cadroue lacks.
The surfaces are left to the surface audit, where holding anything is already a hit.

## `public static readonly Dictionary<string, string[]> TAuditTruthInformative`

The kinds that only inform, each with the driver folders it waits on.
Such a kind gates nothing while a listed folder holds no tracked source.
Once every listed folder holds one, the kind fails like any other.
Mismatching waits on `src/Cadroue.UIDemeanor`, which Cadroue lacks, so it only informs.

## `public static readonly Dictionary<string, string> TAuditControlPrefix`

The control prefix of each driver ring, keyed by the ring's folder.
A driver type whose name starts with its ring's prefix is a control type.
A control shows what it is handed and may not request logic, so puppeteering names any request inside one.
Cadroue's Deportment types carry `L`, so none is a control type by this prefix.
`AuditUI.json` holds the same rows as `truth.controlPrefixes`, copied by hand.

## `public static readonly string[] TAuditFrameworkPacks`

The shared frameworks referenced beside the test runtime, so framework types resolve.

## `public static readonly string[] TAuditControlBases`

A type deriving from one of these is a GUI control, whose state may not decide a request.

## `public static readonly string[] TAuditOrderVerbs`

The collection calls that change an order, which only the engine may decide.

## `public static readonly string[] TAuditFillVerbs`

The calls that write into a collection in place.
A `readonly` field filled through one is not a fixture.

## `public const string TAuditRequestPrefix = "LRequest";`

The prefix every request record carries.
No Cadroue type carries it, so no record counts as a request by name.

## `public static readonly string[] TAuditSendRoots`

The calls a request finally goes through.
A member that reaches one, calls a Conduct method or builds a request is a sender.
No Cadroue member bears these names, so only the other two routes find a sender.

## `public static readonly string[] TAuditClockTypes`

The types that fire on time rather than on a user act.
A clock drives a request through an event, a callback it is built with or a loop waiting on it.

## `public static readonly string[] TAuditConsoleInput`

The console calls that carry what the user typed, the control input of a console driver.
Each is written as the full name of its declaring type and member.

## `public static readonly string[] TAuditDialogTypes`

The types whose answer confirms a request.
A confirm belongs in the gate behind a port, so a dialog answer deciding a request is gatekeeping.

## `public static readonly string[] TAuditDelayMembers`

The waits that turn a loop into a clock.

## `public const string TAuditInputBase = "System.Windows.Controls.Control";`

The base type of every control that takes user input.
The laundering walk colours an input member only on a receiver deriving from it.
`TextBox`, `ComboBox` and `ToggleButton` derive from it, while `TextBlock` and `Run` do not.
`AuditUI.json` holds the same name as `truth.inputBase`, copied by hand.

## `public static readonly string[] TAuditInputMembers`

The members of a GUI control that carry what the user typed or chose.
Caret and selection positions stay off the list, since a driver keeps caret, focus and placement work.
`AuditUI.json` holds the same list as `truth.inputMembers`, copied by hand.

## `public static readonly string[] TAuditFocusMembers`

The members of a GUI control that tell whether the user is at it.
A guard on one of them only asks whose event it is, so the misfiring scan exempts it.

## `public static readonly string[] TAuditTruthHandles`

Driver types a driver field may hold as a handle rather than as a value.
A handle is called on and passed on, so the field is skipped before any rule reads it.
A Conduct type is a handle by rule and is never listed.
A type from below Conduct is never a handle, so holding one is a duplicating hit counted in the ledger.
The list is empty, so no driver type is held as a handle.
A type is matched as the binder shows it, with any nullable mark dropped.

## `public static readonly string[] TAuditMoonlightingVerbs`

Query methods that, applied to a value from below Conduct, are moonlighting.
