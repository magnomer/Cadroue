# TAuditBinderSymbol.cs
Hash: `d21c6b5a14188cd4`

## `internal static class TAuditBinderSymbol`

The symbol and the type a node binds to in the compilation of `TAuditBinder`.
It also holds the plain predicates the walkers ask of a bound symbol or type.
Symbol lookups are cached by node and safe to read from many threads.

## `private static readonly Dictionary<SyntaxNode, ISymbol?> TAuditSymbols`

Every symbol lookup is cached by node, since the walkers ask for the same identifier many times.

## `public static bool TAuditHeaderCheck(SimpleNameSyntax name)`

True for a name inside a using directive or a namespace declaration, which names no type.

## `public static bool TAuditDataCheck(INamedTypeSymbol type)`

True for an enum, a struct, a record or a delegate, a type a value is the whole of.
A class or an interface is behaviour, and a static class is behaviour reached through its name.

## `public static INamedTypeSymbol? TAuditTypeRead(ISymbol symbol)`

The type a name stands for, which is the type itself or the type owning the member the name picks.
A local or a parameter is a name the file gave itself, so its uses are not counted again.

## `public static ISymbol? TAuditSymbolRead(SemanticModel model, SimpleNameSyntax name)`

The symbol a name binds to, or the first candidate when the bind is ambiguous.

## `public static ISymbol? TAuditSymbolRead(SyntaxNode node)`

The symbol a node declares or binds to, by its original definition, cached by node.
An argument is read through its expression.

## `public static ITypeSymbol? TAuditTypeRead(SyntaxNode node)`

The type of an expression, or the type behind the symbol a node names.

## `public static string? TAuditSourceRead(INamedTypeSymbol type)`

The repo-relative file declaring the type, or null for a type outside the sources.

## `public static bool TAuditControlCheck(ITypeSymbol? type)`

True when the type or any base of it is a listed control base.

## `public static bool TAuditMemberCheck(ISymbol? symbol, IReadOnlyList<string> members)`

True when the symbol's declaring type and name, joined by a dot, are in the list.

## `public static bool TAuditNamedCheck(ITypeSymbol? type, IReadOnlyList<string> names)`

True when the type's bare name is in the list.

## `public static string TAuditLabelRead(ISymbol symbol)`

The symbol's name, prefixed by its owning type's name when it has one.

## `private static ISymbol? TAuditSymbolResolve(SyntaxNode node)`

The declared symbol of a declaration node, else the bound symbol, else the first candidate.
