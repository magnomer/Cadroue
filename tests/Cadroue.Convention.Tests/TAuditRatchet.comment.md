# TAuditRatchet.cs

## `public sealed class TAuditRatchet`

Holds the audit settings against the committed copy, so a gate can only tighten inside a working tree.
A ceiling raised, a waiver row added or an enforcement switched off fails until the user commits it.
Only the user commits, so the diff that loosens a gate is always seen.
The hold runs within one generation only.
A new generation changes what the walk reports, so its ceilings and waivers are baselined afresh.

## `private static readonly string TAuditSettingFolder`

The test project folder, named from the project so this file is the same in every project.

## `private static readonly Regex TAuditCeilingPattern`

A ceiling entry as it stands in a settings file.
A key is anything but a quote, so a chain key with its colon, greater-than and dots is read whole.

## `private static readonly Regex TAuditRowPattern`

A `path:name` waiver row as it stands in a settings file.

## `private static readonly Regex TAuditRolePattern`

A dictionary entry whose value is a list of names, with the key and the list captured.

## `private static readonly Regex TAuditQuotedPattern`

One quoted name inside a captured list.

## `private static readonly Regex TAuditEnforcedPattern`

An enforcement flag as it stands in a settings file.

## `private static readonly Regex TAuditGenerationPattern`

The generation constant as it stands in a settings file.

## `public void AuditRatchet_TruthCeiling_NeverRises()`

No truth ceiling stands above its committed value.

## `public void AuditRatchet_StrictCeiling_NeverRises()`

No strict ceiling stands above its committed value.

## `public void AuditRatchet_ObjectCeiling_NeverRises()`

No object or part ceiling stands above its committed value.

## `public void AuditRatchet_ChainCeiling_NeverRises()`

No chain ceiling stands above its committed value.

## `public void AuditRatchet_FrameCeiling_NeverRises()`

No frame ceiling stands above its committed value.

## `public void AuditRatchet_LineCeiling_NeverRises()`

No line ceiling stands above its committed value.

## `public void AuditRatchet_ChainWaiver_NeverGrows()`

No chain waiver is missing from the committed waiver list.

## `public void AuditRatchet_FrameWaiver_NeverGrows()`

No frame waiver is missing from the committed waiver list.

## `public void AuditRatchet_ChainReach_NeverWidens()`

No ring reaches a ring the committed chain did not let it reach.
A reach dropped is a tightening and passes.

## `public void AuditRatchet_ChainSurface_NeverWidens()`

No surface admits a name the committed surface did not.

## `public void AuditRatchet_FrameAllowed_NeverWidens()`

No namespace joins the frame without a commit.

## `public void AuditRatchet_Enforced_NeverFlipsOff()`

No enforcement flag committed as true is false in the working tree.

## `private static void TAuditCeilingCheck(string path, IReadOnlyDictionary<string, int> current)`

Reads the committed ceilings and lists every kind whose current value is higher.
A file not yet committed has nothing to hold against and passes.

## `private static void TAuditRowCheck(string path, string block, IReadOnlyList<string> rows, string label)`

Reads the committed rows of one waiver block and lists every current row missing from it.

## `private static void TAuditListCheck(string path, string block, IReadOnlyDictionary<string, string[]> current, string label, string verb)`

Reads the committed lists of one dictionary block and lists every current name a key gained.

## `private static string TAuditBlockRead(string committed, string name)`

The text of one setting from its name to the end of its initializer.

## `private static bool TAuditGenerationCheck(string committed)`

True when the committed settings were written at the running generation.

## `private static string? TAuditCommittedRead(string path)`

The file as `git show HEAD:` prints it, or null when it is not in HEAD.
