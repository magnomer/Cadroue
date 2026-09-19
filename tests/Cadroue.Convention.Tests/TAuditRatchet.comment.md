# TAuditRatchet.cs

## `public sealed class TAuditRatchet`

Holds the audit settings against the committed copy, so a gate can only tighten inside a working tree.
A ceiling raised or an enforcement switched off fails until the user commits it.
Only the user commits, so the diff that loosens a gate is always seen.
The hold runs within one generation only.
A new generation changes what the walk reports, so its ceilings and waivers are baselined afresh.

## `private static readonly string TAuditSettingFolder`

The test project folder, named from the project so this file is the same in every project.

## `private static readonly Regex TAuditCeilingPattern`

A ceiling entry as it stands in a settings file.

## `private static readonly Regex TAuditEnforcedPattern`

The enforcement switch as it stands in a settings file.

## `private static readonly Regex TAuditGenerationPattern`

The generation a settings file was written at.

## `public void AuditRatchet_TruthCeiling_NeverRises()`

No custody ceiling stands above its committed value.

## `public void AuditRatchet_StrictCeiling_NeverRises()`

No strict ceiling stands above its committed value.

## `public void AuditRatchet_Enforced_NeverFlipsOff()`

A switch committed as true is still true.

## `private static void TAuditCeilingCheck(string path, IReadOnlyDictionary<string, int> current)`

Reads the committed ceilings and lists every kind whose current value is higher.
A file not yet committed has nothing to hold against and passes.

## `private static bool TAuditGenerationCheck(string committed)`

True when the committed settings were written at the running generation.

## `private static string? TAuditCommittedRead(string path)`

The file as `git show HEAD:` prints it, or null when it is not in HEAD.
