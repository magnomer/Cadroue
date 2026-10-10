# AuditBinder.cs
Hash: `02159111a03b5ab7`

The one compilation every bound audit script shares, the script counterpart of the convention test's `TAuditBinder`.
AuditStructure, AuditObject, AuditFake, AuditFault, AuditPlatform and AuditUI copy it into their helper beside their own program.
Each helper builds once into the temp folder, keyed by a hash of its text, binder, framework and SDK.
So an edit rebuilds every helper once.

## Configuration

`AuditBinder.json` names the source root, the build configuration, the host project and the shared frameworks.
Its exclusions match the name audit settings the convention tests enumerate with.
The tests hold their own copy, and scripts/principles.md keeps the two in step by hand.

## Binding

The sources are the tracked and untracked `.cs` files under the source root, never ignored ones.
The generated `.g.cs` files of every project join them, `_wpftmp` copies left out.
A project that holds markup but has no generated folder fails, since the solution was not built.
The references are the shared frameworks beside the helper runtime and the host build output, own assemblies left out.
The helper framework picks those shared frameworks, so AuditFake and AuditObject pin it to the test framework.
A compile error fails the bind, so an unbound name can never slip past an audit.

## Trees

`LAuditTrees` holds the trees under the source root outside any `obj` folder, the files an audit walks.
The generated trees stay in the compilation, so a partial type binds whole.

## `public static List<string> LAuditFileRead(`

Git enumerates the files, so tracked and untracked files count and ignored ones never do.
Each pattern matches case-insensitively.
A tracked file deleted from disk is dropped rather than failing a later read.
An empty `roots` list admits every folder.
Segment and suffix exclusions ignore case, but a name prefix must match its case exactly.
The list comes back sorted, so an audit's findings keep a stable order.
A git failure throws with git's own error text.
