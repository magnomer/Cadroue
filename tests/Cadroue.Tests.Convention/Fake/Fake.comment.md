# Fake

This folder holds the fake audit finding members nothing live reads.
It also holds its candidates, members, serializer reads, symbols, uses, setting and walker.

## `TAuditFake`

Finds the members nothing live reads, which make the code look fuller than it works.
A member read only by tests is the most suspicious, since a green test then proves nothing the app does.
While `TAuditFakeEnforced` is false every kind fact passes and reports as a warning.
Enforced, a kind fails when it counts above its ceiling.
The walk runs once and every fact reads the same rows.
The report is written once, on the first fact that runs.
The runner's output is kept so a passing fact can still print its count.
An orphan is a member that neither a live reader nor a test reads.
Each hit names the file, line, member and the fake members that alone read it.
A tested member is one that tests read and nothing live does.
Each hit names the file, line, member and the tests that read it.
A ceiling above its count is stale and fails, so a shed hit is locked in.
That fails whether or not the kinds are enforced.
The tracked tests and markup drop the same excluded segments, suffixes and prefixes, as AuditFake.ps1 does.
The markup words are the attribute names, the words in attribute values and each element name after its last dot.
Text and comments are left out, so a word in prose keeps nothing alive.
The element names are the types markup constructs.
An empty test enumeration fails, since the audit would otherwise pass vacuously.
A project without markup is audited all the same, as AuditFake.ps1 does.
The report holds the counts, the ceilings and every hit, grouped by kind.
Its text matches the report of AuditFake.ps1 line for line, so a diff of the two shows any drift.
Lines end in LF alone.

## `TAuditFakeCandidate`

Registers the members and classes the fake audit tracks.
A member called from outside, such as an override, is no candidate.
Positional record properties are registered.
Enum members are left out, since a stored number may name them without code.
Every class that is not static is registered under its own key.
A derived class reads its base class, since constructing the one constructs the other.
A partial member is added once.
A stored member is a field, a field-like event or a property whose accessors carry no body.
An override, an interface member's implementation, an indexer, an extern and `Main` are called from outside.

## `TAuditFakeMember`

One source member the fake audit judges, keyed by its documentation id.
`TAuditMemberStored` marks a field, a field-like event or an auto-property, which a write alone never uses.
`TAuditMemberReaders` holds the keys of the source members that read it, or an empty key for a live root.
`TAuditMemberTesters` names the test members that read it.
`TAuditMemberLive` is set once a live reader reaches it.

## `TAuditFakeSerial`

Finds what the JSON serializer reads, since it reads by reflection where no walk can see a use.
Every type a `JsonSerializer` call names, by type argument or by argument, is serialized.
A source type and every property of it are marked serialized, through arrays, type arguments and property types.
The type is marked too, since the serializer constructs it.

## `TAuditFakeSetting`

Hand-written and tracked, with no script writing it.
`AuditFake.ps1` reads its own AuditFake.json and never writes this file.
When `TAuditFakeEnforced` is false every kind fact is a warning that passes.
When true a fact fails when a kind counts above its ceiling.
The report path is a format string over the version, so each version gets its own file.
The `Orphan` ceiling counts members that nothing live and no test reads.
The `Tested` ceiling counts members that only tests read.
A count above its ceiling fails the fact, and a ceiling above the count is stale and fails too.
Lower a ceiling when a member goes live or is removed, never raise one to admit a new one.
The test include patterns choose the tests whose reads count as test reads.
The markup include patterns choose the markup whose words keep a member live.
The using list holds the namespaces the test project imports implicitly, declared global in the test compilation.

## `TAuditFakeSymbol`

Folds a symbol into the one declaration it stands for, and keys it.
The candidate scan, the use scan and the serializer scan share these keys.
A read of an implementation also reads the interface members it stands for.
An extension call, a partial part, an accessor and a generic instance all fold into one declaration.
The key is the documentation id, which is the same in the source and the test compilation.

## `TAuditFakeUse`

Records every read of a tracked member from source or from tests.
A source read names its reader, and a test read names the test.
The liveness pass in `TAuditFakeWalker` follows those readers.
The empty reader key stands for a read outside any member, such as a top-level statement or a base list.
A construction or an attribute reads the class it builds.
A `foreach` also reads the enumerator members it binds to.
A read of a member also reads the interface members it implements.
A read inside the member itself is recursion and never counts.
A stored slot that is assigned, incremented or passed out is written, not read.
A field-like event is read only when a handler is added or removed.
A lambda or local function belongs to the member around it.
A body that is no candidate, such as a constructor or an override, reads for its tracked class.
A static constructor stays a root, since it runs on any static use.
A test read is labelled by the type and member name of the test, or its file name.

## `TAuditFakeWalker`

Finds the source members that no live code reads.
The source compilation is the shared binder's, generated markup classes included.
The tests compile on their own against it, so a test read binds to the same member.
Internal members the tests reach still bind as candidates, so their reads are kept.
Every class is tracked too, and only live code naming it, markup constructing it or the serializer keeps it live.
A constructor, an override or an interface implementation reads for its class, not as a root.
So a class that no live code names no longer keeps its callees alive.
`TAuditFakeCandidate` registers the candidates and `TAuditFakeUse` records the reads.
This class compiles the tests, settles what is live and writes the rows.
Liveness marks every member a root reads, then every member a live member reads, until nothing changes.
A root is a markup word, a serialized member, a class markup constructs, or a read from untracked code.
A markup word keeps a member of any type alive, since markup cannot be bound by type.
Each row is `Tested` when a test reads the member and `Orphan` otherwise.
The reason names the tests and the fake members that alone read it.
A type key, marked by the `T:` prefix, is left out of the report.
Rows come back in path and line order.

## Stamp

Commit: `90a13c9590ba`
- `TAuditFake.cs` `1f62f085565b60c8`
- `TAuditFakeCandidate.cs` `c76e4546064389cc`
- `TAuditFakeMember.cs` `3c2c44c59c44edbc`
- `TAuditFakeSerial.cs` `b9b72edf6488103a`
- `TAuditFakeSetting.cs` `ccf4c2e4bb6c749f`
- `TAuditFakeSymbol.cs` `c82651b1fabf2050`
- `TAuditFakeUse.cs` `019a711c29d548c2`
- `TAuditFakeWalker.cs` `962ec2a70800a982`
