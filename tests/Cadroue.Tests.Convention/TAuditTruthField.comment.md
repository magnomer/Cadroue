# TAuditTruthField.cs
Hash: `0d1c890b1259ddb3`

## `internal sealed record TAuditTruthField(HashSet<ISymbol> TFieldSymbols, string TFieldName, ITypeSymbol TFieldType, string TFieldPath, int TFieldLine, bool TFieldShared)`

One audited field, with the symbols that mean it, its label, its type and where it is declared.
A positional parameter carries both its parameter symbol and the property it generates.
The record also lists which members of a class are audited fields.
`TAuditTruthWalker` judges each one it yields.

## `internal static IEnumerable<TAuditTruthField> TAuditFieldRead(IReadOnlyList<TypeDeclarationSyntax> type)`

The mutable fields, the settable properties and the positional record parameters of a class.
A `readonly` or `const` field is a fixture only while nothing writes into it and it holds no engine type.
A `static` field is shared and its writes are read in every file.
A settable property and a positional parameter are always shared.
A field initialised to `null!` is audited like any other, since its type says what it holds.
A field typed as a handle grips a gate rather than holding a value, and is skipped.
A getter property that reads the field is followed as the field, so gatekeeping cannot hide behind it.

## `private static bool TAuditFillCheck(HashSet<ISymbol> symbols, IReadOnlyList<TypeDeclarationSyntax> type)`

True when any part of the class writes into the field, by assignment, slot or fill verb.
