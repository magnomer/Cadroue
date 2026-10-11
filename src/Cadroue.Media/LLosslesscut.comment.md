# LLosslesscut.cs
Hash: `c017202c395edd45`

## `public static LLosslesscutProject? LLosslesscutProjectRead(`

An unreadable or malformed file comes back as null with its fault, so the caller decides how to report it.

## `public static LLosslesscutProject LLosslesscutParse(string lLosslesscutText)`

Times are read as decimals, so imported cut points keep the project's exact millisecond values.
A segment that is not an object is kept as a placeholder, so validation can report it by index.

## `public static LLosslesscutResult LLosslesscutValidate(`

Each rejected segment becomes an issue instead of an exception, so one bad segment never loses the rest.
A project naming no media file counts as agreeing with any source.
An omitted end extends to the open media duration, and fails only when that duration is unknown.

## `public static bool LLosslesscutVersionCheck(int? lLosslesscutVersion) =>`

A project without a version is accepted as the supported format.

## `public static IReadOnlyList<string> LLosslesscutAdjacentRead(string lLosslesscutSourcePath)`

Only projects in the source folder that name the source file are offered, newest first.
An unrelated or malformed project is skipped, so it never blocks adjacent-project detection.
