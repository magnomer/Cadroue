# TAuditRegistry.cs

## `public static TAuditRegistry TAuditLoad()`

Loads the registered names from TAuditNameRegistry.cs, the one generated file in this project.
That file is written by syncnames.ps1 from docs-internal and holds names only, never a setting.
Run syncnames first: a test run before a sync audits against stale names.
