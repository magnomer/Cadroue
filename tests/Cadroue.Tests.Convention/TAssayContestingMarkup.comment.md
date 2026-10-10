# TAssayContestingMarkup.cs
Hash: `affb79423d2e800a`

## `public sealed class TAssayContestingMarkup`

Assays of the contesting hit for a property of a surface with generated markup.
They hand their own sources to `TAuditBinder.TAuditAssayRun` and walk with `TAuditTruthWalker.TAuditRun`.
The rule was switched off once, and its hit failed.
A failing assay exposes a walker bug, never a source to fix.

## `private const string TAssayMarkupPath = "src/Cadroue.UIVeneer/obj/Assay/PAssayView.g.cs";`

Where the markup compiler would write the generated half, so the tracked tree never walks it.

## `private const string TAssayMarkupDriver = """`

A driver whose field copies a property of a surface type with markup.

## `private const string TAssayMarkupText = """`

The generated half of that surface, written as the markup compiler writes it.

## `public void AuditTruth_MarkupProperty_ReportsContesting()`

A public property of a type with generated markup has an unseen writer and is plain.

## `public void AuditTruth_CodeProperty_AllowsEngineCopy()`

Without the generated half the same property is engine through its C# writer.

## `private static List<TViolation> TAssayMarkupRun(bool markup)`

Walks the markup driver, with or without its generated half, and keeps the contesting hits.
