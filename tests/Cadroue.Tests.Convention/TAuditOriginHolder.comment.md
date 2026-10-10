# TAuditOriginHolder.cs
Hash: `6992920120af377a`

## `internal static class TAuditOriginHolder`

Reads the values a Deportment holder is written with, for `TAuditOriginWalker` to sort.
A holder's use sites come from `TAuditOriginSite`.

## `internal static List<ExpressionSyntax?>? TAuditOriginRead(ISymbol held)`

The written values of a holder, or null when the symbol is no holder.
A null entry is an unseen writer and reads plain.
A holder is a field, property, local, constructor parameter or method parameter declared in a shell source.
A method parameter is a by-value parameter of an ordinary method.
A setter's `value` stands for the property's own writes.
A non-private field or property of a type with generated markup code reads as unseen.
Its markup can bind or set it where no C# writer shows.
A dependency property field takes its metadata default and its `SetValue` calls.
A property with a getter body takes what the getter returns.
A positional record property and a constructor parameter take their `new` arguments.
A method parameter takes its arguments at every call of the method.
A local takes its initializer, its loop collection or the value its pattern tests.
Every holder except a getter body or dependency property also takes its assignments.

## `internal static List<ExpressionSyntax?>? TAuditHelperRead(IdentifierNameSyntax identifier, SyntaxNode reference)`

Null when the write is not a `ref` hand-off.
A `ref` argument into a plain helper writes the arguments the helper assigns to that parameter.
The helper's own assignment to its `ref` parameter yields nothing, since each call site already counts.
A helper that does anything else with the parameter makes the write unseen.

## `private static List<IParameterSymbol>? TAuditHelperScan(IMethodSymbol method, IParameterSymbol held)`

The parameters a helper assigns to its `ref` parameter, or null when the helper is not plain.
Plain means every write is a simple assignment from another by-value parameter of the same method.

## `private static List<ExpressionSyntax?>? TAuditGetterRead(PropertyDeclarationSyntax property)`

The returned expressions of a getter body, or null for an auto property.
Returns inside lambdas or local functions are not the getter's.

## `private static List<ExpressionSyntax?> TAuditLocalRead(SyntaxNode declaration)`

The value a local starts with, its loop collection, or the value its pattern or switch tests.
An `out` variable or a catch variable is unseen.
