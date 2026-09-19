# TAuditSemantic.cs

## `internal static class TAuditSemantic`

One compilation of every shell source, so each walker asks the compiler what a name is instead of guessing.
Logic is a symbol declared in a logic assembly, or a value of a type declared there.
A control is a type deriving from a listed framework base.
No prefix and no list of type names decides anything here.

## `private static readonly Dictionary<SyntaxNode, ISymbol?> TAuditSymbols = [];`

Every symbol lookup is cached by node, since the walkers ask for the same identifier many times.

## `public static IReadOnlyList<SyntaxNode> TAuditModelCreate(IReadOnlyList<string> sourcePaths)`

Parses the shell sources and the generated files under `obj`, then references the built logic assemblies and the shared frameworks.
Returns the walked roots.
The compilation is kept while the source list is unchanged, so the truth and strict walks share it.
Errors in the compilation do not matter, since nothing is emitted and an unresolved name is simply not logic.

## `private static List<string> TAuditGeneratedRead(string repoRoot)`

The `.g.cs` and global-using files of the newest build under each shell root, so markup fields and implicit usings resolve.

## `private static List<MetadataReference> TAuditReferenceRead(string repoRoot)`

The shared framework packs beside the test runtime, then every library in the newest build output of the reference root.
The shell assemblies themselves are skipped, since their sources are in the compilation.
A missing logic assembly fails the audit with a build hint rather than passing with nothing resolved.

## `public static IReadOnlyList<string> TAuditRootRead(IEnumerable<string> patterns)`

The folder each `git ls-files` pattern names, without its wildcard.

## `public static IReadOnlySet<string> TAuditDeportmentRead()`

Every type name and member name declared in the Deportment namespace of the compilation.
The reach walk reads it so a markup binding to Deportment state is not mistaken for a reach into logic.
Empty before the compilation exists.

## `public static bool TAuditWalkCheck(SyntaxNode root)`

True for a file under a walked root, so Deportment sources compile but are not audited for custody or strictness.

## `public static ISymbol? TAuditSymbolRead(SyntaxNode node)`

The declared or referenced symbol of a node, as its original definition, or null.

## `public static ITypeSymbol? TAuditTypeRead(SyntaxNode node)`

The type of an expression, else the type a local, parameter, field, property or method carries.

## `public static bool TAuditLogicCheck(SyntaxNode node)`

True when the node names a logic symbol or evaluates to a logic type.

## `public static bool TAuditLogicCheck(ISymbol? symbol)`

A local or parameter is logic by its type, anything else by the assembly declaring it.

## `public static bool TAuditLogicCheck(ITypeSymbol? type)`

An array by its element, a generic by any argument, a type by its assembly.

## `public static bool TAuditShellCheck(ITypeSymbol? type)`

True for a type declared in the shell compilation itself.

## `public static bool TAuditControlCheck(ITypeSymbol? type)`

True when the type or a base of it is one of the listed control bases.

## `public static bool TAuditNamedCheck(ITypeSymbol? type, IReadOnlyList<string> names)`

True when the type's simple name is in the list, for clocks and other framework types named in the settings.

## `public static string TAuditLabelRead(ISymbol symbol)`

`Type.Member`, the name a hit carries.

## `private static bool TAuditAssemblyCheck(IAssemblySymbol? assembly)`

True when the assembly is one of the listed logic assemblies.
