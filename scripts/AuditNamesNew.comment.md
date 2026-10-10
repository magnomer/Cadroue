# AuditNamesNew.ps1

Checks proposed names against the naming convention before any code uses them.
It reads the same bases, verbs, exemptions and settings as `AuditNames.ps1`, but touches no source.
It runs in plain PowerShell, so it answers in about a second.

## Registry

The bases, verbs and exemptions come from `scripts/AuditNames.registry.json`, never from `docs-internal`.
`SyncNames.ps1` is the only reader of `docs-internal`.
It writes that JSON registry for the audits and `TAuditNameRegistry.cs` for the convention tests.
The chain is `docs-internal` -> `SyncNames.ps1` -> registry JSON (audits) and `TAuditNameRegistry.cs` (tests).
A missing or malformed registry fails the run and asks you to run `SyncNames.ps1`.
Run `SyncNames.ps1` after every edit to `docs-internal`, or this check sees stale names.

## Usage

```
AuditNamesNew [-File <names.txt>] [-Root <path>] [-Help] [<token> ...]
```

Every token is a name or a tagged word.
Tokens from `-File` and from the command line are checked together.

| Token | Meaning |
|---|---|
| `PHelper` | A name whose kind is guessed from its last component. |
| `method:LHelperRead` | A method name, which must end in a registered verb. |
| `data:PHelperFont` | A data or type name, which must not end in a verb. |
| `test:Load_Empty_Throws` | A test method name, checked only against the test-prefix gate. |
| `base:Helper` | A proposed new base, checked and treated as registered for this run. |
| `verb:Tidy` | A proposed new verb, checked and treated as registered for this run. |

## Examples

```
AuditNamesNew tester
AuditNamesNew PHelper base:Helper
AuditNamesNew method:LHelperRead data:PHelperFont base:Helper
AuditNamesNew -File names.txt
```

## Name list file

A list file holds tokens separated by spaces, commas, or line breaks.
Text after `#` on a line is ignored.
Blank lines are ignored.

```
# New names
base:Helper
PHelper
method:LHelperRead
data:PHelperFont
```

## Checks on a name

- A name in the `AUDIT:EXEMPT` block passes with a note naming the files its row grants.
- The name carries a prefix from `AuditNames.json`, in any of its listed cases.
- Leading underscores are skipped before the prefix is read.
- The rest splits cleanly into PascalCase components.
- The first component is a registered base, or one passed with `base:`.
- The component count stays within `componentLimit` after the prefix.
- A count at `componentReview` passes with a note.
- A method ends in a registered verb.
- A data or type name does not end in a verb, whatever its component count.
- A guessed kind is a method when it has two or more components and the last is a verb.

## Checks on a test method name

- A descriptive name without a prefix passes while no test method carries the test prefix.
- A name with the test prefix passes, and then every test method must carry it.
- A name with any other prefix fails.
- The source is not read, so whether a test method already carries the prefix is for you to know.

## Checks on a new base or verb

- The word is two or more letters and starts with a capital.
- The word is exactly one component.
- The word is never both a base and a verb.
- A word already registered passes with a note.

## Output

Each token prints `PASS` or `FAIL`, its kind, and its prefix and components.
Each fault prints on its own indented line below.
The exit code is 0 when every token passes and 1 otherwise.

## Limits

A member's owning type is not counted, so count its components yourself.
The check cannot judge meaning, ownership, or whether a base is the right one.
A passing name still needs its new base or verb added to `docs-internal`.
`SyncNames.ps1` then carries it into the registry JSON.
