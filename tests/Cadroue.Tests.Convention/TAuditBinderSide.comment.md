# TAuditBinderSide.cs
Hash: `97196759b42a52c3`

## `internal static class TAuditBinderSide`

The side of the cut a bound type is declared on, read from its source folder.
Shell is a UI or capsule root, conduct is the Conduct root, and engine is every other source.
The folders come from the settings, so nothing here names a project.
A type's side follows its type arguments and array elements too.

## `private const string TAuditShellSide = "shell";`

The side of a type declared under a UI or capsule root.

## `private const string TAuditConductSide = "conduct";`

The side of a type declared in Conduct, the gates a driver is meant to hold.

## `private const string TAuditEngineSide = "engine";`

The side of a type declared in any other source, below Conduct.

## `private static readonly string[] TAuditLogicSides`

The sides below the cut, which a request reaches.

## `public static bool TAuditLogicCheck(SyntaxNode node)`

True when the node's symbol or its type is logic.

## `public static bool TAuditLogicCheck(ISymbol? symbol)`

True for a type below the cut, a member of one, or a local or parameter of one.

## `public static bool TAuditLogicCheck(ITypeSymbol? type)`

True when the type, its element or any type argument is declared below the cut, Conduct included.

## `public static bool TAuditEngineCheck(SyntaxNode node)`

True when the node's symbol or its type is declared below Conduct.

## `public static bool TAuditEngineCheck(ISymbol? symbol)`

True for a type below Conduct, a member of one, or a local or parameter of one.

## `public static bool TAuditEngineCheck(ITypeSymbol? type)`

True when the type, its element or any type argument is declared below Conduct.

## `public static bool TAuditConductCheck(ITypeSymbol? type)`

True for a Conduct type whose type arguments are Conduct types too, which a driver may hold.

## `public static bool TAuditShellCheck(ITypeSymbol? type)`

True when the type, its element or any type argument is declared in a UI or capsule source.
Generated sources are included.

## `public static bool TAuditSurfaceCheck(ITypeSymbol? type)`

True when the type is declared under a veneer root.

## `private static bool TAuditDepthCheck(ISymbol? symbol, string[] sides)`

True when the symbol is a type on a given side, a member of one, or a local of one.
A parameter counts as a local here.

## `private static bool TAuditDepthCheck(ITypeSymbol? type, string[] sides)`

True when the type, its element or any type argument is declared on one of the sides.

## `public static IReadOnlySet<string> TAuditDeportmentRead()`

Every type name and member name the deportment namespace declares, so a markup binding to one is not a reach.
A name that a type below the cut also declares is removed, so a deeper binding cannot pass by coincidence.

## `private static string? TAuditSideRead(INamedTypeSymbol? type)`

The side a type is declared on, which is shell, conduct or engine.
A type from metadata sits on no side.
