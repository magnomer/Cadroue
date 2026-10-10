# TAuditFakeCandidate.cs
Hash: `c76e4546064389cc`

## `internal static class TAuditFakeCandidate`

Registers the members and classes the fake audit tracks.
A member called from outside, such as an override, is no candidate.

## `public static void TAuditMemberScan(SemanticModel model, Dictionary<string, TAuditFakeMember> members)`

Registers every candidate member one tree declares, positional record properties included.
Enum members are left out, since a stored number may name them without code.
Every class that is not static is registered under its own key.

## `public static void TAuditLineageAdd(Dictionary<string, TAuditFakeMember> members)`

A derived class reads its base class, since constructing the one constructs the other.

## `private static void TAuditMemberAdd(Dictionary<string, TAuditFakeMember> members, ISymbol symbol, SyntaxNode node, string path, bool stored)`

Adds one member under its key unless a partial part already added it.

## `private static IEnumerable<ISymbol> TAuditDeclaredRead(SemanticModel model, MemberDeclarationSyntax member)`

The symbols one member declaration declares, or none for a nested type or delegate.

## `private static bool TAuditStoredCheck(MemberDeclarationSyntax member)`

True for a field, a field-like event or a property whose accessors carry no body.

## `private static bool TAuditCandidateCheck(ISymbol symbol)`

An ordinary method, a property, an event or a field that the code alone must keep alive.
An override, an interface member's implementation, an indexer, an extern and `Main` are called from outside.
