# TAuditFakeWalker.cs
Hash: `eb85e80055b371c6`

## `internal static class TAuditFakeWalker`

Finds the source members that no live code reads.
The source compilation is the shared binder's, generated markup classes included.
The tests compile on their own against it, so a test read binds to the same member.
Every class is tracked too, and it is live only when live code, markup or the serializer constructs it.
A constructor, an override or an interface implementation reads for its class, not as a root.
So a class nobody constructs no longer keeps its callees alive.

`TAuditFakeCandidate` registers the candidates and `TAuditFakeUse` records the reads.
This class compiles the tests, settles what is live and writes the rows.

## `private const string TAuditTypeMark = "T:";`

The documentation id prefix of a type, which marks a class key the report leaves out.

## `private static readonly CSharpParseOptions TAuditSyntaxOptions`

The parse options every test source is read with.

## `public static IReadOnlyList<TViolation> TAuditRun(IReadOnlyList<string> testPaths, IReadOnlySet<string> markup, IReadOnlySet<string> elements)`

Registers every candidate member and class, records every read from source and from tests, then marks what is live.
`markup` holds the words markup attributes use, and `elements` the types markup constructs.
Returns one row per member left fake, in path and line order.

## `private static CSharpCompilation TAuditTestCreate(IReadOnlyList<string> testPaths)`

Compiles the tests against the source compilation with the test project's implicit namespaces.
Internal members the tests reach still bind as candidates, so their reads are kept.

## `private static void TAuditLiveApply(Dictionary<string, TAuditFakeMember> members, IReadOnlySet<string> markup, IReadOnlySet<string> elements, HashSet<string> serialized)`

Marks live every member a root reads, then every member a live member reads, until nothing changes.
A root is a markup word, a serialized member, a class markup constructs, or a read from untracked code.
A markup word keeps a member of any type alive, since markup cannot be bound by type.
The strict audit counts a markup name below the driver as overreaching, so such a binding is still held.

## `private static TViolation TAuditRowCreate(TAuditFakeMember member, Dictionary<string, TAuditFakeMember> members)`

One hit, named `Tested` when a test reads the member and `Orphan` otherwise.
The reason names the tests and the fake members that alone read it.

## `private static string TAuditListFormat(List<string> names)`

The first three names and how many more there are.
