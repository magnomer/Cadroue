# TAuditRegistry.cs

## `internal sealed class TAuditRegistry`

The registered bases, verbs, and exemptions the name audit checks against.
Loaded from `TAuditNameRegistry`, so the tests read no document and no embedded resource.

## `public bool TAuditExemptValidate(string name, string sourcePath)`

A row grants its name only inside the files it names.
The same word stays a violation everywhere else.
`*` grants a mechanism that is universal by spelling, such as a template part.

## `public static TAuditRegistry TAuditLoad()`

Builds the sets from the registry arrays of TAuditNameRegistry.cs, the one generated file in this project.
That file holds names only, never a setting.
Regenerate it before the tests: a stale registry audits against stale names.
File names in an exemption compare case-insensitively, since the file system does.
