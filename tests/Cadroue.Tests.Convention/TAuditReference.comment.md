# TAuditReference.cs

## `internal static class TAuditReference`

What the binder compiles against, and the generated code it adds.
Each project's output is read from the folder its own target framework names, never the newest folder.
A stale folder beside the right one can then never be read by mistake.

## `private const string TAuditReferenceSource = "src/*.csproj";`

The pattern of the project files whose outputs are read.

## `public static List<string> TAuditGeneratedRead()`

The generated `.g.cs` markup classes of every project, temp projects left out.
A project with markup but no generated folder fails with the build step named.
Assembly attributes and other generated files are left out, since the projects compile as one.

## `private static IReadOnlyList<string> TAuditProjectRead()`

The tracked project files, failing when there is none.

## `private static string TAuditTargetRead(string project, string output)`

The `bin` or `obj` folder of the project's configuration and target framework.

## `public static List<MetadataReference> TAuditReferenceRead()`

The assemblies of each framework pack and every package library in the reference root's build output.
When two packs ship an assembly of the same name, the higher version wins.
So WPF's `WindowsBase` shadows the runtime's facade and every WPF type binds.
The project's own assemblies are skipped, since their sources are in the compilation.
Missing build output fails with the build step named.

## `private static void TAuditAssemblyAdd(`

Keeps the assembly when it is managed and newer than the one of that name held so far.
A native library is skipped, since it cannot be a reference.
