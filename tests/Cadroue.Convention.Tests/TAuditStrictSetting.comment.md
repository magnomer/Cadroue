# TAuditStrictSetting.cs

## `internal static class TAuditStrictSetting`

Hand-written and tracked: the strict-audit switch, the report path and the classifiers live here.
No script writes this file.

## `public const bool TAuditStrictEnforced = true;`

False makes every strict fact a warning that passes.
True fails a fact when a kind counts above its ceiling.

## `public const string TAuditStrictReport = "temp/audit/Truth-{0}.md";`

Where the report lands, with the version in the name.
`temp` is ignored by Git, so a run never dirties the tree.

## `public static readonly IReadOnlyDictionary<string, int> TAuditStrictCeiling`

The hit count each kind may reach.
A count above fails the fact, a ceiling above the count is stale and fails too.
Lower a ceiling when the shell sheds a hit, never raise one to admit a new one.

## `public static readonly string[] TAuditVeneerInclude`

Every class under these roots is a veneer, as is any class deriving from a control base.

## `public static readonly string[] TAuditReachInclude`

The `git ls-files` patterns of the shell markup.

## `public const string TAuditDeportmentNamespace = "Cadroue.UIDeportment";`

The namespace whose types and members a markup binding may name, since Deportment is shell state, not logic.

## `public static readonly string[] TAuditReachNamespaces`

Namespaces a markup file may not map, since mapping one lets a binding reach logic.

## `public static readonly string[] TAuditTreatVerbs`

Query methods that, applied to a logic value, are data treatment.
