# TAuditRatchetValue.cs
Hash: `ea836dbf301a0a7c`

## `internal static class TAuditRatchetValue`

Parses the held settings files and ledgers into slots of rows, keyed `Type.Field`.
The same shape comes from a run-time value, so `TAuditRatchet` compares the two directly.

## `private static readonly CSharpParseOptions TAuditSyntaxOptions`

The parse options of the settings files, the same for the committed and the working copy.

## `public static Dictionary<string, Dictionary<string, List<string>>?> TAuditValueRead(IReadOnlyDictionary<string, string?> texts)`

Every static or const field of the given files, keyed `Type.Field`, as slots of rows.
The files are compiled together, so a constant naming another settings file resolves.
A ledger is keyed by its file name stem, repeated as type and field.
A field that cannot be read maps to null.

## `private static Dictionary<string, List<string>>? TAuditExpressionRead(SemanticModel model, ExpressionSyntax value)`

One initializer as slots of rows.
A constant is one row in the empty slot, and a list is its rows in the empty slot.
A dictionary is one slot per key, in the indexer form or the pair form.
Anything else is unreadable and maps to null.

## `private static List<string>? TAuditListRead(SemanticModel model, ExpressionSyntax value)`

The constant rows of a collection expression or an array, or null when any row is not constant.

## `private static Dictionary<string, List<string>>? TAuditLedgerParse(string text)`

A ledger as one slot per kind and path, or null when the JSON has another shape.

## `public static Dictionary<string, List<string>>? TAuditRuntimeRead(object? value)`

A field's run-time value in the same slot shape the parser gives.

## `private static string TAuditScalarFormat(object? value)`

One value as invariant text, so the parsed and the run-time forms compare equal.

## `private static List<MetadataReference> TAuditReferenceRead()`

The runtime's own assemblies, enough to compile the settings files alone.
