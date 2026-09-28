# TAuditNameFilter.cs

## `internal static class TAuditNameFilter`

What says a specimen lies outside the audit.
A name fixed from outside, by a framework contract, an override, an extern or an attribute, is skipped.
A declaration marked as generated code is skipped.
A name a source generator emits is built by the walker and audited at its declaration's line.
The prefix read here decides which prefix a name carries, if any.
The walker asks these questions and audits only what passes.

## `private static Dictionary<string, List<TypeDeclarationSyntax>> TAuditTypeParts`

Every part of every source type, keyed by namespace, enclosing types, name and arity.

## `private static Dictionary<string, List<string>> TAuditTypeKeys`

The keys of every source type with one simple name, so a base list name finds its declarations.

## `internal static void TAuditTypeScan(IEnumerable<SyntaxTree> trees)`

Indexes every type part before any name is judged.

## `private static bool TAuditContractFind(string key, string name, HashSet<string> visited)`

True when the type or a source base type at any depth declares a framework contract naming the member.
Each type key is visited once, so a cycle in the base lists cannot loop.

## `private static string TAuditKeyRead(TypeDeclarationSyntax type)`

One key per type: its namespace, its enclosing types, its name and its arity, as auditnames.ps1 keys it.

## Inline notes

### `internal static bool TAuditContractCheck(SyntaxNode node, string name)`

A member may be a contract member of a framework interface the enclosing type declares.
Such a name is externally fixed by that interface.
That is the same reason an explicit interface implementation is exempt.
The rule is applied here to the implicit form.
For a partial type the interface may be declared in another part in another file.
The walker indexes every part first, so every part's interfaces are read.
A contract declared by a source base type counts too, as it does in auditnames.ps1.
A contract name on a type that declares no such interface anywhere is audited like any other.
