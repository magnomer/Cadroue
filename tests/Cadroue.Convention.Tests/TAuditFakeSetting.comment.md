# TAuditFakeSetting.cs

## `internal static class TAuditFakeSetting`

Hand-written and tracked: the fake-audit switch, the report path, the ceilings and the scopes live here.
No script writes this file.
`auditfake.ps1` reads its own auditfake.json and never writes this file.

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
A count above fails the fact, a ceiling above the count is stale and fails too.
Lower a ceiling when a member goes live or is removed, never raise one to admit a new one.

## `public static readonly string[] TAuditFakeInclude`

The `git ls-files` patterns of the tests whose reads count as test reads.

## `public static readonly string[] TAuditMarkupInclude`

The `git ls-files` patterns of the markup whose words keep a member live.

## `public static readonly string[] TAuditFakeUsing`

The namespaces the test project imports implicitly, declared global in the test compilation.
