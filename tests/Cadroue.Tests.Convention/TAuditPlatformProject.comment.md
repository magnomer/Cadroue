# TAuditPlatformProject.cs
Hash: `69626c7ae8f733ad`

## `internal static class TAuditPlatformProject`

Reads properties and items out of a loaded project file for the platform audit.
Element names and values compare without case, as MSBuild does.

## `public static bool TAuditTrueCheck(string value)`

MSBuild reads a boolean property without regard to case, so the check does too.

## `public static bool TAuditListCheck(XDocument document, string element, string rule)`

True when one property of a project file lists the rule among its codes.
Codes split at semicolons, commas and white space as the compiler reads them, and compare without case.

## `public static string[] TAuditFrameworkRead(XDocument document)`

Every framework one project file targets, whether written singly or as a list.

## `public static IEnumerable<string> TAuditValueRead(XDocument document, string element)`

The trimmed text of every element with that name in any case and any namespace.

## `public static IEnumerable<string> TAuditIncludeRead(XDocument document, string element)`

The `Include` value of every element with that name in any case.
