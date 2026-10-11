# TAuditNameSetting.cs
Hash: `b66a82cae921921c`

## `internal static class TAuditNameSetting`

Every naming-audit setting lives here and nowhere else, hand-written and tracked.
No script writes this file, and no registered name may appear in it.
Bases, verbs, and exemptions reach the tests only through the generated TAuditNameRegistry.cs and TAuditVerbRegistry.cs.
The tests read no script configuration, so no json or ps1 can change what they check.
They do read the untracked sources Git lists, since an unstaged file is still source.

## `public const int TAuditPrefixCeiling`

The number of types whose prefix lies outside their turf.
Most are Deportment's `L` types, since the turf map still grants that folder `Q`.
It only falls, and the script holds the same number as `naming.prefixCeiling`.

## `public static readonly string[] TAuditPrefixes`

Every prefix, longest first, so `PS` is read before `P`.
`P` marks a Veneer type, `Q` a driver, `C` Conduct, `L` logic and `T` a test.
Cadroue's Deportment mostly carries `L`, so only a few of its types match its `Q` turf.
An `S` after the first letter marks a subwindow, and a lowercase form marks a private field.

## `public static readonly Dictionary<string, string[]> TAuditPrefixTurfs`

Each project folder with the prefixes its types may carry.
Rows for folders Cadroue lacks, such as `Cadroue.UIDemeanor`, match no source.
The script holds the same map as `naming.prefixTurfs`.

## `public static readonly string[] TAuditTurfSealed`

The turfs whose out-of-turf types may not be public.
Conduct is sealed, since a public `L` type there would offer the engine to the UI.
A public type here is a hard hit kept out of the turf count, so the ceiling stays unchanged.
The script holds the same list as `naming.sealedTurfs`.
