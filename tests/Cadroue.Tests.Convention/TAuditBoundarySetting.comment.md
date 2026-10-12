# TAuditBoundarySetting.cs
Hash: `0cafeafae95b1658`

## `internal static class TAuditBoundarySetting`

Hand-written and tracked.
It holds the patterns and file lists the boundary facts hold the UI sources to.
No script writes this file.

## `public const int TAuditGeneration`

Numbers the revision of the boundary settings this file holds.

## `public static readonly string[] TAuditBoundaryForbidden`

The lines no UI source may hold.
Only a `using static` of a project namespace is listed, since it hides a logic name behind a bare call.
Each is a regular expression, so spacing does not slip past.

## `public static readonly string[] TAuditBoundaryState`

The state names a UI source may not compare against outside the converters.
Empty, so the fact holds nothing until a Cadroue state name is listed.

## `public static readonly string[] TAuditBoundaryConverter`

The files that may read a state apart.
Empty, since no state name is listed for a converter to read.
A row that compares no state fails as stale.

## `public static readonly string[] TAuditBoundaryHidden`

The constructs that keep code out of a syntax walk: a preprocessor branch, reflection, `dynamic`, inline markup code.
Also an enum parsed from text, a `using` alias and an `extern alias`.
Each gives a logic name a second spelling.
The walkers parse without symbols, so a branch would be skipped, and reflection names nothing the walk can follow.

## `public static readonly string[] TAuditBoundaryLoader`

The UI files that may name reflection, such as a reader of embedded resources.
Empty, since no UI source names `System.Reflection`.

## `public const string TAuditBoundaryReflection`

The namespace that reaches a member by its name as text.

## `public const string TAuditBoundaryTimer`

The type a held draft may not keep its own debounce timer in.
It is read only for files matching `TAuditBoundaryHold`.

## `public static readonly string[] TAuditBoundaryHold`

The file name patterns of the UI sources that hold a draft and may not keep a timer.
Empty, since no Cadroue UI file holds a draft by name.
A pattern naming no UI file fails as stale.

## `public const string TAuditBoundaryPanel`

A `P` or `PS` type declaration, which only the UI folders may hold.
