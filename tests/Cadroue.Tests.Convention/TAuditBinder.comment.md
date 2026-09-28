# TAuditBinder.cs

## `internal static class TAuditBinder`

One Roslyn compilation of every tracked source under `src`, shared by every audit that binds.
The chain, the frame and the shell walks read the same trees, so the bind is paid once.
The shell's generated markup classes join the compilation, so an `x:Name` field binds like any other.
`TAuditReference` supplies the references and the generated code.
The compilation must hold no error, so an unbound name can never drop out of a walk unseen.
Nothing here names a project: the folders, the exclusions and the project name come from the settings.

## `private const string TAuditBinderSource = "src/";`

The folder every ring lives under.

## `private const string TAuditShellSide = "shell";`

The side of a type declared under a UI root.

## `private const string TAuditConductSide = "conduct";`

The side of a type declared in Conduct, the gates a driver is meant to hold.

## `private const string TAuditEngineSide = "engine";`

The side of a type declared in any other source, below Conduct.

## `private static readonly string[] TAuditLogicSides`

The sides below the cut, which a request reaches.

## `private static readonly CSharpParseOptions TAuditSyntaxOptions`

The parse options every source is read with.

## `private static readonly Lazy<CSharpCompilation> TAuditBinderCompilation`

The compilation, built on first use and kept for the run.

## `private static readonly Lazy<IReadOnlyList<SyntaxTree>> TAuditTracked`

The trees of the tracked sources alone, without the generated files, in path order.

## `private static readonly Dictionary<SyntaxTree, SemanticModel> TAuditModels`

One semantic model per tree, built on first read and kept, since every walker reads it.

## `private static readonly Dictionary<SyntaxNode, ISymbol?> TAuditSymbols`

Every symbol lookup is cached by node, since the walkers ask for the same identifier many times.

## `public static string TAuditRoot { get; }`

The Git working tree the sources are read from.

## `public static IReadOnlyList<SyntaxTree> TAuditTrees`

Every parsed tracked source, in path order.

## `public static CSharpCompilation TAuditCompilation`

The whole compilation, generated markup classes included, for a walk that must see every reader.

## `public static SemanticModel TAuditModelRead(SyntaxTree tree)`

The semantic model of one tree, safe to read from many threads.

## `public static SemanticModel TAuditModelRead(SyntaxNode node)`

The semantic model of the tree a node belongs to.

## `public static IReadOnlyList<SyntaxNode> TAuditWalkRead(IReadOnlyList<string> sourcePaths)`

The roots of the tracked trees the given paths name, so a walker scans its own scope.

## `public static void TAuditCoverCheck(string audit, IReadOnlyList<string> sourcePaths)`

Fails when a tracked source is missing from the compiled trees, so a dropped file cannot pass unseen.

## `public static bool TAuditWalkCheck(SyntaxNode root)`

True when the root's file sits under a folder the driver walk audits.

## `public static IReadOnlyList<string> TAuditRootRead(IEnumerable<string> patterns)`

The folders the `git ls-files` patterns start with, each once.

## `public static string TAuditRelativeRead(string path)`

The repo-relative path with forward slashes.

## `public static string? TAuditRingRead(string relative, IEnumerable<string> rings)`

The ring whose folder holds the file, or null outside every ring given.
A ring is its project folder under `src`, matched whole, so `Cadroue.UI` never claims `Cadroue.UIVeneer`.

## `public static bool TAuditHeaderCheck(SimpleNameSyntax name)`

True for a name inside a using directive or a namespace declaration, which names no type.

## `public static bool TAuditDataCheck(INamedTypeSymbol type)`

True for an enum, a struct, a record or a delegate: a type a value is the whole of.
A class or an interface is behaviour, and a static class is behaviour reached through its name.

## `public static INamedTypeSymbol? TAuditTypeRead(ISymbol symbol)`

The type a name stands for: the type itself, or the type owning the member the name picks.
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

True when the type, its element or any type argument is declared in a UI source, generated ones included.

## `public static bool TAuditSurfaceCheck(ITypeSymbol? type)`

True when the type is declared under a surface root.

## `private static bool TAuditDepthCheck(ISymbol? symbol, string[] sides)`

True when the symbol is a type on a given side, a member of one, or a local of one.
A parameter counts as a local here.

## `private static bool TAuditDepthCheck(ITypeSymbol? type, string[] sides)`

True when the type, its element or any type argument is declared on one of the sides.

## `public static bool TAuditControlCheck(ITypeSymbol? type)`

True when the type or any base of it is a listed control base.

## `public static bool TAuditMemberCheck(ISymbol? symbol, IReadOnlyList<string> members)`

True when the symbol's declaring type and name, joined by a dot, are in the list.

## `public static bool TAuditNamedCheck(ITypeSymbol? type, IReadOnlyList<string> names)`

True when the type's bare name is in the list.

## `public static string TAuditLabelRead(ISymbol symbol)`

The symbol's name, prefixed by its owning type's name when it has one.

## `public static IReadOnlySet<string> TAuditDeportmentRead()`

Every type name and member name the deportment namespace declares, so a markup binding to one is not a reach.
A name that a type below the cut also declares is removed, so a deeper binding cannot pass by coincidence.

## `private static string? TAuditSideRead(INamedTypeSymbol? type)`

The side a type is declared on: shell, conduct or engine.
A type from metadata sits on no side.

## `private static ISymbol? TAuditSymbolResolve(SyntaxNode node)`

The declared symbol of a declaration node, else the bound symbol, else the first candidate.

## `private static IReadOnlyList<SyntaxTree> TAuditTrackedRead()`

The compilation's trees whose file sits under `src` and not under an `obj` folder.

## `private static CSharpCompilation TAuditCompilationRead()`

Enumerates the sources with Git, adds the generated markup classes, parses and binds them.
An empty enumeration fails rather than passing vacuously.
The sources compile as one program, since Host carries the entry point.
Any compile error fails, since a stale or missing build would otherwise weaken every walk silently.
