# TAuditFakeWalker.cs

## `internal static class TAuditFakeWalker`

Finds the source members that no live code reads.
The source compilation is the shared binder's, generated markup classes included.
The tests compile on their own against it, so a test read binds to the same member.
Every class is tracked too, and it is live only when live code, markup or the serializer constructs it.
A constructor, an override or an interface implementation reads for its class, not as a root.
So a class nobody constructs no longer keeps its callees alive.

## `private const string TAuditRootReader = "";`

The reader key of a read from code that is live by itself, such as top-level statements or generated code.

## `private const string TAuditTypeMark = "T:";`

The documentation id prefix of a type, which marks a class key the report leaves out.

## `private static readonly CSharpParseOptions TAuditSyntaxOptions`

The parse options every test source is read with.

## `public static IReadOnlyList<TViolation> TAuditRun(`

Registers every candidate member and class, records every read from source and from tests, then marks what is live.
`markup` holds the words markup attributes use, and `elements` the types markup constructs.
Returns one row per member left fake, in path and line order.

## `private static CSharpCompilation TAuditTestCreate(IReadOnlyList<string> testPaths)`

Compiles the tests against the source compilation with the test project's implicit namespaces.
Internal members the tests reach still bind as candidates, so their reads are kept.

## `private static void TAuditMemberScan(SemanticModel model, Dictionary<string, TAuditFakeMember> members)`

Registers every candidate member one tree declares, positional record properties included.
Enum members are left out, since a stored number may name them without code.
Every class that is not static is registered under its own key.

## `private static void TAuditLineageAdd(Dictionary<string, TAuditFakeMember> members)`

A derived class reads its base class, since constructing the one constructs the other.

## `private static void TAuditMemberAdd(`

Adds one member under its key unless a partial part already added it.

## `private static IEnumerable<ISymbol> TAuditDeclaredRead(SemanticModel model, MemberDeclarationSyntax member)`

The symbols one member declaration declares, or none for a nested type or delegate.

## `private static bool TAuditStoredCheck(MemberDeclarationSyntax member)`

True for a field, a field-like event or a property whose accessors carry no body.

## `private static bool TAuditCandidateCheck(ISymbol symbol)`

An ordinary method, a property, an event or a field that the code alone must keep alive.
An override, an interface member's implementation, an indexer, an extern and `Main` are called from outside.

## `private static List<ISymbol> TAuditContractRead(ISymbol symbol)`

The interface members one member implements.
A read of an implementation also reads the interface members it stands for.

## `private static void TAuditUseScan(`

Binds every name that spells a candidate and records the read.
A construction or an attribute reads the class it builds.
A `foreach` also reads the enumerator members it binds to.

## `private static void TAuditUseAdd(`

Records one read of a member and of the interface members it implements.
A read inside the member itself is recursion and never counts.

## `private static bool TAuditReadCheck(SyntaxNode site, ISymbol target, TAuditFakeMember member)`

Whether one reference reads the member rather than only writing it.
A stored slot that is assigned, incremented or passed out is written, not read.
A field-like event is read only when a handler is added or removed.

## `private static string? TAuditOwnerRead(`

The key of the member whose body holds the reference, or null at type level.
A lambda or local function belongs to the member around it.
A body that is no candidate, such as a constructor or an override, reads for its tracked class.
A static constructor stays a root, since it runs on any static use.

## `private static string TAuditLabelRead(SemanticModel model, SyntaxNode site)`

The type and member name of the test that holds the reference, or its file name.

## `private static void TAuditLiveApply(`

Marks live every member a root reads, then every member a live member reads, until nothing changes.
A root is a markup word, a serialized member, a class markup constructs, or a read from untracked code.
A markup word keeps a member of any type alive, since markup cannot be bound by type.
The strict audit counts a markup name below the driver as a reach, so such a binding is still held.

## `private static TViolation TAuditRowCreate(TAuditFakeMember member, Dictionary<string, TAuditFakeMember> members)`

One hit, named `Tested` when a test reads the member and `Orphan` otherwise.
The reason names the tests and the fake members that alone read it.

## `private static string TAuditListFormat(List<string> names)`

The first three names and how many more there are.

## `private static ISymbol TAuditNormalRead(ISymbol symbol)`

The one declaration a reference stands for.
An extension call, a partial part, an accessor and a generic instance all fold into it.

## `public static string TAuditKeyRead(ISymbol symbol)`

The documentation id of the folded declaration, which is the same in the source and the test compilation.
