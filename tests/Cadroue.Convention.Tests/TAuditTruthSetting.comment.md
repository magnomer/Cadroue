# TAuditTruthSetting.cs

## `internal static partial class TAuditTruthSetting`

Hand-written and tracked: the truth-audit scope, the compilation inputs and the ceilings live here.
There is no waiver and no handle list, since the compiler says what is logic.
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

## `public const string TAuditObserverType = "PObserver";`

The shell's subscription type, so a lambda handed to one that ignores its bulletin is deaf too.

## `public const string TAuditShellAssembly = "Cadroue.Shell";`

The name of the one compilation the shell sources are joined into.

## `public const string TAuditConfiguration = "Debug";`

The build configuration whose output and generated files the compilation reads.

## `public const string TAuditReferenceRoot = "src/Cadroue.UIVeneer";`

The project whose build output carries every library the shell references.

## `public static readonly string[] TAuditShellInclude`

The `git ls-files` patterns of every shell source, all of which enter the compilation.

## `public static readonly string[] TAuditTruthInclude`

The patterns of the sources the custody walk audits, the Veneer alone.
The Deportment owns UI state by design, so holding or passing a logic value there is not a custody hit.

## `public static readonly string[] TAuditLogicAssemblies`

The assemblies whose symbols are logic.
A value is logic when its type or its declaring member comes from one of these.

## `public static readonly string[] TAuditFrameworkPacks`

The shared frameworks referenced beside the test runtime, so framework types resolve.

## `public static readonly string[] TAuditReferenceSkip`

Libraries in the build output that are not referenced, since their sources are in the compilation.

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

## `public static readonly string[] TAuditInputMembers`

The members of a control that carry what the user typed or chose.

## `public static readonly IReadOnlyDictionary<string, int> TAuditTruthCeiling`

The hit count each kind may reach.
A count above fails the fact, a ceiling above the count is stale and fails too.
Lower a ceiling when the shell sheds a hit, never raise one to admit a new one.


