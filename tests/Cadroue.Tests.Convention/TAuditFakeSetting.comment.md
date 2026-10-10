# TAuditFakeSetting.cs
Hash: `b961fbab6d81572a`

## `internal static class TAuditFakeSetting`

Hand-written and tracked.
The fake-audit switch, the report path, the ceilings and the scopes live here.
`AuditFake.ps1` reads its own AuditFake.json and never writes this file.

## `public const int TAuditGeneration = 21;`

Numbers the revision of the fake-audit settings this file holds.

## `public const bool TAuditFakeEnforced = true;`

False makes every fake fact a warning that passes.
True fails a fact when a kind counts above its ceiling.

## `public const string TAuditFakeReport = "temp/audit/Fake-{0}.md";`

Where the report lands, with the version in the name.
`temp` is ignored by Git, so a run never dirties the tree.

## `public static readonly IReadOnlyDictionary<string, int> TAuditFakeCeiling`

The hit count each kind may reach.
`Orphan` counts the members that nothing live and no test reads.
`Tested` counts the members that only tests read.
A count above its ceiling fails the fact.
A ceiling above the count is stale and fails too.
Lower a ceiling when a member goes live or is removed.
Never raise one to admit a new one.

## `public static readonly string[] TAuditFakeInclude`

The `git ls-files` patterns of the tests whose reads count as test reads.

## `public static readonly string[] TAuditMarkupInclude`

The `git ls-files` patterns of the markup whose words keep a member live.

## `public static readonly string[] TAuditFakeUsing`

The namespaces the test project imports implicitly, declared global in the test compilation.
