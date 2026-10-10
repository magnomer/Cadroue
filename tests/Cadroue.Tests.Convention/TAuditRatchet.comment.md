# TAuditRatchet.cs
Hash: `d90e60acbc0dc352`

## `public sealed class TAuditRatchet`

Holds every audit setting against its committed copy, so a gate can only tighten inside a working tree.
Every static or const field of every `TAudit*Setting.cs` file is held, and so is every `TAudit*Ledger.json` file.
A loosened field fails until the user commits it.
Only the user commits, so the diff that loosens a gate is always seen.
The hold runs within one generation only.
A new generation changes what the walk reports, so every setting is baselined afresh.
The held files are found and read by `TAuditRatchetFile`.
Their fields are parsed into slots of rows by `TAuditRatchetValue`.

## `internal const string TAuditRatchetAudit`

The audit name every ratchet failure carries, shared with `TAuditRatchetFile`.

## `private static readonly string TAuditConventionPath`

The file whose committed generation decides whether the hold runs.

## `private static readonly Regex TAuditGenerationPattern`

The generation constant as it stands in the committed convention file.

## `private static readonly string[] TAuditCeilingSuffixes`

A field whose name ends in one of these holds numbers that may only fall.
A slot missing from the working tree reads as zero, so dropping a slot tightens.
A slot new in the working tree is compared with zero, so it may not admit a hit.

## `private static readonly string[] TAuditShrinkSuffixes`

A field whose name ends in one of these holds rows that may only be removed.

## `public void AuditRatchet_Settings_NeverLoosen()`

Every held field keeps its direction against HEAD.
A `Floor` field may only rise.
So a `Floor` is only a minimum the code must keep, such as a ring's file count.
A threshold whose rise spares more code ends in `Limit` instead, so it may only fall.
An `Enforced` flag may only switch on.
Every other field is fixed and may not change at all.
A settings file or field added, removed or unreadable at HEAD fails too.
So renaming a file or a field cannot free its ceilings.
The hold fails outright when HEAD cannot be read, rather than passing on nothing.

## `public void AuditRatchet_SettingParse_MatchesRuntime()`

Every field the ratchet parses reads the same as its value at run time.
An entry the parser cannot read, such as an expression that is not constant, fails here.
Without this, a misread entry would let every later raise of it pass.

## `private static IEnumerable<string> TAuditLoosenRead(string key, Dictionary<string, List<string>>? before, Dictionary<string, List<string>>? after, bool held)`

The loosenings of one field, chosen by the direction its name ending gives.

## `private static IEnumerable<string> TAuditCeilingRead(Dictionary<string, List<string>> before, Dictionary<string, List<string>> after, int loose)`

Every slot that moved the loose way, where `loose` is the sign of a loosening move.
A ceiling loosens upward and a floor loosens downward.

## `private static IEnumerable<string> TAuditShrinkRead(Dictionary<string, List<string>> before, Dictionary<string, List<string>> after)`

Every row the working copy gained over the committed copy.

## `private static IEnumerable<string> TAuditEnforcedRead(Dictionary<string, List<string>> before, Dictionary<string, List<string>> after)`

A flag committed as true that is false in the working tree.

## `private static IEnumerable<string> TAuditFixedRead(Dictionary<string, List<string>> before, Dictionary<string, List<string>> after)`

Every slot whose rows differ, compared as sorted lists, or that exists on one side only.

## `private static decimal TAuditNumberRead(List<string>? items)`

The single number a slot holds, or zero when it holds none.

## `private static string TAuditNumberFormat(Dictionary<string, List<string>> values, string slot)`

The slot's value as the report shows it, or `absent`.
