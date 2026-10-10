# TAuditNameSetting.cs
Hash: `62640ae9d0b08cf6`

## `internal static class TAuditNameSetting`

Every naming-audit setting lives here and nowhere else, hand-written and tracked.
No script writes this file, and no registered name may appear in it.
Bases, verbs, and exemptions reach the tests only through the generated TAuditNameRegistry.cs and TAuditVerbRegistry.cs.
The tests read no script configuration, so no json or ps1 can change what they check.
They do read the untracked sources Git lists, since an unstaged file is still source.

## `public const int TAuditPrefixCeiling`

The number of types whose prefix lies outside their turf.
It only falls, and the script holds the same number as `naming.prefixCeiling`.

## `public static readonly string[] TAuditPrefixes`

Every prefix, longest first, so `PS` is read before `P`.
`P` marks a surface, `Q` a driver, `C` Conduct, `L` every other project and `T` a test.
An `S` after the first letter marks a subwindow, and a lowercase form marks a private field.

## `public static readonly Dictionary<string, string[]> TAuditPrefixTurfs`

Each project folder with the prefixes its types may carry.
The script holds the same map as `naming.prefixTurfs`.

## `public static readonly string[] TAuditTurfSealed`

The turfs whose out-of-turf types may not be public.
Conduct is sealed, since a public `L` type there would offer the engine to the UI rings.
A public type here is a hard hit kept out of the turf count, so the ceiling stays unchanged.
The script holds the same list as `naming.sealedTurfs`.
