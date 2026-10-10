# TAuditReference.cs
Hash: `87cf89832a7a4f05`

## `internal static class TAuditReference`

What the binder compiles against, and the generated code it adds.
Each project's output is read from the folder its own target framework names, never the newest folder.
A stale folder beside the right one can then never be read by mistake.

## `private const string TAuditReferenceSource = "src/*.csproj";`

The pattern of the project files whose outputs are read.

## `private static readonly Lazy<IReadOnlyList<MetadataReference>> TAuditReferences`

One reference set, built on first read and shared by every compilation.
Each set holds its images in native memory, which the collector frees only late.
A fresh set per assay compilation once grew the test host past ten gigabytes.
Shared references also let Roslyn reuse their symbols, so each assay binds faster.

## `public static List<string> TAuditGeneratedRead()`

The generated `.g.cs` files of every project, markup classes and global usings alike, temp projects left out.
A project with markup but no generated folder fails with the build step named.
Generated files not ending in `.g.cs`, such as assembly attributes, are left out.

## `private static IReadOnlyList<string> TAuditProjectRead()`

The tracked project files, failing when there is none.

## `private static string TAuditTargetRead(string project, string output)`

The `bin` or `obj` folder of the project's configuration and target framework.

## `public static IReadOnlyList<MetadataReference> TAuditReferenceRead()`

The shared reference set.

## `private static IReadOnlyList<MetadataReference> TAuditReferenceCreate()`

The assemblies of each framework pack and every package library in the reference root's build output.
When two packs ship an assembly of the same name, the higher version wins.
So WPF's `WindowsBase` shadows the runtime's facade and every WPF type binds.
The project's own assemblies are skipped, since their sources are in the compilation.
Missing build output fails with the build step named.

## `private static void TAuditAssemblyAdd(
        Dictionary<string, (Version TAuditVersion, string TAuditPath)> chosen, string path)`

Keeps the assembly when it is managed and newer than the one of that name held so far.
A native library is skipped, since it cannot be a reference.
