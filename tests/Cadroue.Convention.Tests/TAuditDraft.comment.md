# TAuditDraft.cs

## `public sealed class TAuditDraft`

Keeps every draft field on every side that carries a draft.
A new record field must be named by the portrait, the markup and the exemplar, or waived by name.
Naming is a word match over the side's tracked sources, so a positional argument never counts.
The settings and every waiver reason live in `TAuditDraftSetting`.

## `public void AuditDraft_Portrait_NamesEveryProperty()`

The portrait side: the `Portrait` folders of the core and the application, and the `LEnginePortrait` files of the engine.

## `public void AuditDraft_Markup_NamesEveryProperty()`

The markup side: the `Markup` folder of the core and the engine loader.

## `public void AuditDraft_Exemplar_NamesEveryProperty()`

The exemplar side: the one file that builds the shared exemplar entry.

## `public void AuditDraft_Setting_MatchesTheRecords()`

Every listed type must be a record under the include, and every shared waiver must still be a property.

## `private static void TAuditDraftCheck(string side, IReadOnlyList<string> include, IReadOnlyList<string> waiver)`

A project that declares no draft record has nothing to hold, so the check returns at once.
Reads the records, joins the side's sources, and looks every property up.
A shared waiver is skipped outright.
A side waiver that the side names anyway is stale, as is one naming no property.
The report lists the unnamed properties by record, then the stale waiver lines.

## `private static Dictionary<string, IReadOnlyList<string>> TAuditDraftRead(string repoRoot)`

Parses the included sources and keeps the positional parameters of each listed record.
Only positional parameters count, so a derived property such as `LCardDraftEmpty` never enters the audit.

## `private static string TAuditTextRead(string repoRoot, IReadOnlyList<string> include)`

Joins every tracked file the patterns match, refusing an empty match as a stale pattern.

## `private static bool TAuditNameCheck(string text, string name)`

True when the name stands as a whole word somewhere in the text.
