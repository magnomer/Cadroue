# Audit principles

Every audit script in this folder has a counterpart among the convention tests in `tests/Cadroue.Tests.Convention`.
The two sides judge the same ground truth, but neither depends on the other.

## Principle

A script and its counterpart test never depend on each other.
Each side stands alone as far as it technically can.
Both sides still agree on the same ground truth with the same quality.
So when one side is broken, the other still tells the truth.

## Standalone

A script never reads, runs or builds a test to learn a rule, and never parses a test report.
A test never reads a script or its configuration to learn a rule or a setting.
Each side carries its own settings, copied by hand, in its own place.
A script keeps them in its `.json` file, and a test keeps them in its `TAudit*Setting.cs` file or ledger.
The scripts share `AuditBinder.cs` among themselves, and the tests share their helpers among themselves.
Nothing is shared across the two sides.
A file on either side may still be audited as a subject, like any other tracked file.
A script never writes a test file as part of an audit.
`SyncNames.ps1` refreshes the test's name registry, but it is a separate tool run by hand.

## Same truth

Given the same tree, both sides reach the same verdict.
They count the same hits and list the same hits.
They enumerate the same files, through git with the same pathspecs, exclusions and order.
They apply the same thresholds, ceilings, ledgers and enforced flags with the same meaning.
The Roslyn helpers pin the same Roslyn version and target framework as the convention tests.
Agreement covers results only, never presentation.
Each side reports in its own style: headings, wording, colour, tables and order are its own.

## Allowed differences

A difference in results is allowed only where one side technically cannot do what the other does.
A config fault may throw in a script and fail as a fact in a test.
A script may add views that gate nothing, such as totals, hotspots or at-limit names.
A console list may be cut short, while the report file stays complete.
Every allowed difference in results is written in the script header or the test sidecar.

## Project-agnostic

Every audit and every convention test is project-agnostic.
Its rule makes sense in any project, and only its settings name this project.
A rule about one project's own features is a test in that project's suite, with no script.

## Pairs

| Script | Counterpart tests |
|---|---|
| `AuditNames.ps1` | `TAuditName` |
| `AuditLines.ps1` | `TAuditLine` |
| `AuditComments.ps1` | `TAuditComment` |
| `AuditFake.ps1` | `TAuditFake` |
| `AuditFault.ps1` | `TAuditFault` |
| `AuditObject.ps1` | `TAuditObject` |
| `AuditPlatform.ps1` | `TAuditPlatform` |
| `AuditStructure.ps1` | `TAuditBorder`, `TAuditCensus`, `TAuditPurity`, `TAuditCharter` |
| `AuditUI.ps1` | `TAuditStrict`, `TAuditTruth`, `TAuditBoundary` |
| `AuditEncoding.ps1` | `TAuditEncoding` |

`TAuditRatchet` and `TAuditConvention` check the test settings themselves, so they have no counterpart.
`AuditNamesNew.ps1` checks names before any code exists, so it has no counterpart.
`TAssay*` tests check the walkers on hand-written sources, not the tree, so they have no counterpart.

## Changing a rule

A rule, threshold, ceiling or ledger row changes on both sides in the same change.
Each side is then run on its own, and both must report the same result.
A drift between the two shows as a verdict one side gives and the other does not.

## PowerShell

Every script runs the same on Windows PowerShell 5.1 and PowerShell 7.
A script stays pure ASCII, since 5.1 reads a file without a byte order mark as ANSI.
A native call with redirected error output runs under `Continue`, since 5.1 turns its stderr into an error.
Git output is decoded as UTF-8, and text is sorted by an ordinal key rather than by culture.
