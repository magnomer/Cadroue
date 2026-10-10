# TAuditRegistry.cs
Hash: `06a72acbb75fb62a`

## `internal sealed class TAuditRegistry`

The registered bases, verbs, and exemptions the name audit checks against.
Loaded from `TAuditNameRegistry` and `TAuditVerbRegistry`, so the tests read no document and no embedded resource.

## `public bool TAuditExemptValidate(string name, string sourcePath)`

A row grants its name only inside the files it names.
The same word stays a violation everywhere else.
`*` grants a mechanism that is universal by spelling, such as a template part.

## `public static TAuditRegistry TAuditLoad()`

Builds the sets from the registry arrays of TAuditNameRegistry.cs and TAuditVerbRegistry.cs, the generated files in this project.
Those files hold names only, never a setting.
Regenerate it before the tests.
A stale registry audits against stale names.
File names in an exemption compare case-insensitively, since the file system does.
