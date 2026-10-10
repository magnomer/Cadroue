# TAuditFakeUse.cs
Hash: `019a711c29d548c2`

## `internal static class TAuditFakeUse`

Records every read of a tracked member from source or from tests.
A source read names its reader, and a test read names the test.
The liveness pass in `TAuditFakeWalker` follows those readers.

## `private const string TAuditRootReader = "";`

The reader key of a read from code that is live by itself, such as top-level statements or generated code.

## `public static void TAuditUseScan(SemanticModel model, Dictionary<string, TAuditFakeMember> members, HashSet<string> names, bool test)`

Binds every name that spells a candidate and records the read.
A construction or an attribute reads the class it builds.
A `foreach` also reads the enumerator members it binds to.

## `private static void TAuditUseAdd(SemanticModel model, SyntaxNode site, ISymbol target, Dictionary<string, TAuditFakeMember> members, bool test)`

Records one read of a member and of the interface members it implements.
A read inside the member itself is recursion and never counts.

## `private static bool TAuditReadCheck(SyntaxNode site, ISymbol target, TAuditFakeMember member)`

Whether one reference reads the member rather than only writing it.
A stored slot that is assigned, incremented or passed out is written, not read.
A field-like event is read only when a handler is added or removed.

## `private static string? TAuditOwnerRead(SemanticModel model, SyntaxNode site, Dictionary<string, TAuditFakeMember> members)`

The key of the member whose body holds the reference, or null at type level.
A lambda or local function belongs to the member around it.
A body that is no candidate, such as a constructor or an override, reads for its tracked class.
A static constructor stays a root, since it runs on any static use.

## `private static string TAuditLabelRead(SemanticModel model, SyntaxNode site)`

The type and member name of the test that holds the reference, or its file name.
