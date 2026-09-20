# TAuditBoundarySetting.cs

## `internal static class TAuditBoundarySetting`

Hand-written and tracked: the names and files the boundary facts hold the shell to.
No script writes this file.
A list left empty keeps its fact passing, so the fact file stays identical to the sibling project's.

## `public static readonly string[] TAuditBoundaryForbidden`

The calls a shell line may not make, each a regular expression.
A `using static` of a logic namespace, which would let a shell file call logic bare and hide the reach.
A `using static` of a shell namespace is the row-primitive style and stays allowed.
The shell resolves no state from text, so no state call is listed.

## `public static readonly string[] TAuditBoundaryState`

State names a panel may not compare against outside a converter.
Empty here: the shell holds no such state name.

## `public static readonly string[] TAuditBoundaryConverter`

The files that may read a state apart.
Empty here.

## `public static readonly string[] TAuditBoundaryHidden`

The constructs that keep code out of a syntax walk: a preprocessor branch, reflection, `dynamic`, inline markup code.
Also an enum parsed from text and a `using` alias, which each give a logic name a second spelling.
A branch would be skipped by the walk, and reflection names nothing the walk can follow.

## `public static readonly string[] TAuditBoundaryLoader`

The files that read embedded resources through the assembly, and so may name reflection.
Empty here: the version and the catalogs are read below the shell.

## `public const string TAuditBoundaryReflection`

The namespace that reaches a member by its name as text.

## `public const string TAuditBoundaryTimer`

The type a panel would hold its own debounce timer in.

## `public static readonly string[] TAuditBoundaryHold`

The file name patterns of the sources that may not keep a timer.
Empty here: no panel holds a draft the engine waits out.

## `public const string TAuditBoundaryPanel`

A panel type declaration, by its prefix, which only the shell may hold.
