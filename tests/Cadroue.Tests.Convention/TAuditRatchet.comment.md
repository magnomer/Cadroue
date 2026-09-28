# TAuditRatchet.cs

## `public sealed class TAuditRatchet`

Holds every audit setting against its committed copy, so a gate can only tighten inside a working tree.
Every static field of every `TAudit*Setting.cs` file is held, and so is every `TAudit*Ledger.json` file.
A loosened field fails until the user commits it.
Only the user commits, so the diff that loosens a gate is always seen.
The hold runs within one generation only.
A new generation changes what the walk reports, so every setting is baselined afresh.

## `private const string TAuditSettingSuffix = "Setting.cs";`

The file name ending of a held settings file.

## `private const string TAuditLedgerSuffix = "Ledger.json";`

The file name ending of a held ledger.

## `private static readonly string TAuditSettingFolder`

The test project folder, named from the project so this file is the same in every project.

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

## `private static readonly CSharpParseOptions TAuditSyntaxOptions`

The parse options of the settings files, the same for the committed and the working copy.

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

## `private static IEnumerable<string> TAuditLoosenRead(`

The loosenings of one field, chosen by the direction its name ending gives.

## `private static IEnumerable<string> TAuditCeilingRead(`

Every slot that moved the loose way, where `loose` is the sign of a loosening move.
A ceiling loosens upward and a floor loosens downward.

## `private static IEnumerable<string> TAuditShrinkRead(`

Every row the working copy gained over the committed copy.

## `private static IEnumerable<string> TAuditEnforcedRead(`

A flag committed as true that is false in the working tree.

## `private static IEnumerable<string> TAuditFixedRead(`

Every slot whose rows differ, compared as sorted lists.

## `private static decimal TAuditNumberRead(List<string>? items)`

The single number a slot holds, or zero when it holds none.

## `private static string TAuditNumberFormat(Dictionary<string, List<string>> values, string slot)`

The slot's value as the report shows it, or `absent`.

## `private static Dictionary<string, Dictionary<string, List<string>>?> TAuditValueRead(`

Every static field of the given files, keyed `Type.Field`, as slots of rows.
The files are compiled together, so a constant naming another settings file resolves.
A ledger is keyed by its file name and read as one field.
A field that cannot be read maps to null.

## `private static Dictionary<string, List<string>>? TAuditExpressionRead(SemanticModel model, ExpressionSyntax value)`

One initializer as slots of rows.
A constant is one row in the empty slot, and a list is its rows in the empty slot.
A dictionary is one slot per key, in the indexer form or the pair form.
Anything else is unreadable and maps to null.

## `private static List<string>? TAuditListRead(SemanticModel model, ExpressionSyntax value)`

The constant rows of a collection expression or an array, or null when any row is not constant.

## `private static Dictionary<string, List<string>>? TAuditLedgerParse(string text)`

A ledger as one slot per kind and path, or null when the JSON does not parse.

## `private static Dictionary<string, List<string>>? TAuditRuntimeRead(object? value)`

A field's run-time value in the same slot shape the parser gives.

## `private static string TAuditScalarFormat(object? value)`

One value as invariant text, so the parsed and the run-time forms compare equal.

## `private static List<MetadataReference> TAuditReferenceRead()`

The runtime's own assemblies, enough to compile the settings files alone.

## `private static IReadOnlyList<string> TAuditTreeRead()`

The held file names in the working tree.

## `private static IReadOnlyList<string> TAuditHeadRead()`

The held file names at HEAD, failing when Git cannot list them.

## `private static bool TAuditHeldCheck(string name)`

True for a settings file or a ledger of the convention tests.

## `private static string TAuditWorkingRead(string name)`

The working copy of one held file.

## `private static string? TAuditCommittedRead(string path)`

The file as `git show HEAD:` prints it, or null when it is not in HEAD.

## `private static string? TAuditGitRead(params string[] arguments)`

The output of one Git command, or null when it fails.
