# TAuditBinder.cs
Hash: `2ab060ef20485ddb`

## `internal static class TAuditBinder`

One Roslyn compilation of every tracked source under `src`, shared by every audit that binds.
The border, the purity and the shell walks read the same trees, so the bind is paid once.
The shell's generated markup classes join the compilation, so an `x:Name` field binds like any other.
`TAuditReference` supplies the references and the generated code.
The compilation must hold no error, so an unbound name can never drop out of a walk unseen.
Nothing here names a project.
The folders and the exclusions come from the settings.
`TAuditBinderSymbol` reads symbols and types from these trees.
`TAuditBinderSide` tells which side of the cut a type is declared on.

## `private const string TAuditBinderSource = "src/";`

The folder every ring lives under.

## `private static readonly CSharpParseOptions TAuditSyntaxOptions`

The parse options every source is read with.

## `private static readonly Lazy<CSharpCompilation> TAuditBinderCompilation`

The compilation, built on first use and kept for the run.

## `private static readonly Lazy<IReadOnlyList<SyntaxTree>> TAuditTracked`

The trees of the tracked sources alone, without the generated files, in path order.

## `private static readonly AsyncLocal<CSharpCompilation?> TAuditAssayCompilation`

The handed source set an assay binds in place of the tracked tree.
It is local to the assay's own flow, so a tracked walk running beside it never sees it.
Outside an assay it is null and the tracked compilation stands.

## `private static readonly AsyncLocal<IReadOnlyDictionary<string, string>?> TAuditAssayMarkup`

The handed markup texts an assay hands, keyed by full path.
It is local to the assay's own flow, like `TAuditAssayCompilation`.
Outside an assay it is null and markup is read from disk.

## `private static readonly Dictionary<SyntaxTree, SemanticModel> TAuditModels`

One semantic model per tree, built on first read and kept, since every walker reads it.

## `public static string TAuditRoot { get; }`

The Git working tree the sources are read from.

## `public static IReadOnlyList<SyntaxTree> TAuditTrees`

Every parsed tracked source, in path order.
Inside an assay, the handed sources instead.

## `public static CSharpCompilation TAuditCompilation`

The whole compilation, generated markup classes included, for a walk that must see every reader.
Inside an assay, the handed compilation instead.

## `public static TAuditAssayResult TAuditAssayRun<TAuditAssayResult>(IReadOnlyDictionary<string, string> sources, Func<TAuditAssayResult> walk)`

Runs a walk over a handed source set instead of the tracked tree.
Each key is a repo-relative virtual path.
So the side of a type follows its folder, as for a real file.
The walker runs unchanged, and only the trees and the compilation it binds against change.
A key ending in `.xaml` is markup, kept as text for `TAuditMarkupRead`, and never parsed as C#.
The handed compilation and markup are cleared when the walk returns, even on a failure.

## `public static string TAuditMarkupRead(string path)`

The text of a markup file, handed by the running assay or read from disk otherwise.
During an assay only handed markup is read, so a missing key fails instead of reaching the disk.

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

## `private static IReadOnlyList<SyntaxTree> TAuditTrackedRead(CSharpCompilation compilation)`

The given compilation's trees whose file sits under `src` and not under an `obj` folder.

## `private static CSharpCompilation TAuditCompilationRead()`

Enumerates the sources with Git, adds the generated markup classes and parses them.
An empty enumeration fails rather than passing vacuously.
The sources compile as one program, since Host carries the entry point.

## `private static CSharpCompilation TAuditCompilationCreate(IReadOnlyList<SyntaxTree> trees, OutputKind kind)`

Binds a source set against the shared references, for the tracked tree and an assay alike.
Any compile error fails, since a stale or missing build would otherwise weaken every walk silently.
An assay compiles as a library, since its sources carry no entry point.
