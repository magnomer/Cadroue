# TAuditNameFilter.cs

## `internal static class TAuditNameFilter`

What says a specimen lies outside the audit.
A name fixed from outside, by a framework contract, an override, an extern or an attribute, is skipped.
A generated member is audited at the declaration it is built from, never at the generated name.
The prefix read here decides which prefix a name carries, if any.
The walker asks these questions and audits only what passes.

## Inline notes

### `internal static bool TAuditContractCheck(SyntaxNode node, string name)`

A member may be a contract member of a framework interface the enclosing type declares.
Such a name is externally fixed by that interface.
That is the same reason an explicit interface implementation is exempt.
The rule is applied here to the implicit form.
For a partial type the interface may be declared in another fragment this file cannot see.
So a contract-member name is accepted on the name alone.
