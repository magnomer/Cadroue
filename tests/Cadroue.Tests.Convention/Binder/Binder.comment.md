# Binder

This folder holds the one Roslyn compilation every binding audit shares.
It also holds its references, the side of a bound type and the symbol a node binds to.

## `TAuditBinder`

One Roslyn compilation of every tracked source under `src`, shared by every audit that binds.
Every binding walk reads the same trees, so the bind is paid once.
The generated markup classes of every project join the compilation, so an `x:Name` field binds like any other.
`TAuditReference` supplies the references and the generated code.
The compilation must hold no error, so an unbound name can never drop out of a walk unseen.
Any compile error fails, since a stale or missing build would otherwise weaken every walk silently.
Nothing here names a project, because the exclusions, the framework packs and the reference root come from the settings.
`TAuditBinderSymbol` reads symbols and types from these trees.
`TAuditBinderSide` tells which side of the cut a type is declared on.
Enumerating the sources with Git must find some, because an empty enumeration fails rather than passing vacuously.
The sources compile as one console program, since one of them carries the entry point.
The tracked trees are those under `src` and not under an `obj` folder, in path order.
The semantic model of each tree is built on first read and kept, since every walker reads it.
Reads are safe from many threads.
`TAuditCoverCheck` fails when a tracked source is missing from the compiled trees, so a dropped file cannot pass unseen.
`TAuditRingRead` matches a ring as its whole project folder under `src`.
So a ring never claims a sibling project whose folder name merely begins with its own.

## `TAuditAssayRun`

Runs a walk over a handed source set instead of the tracked tree.
Each key is a repo-relative virtual path.
So the side of a type follows its folder, as for a real file.
The walker runs unchanged, and only the trees and the compilation it binds against change.
The handed state is held in async-local values, so a tracked walk running beside an assay never sees it.
Outside an assay those values are null and the tracked compilation stands.
An assay compiles as a library, since its sources carry no entry point.
A key ending in `.xaml` is markup, kept as text for `TAuditMarkupRead`, and never parsed as C#.
During an assay only handed markup is read, so a missing key fails instead of reaching the disk.
The handed compilation and markup are cleared when the walk returns, even on a failure.

## `TAuditBinderSide`

The side of the cut a bound type is declared on, read from its source folder.
Shell is a shell or capsule root, conduct is the Conduct root, and engine is every other source.
Project-specific values live in the project's settings files.
A type from metadata sits on no side.
A type's side follows its type arguments and array elements too.
Logic means the sides below the cut, which a request reaches, so Conduct is included.
Engine means below Conduct.
A type counts as Conduct only when its type arguments are Conduct types too.
Generated sources sit under their project's `obj` folder, so they take that project's side.
A parameter counts as a local when the depth of a symbol is judged.
`TAuditDeportmentRead` lists every type and member name the driver namespace that the settings name declares.
So a markup binding to one is not a reach.
A name that a type below the cut also declares is removed, so a deeper binding cannot pass by coincidence.

## `TAuditBinderSymbol`

The symbol and the type a node binds to in the compilation of `TAuditBinder`.
It also holds the plain predicates the walkers ask of a bound symbol or type.
A symbol lookup by node is cached and safe to read from many threads.
The walkers ask for the same identifier many times.
A name inside a using directive or a namespace declaration names no type.
An enum, a struct, a record or a delegate is data, a type a value is the whole of.
A class or an interface is behaviour.
The type a name stands for is the type itself or the type owning the member the name picks.
A local or a parameter is a name the file gave itself, so its uses are not counted again.
An ambiguous bind takes the first candidate.
A declaration node reads as its declared symbol, then the bound symbol, then the first candidate.
A member is matched by its declaring type and name joined by a dot.

## `TAuditReference`

What the binder compiles against, and the generated code it adds.
Each project's output is read from the folder its own target framework names, never the newest folder.
A stale folder beside the right one can then never be read by mistake.
One reference set is built on first read and shared by every compilation.
Each set holds its images in native memory, which the collector frees only late.
A fresh set per assay compilation would multiply that memory.
Shared references also let Roslyn reuse their symbols, so each assay binds faster.
The generated code is the `.g.cs` markup classes of every project, temp projects left out.
A project with markup but no generated folder fails with the build step named.
Generated files not ending in `.g.cs`, such as assembly attributes, are left out.
At least one tracked project file must be enumerated, or the read fails.
The assemblies of each framework pack and every package library in the reference root's build output are referenced.
When two packs ship an assembly of the same name, the higher version wins.
So a pack's real assembly shadows the runtime's facade of the same name, and every type of the pack binds.
The project's own assemblies are skipped, since their sources are in the compilation.
Missing build output fails with the build step named.
A native library is skipped, since it cannot be a reference.

## Stamp

Commit: `90a13c9590ba`
- `TAuditBinder.cs` `2ab060ef20485ddb`
- `TAuditBinderSide.cs` `97196759b42a52c3`
- `TAuditBinderSymbol.cs` `d21c6b5a14188cd4`
- `TAuditReference.cs` `87cf89832a7a4f05`
