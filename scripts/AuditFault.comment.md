# AuditFault.ps1
Hash: `ba08597c744bdfbe`

Counts the catch clauses below Conduct that swallow a fault, without the convention tests.
It is the standalone counterpart of `TAuditFault` and reports the same hits.
It binds the sources through the shared binder, so build the solution first.

## Usage

```
AuditFault [-Root <path>] [-ReportDirectory <path>] [-Top <n>] [-Open] [-NoOpen] [-NoPause] [-Help]
```

`-Root` audits another git working tree with the same configuration.
`-ReportDirectory` writes the report and the page to another folder.
`-Top` sets how many rows of each ring the console shows.
`-Open` opens the Markdown report after the run.
`-NoOpen` writes the page without opening it.
`-NoPause` turns console paging off.

## Rule

Each catch clause in a ring file is judged on its block, nested lambdas and local functions left out.
A throw statement or a throw expression hands the fault out.
So does a delegate or event invocation, whatever its arguments.
So does carrying the caught variable to a field, a property, an `out` or `ref` parameter, or a `return`.
Passing it to a delegate, or to a ring method whose parameter is itself carried out, carries it too.
The carried ring parameters are settled to a fixed point before any clause is judged.
A call through an interface or into another ring is a recording, never a way out.
A catch of `OperationCanceledException`, or of a type derived from it, is exempt by shape.
Every other clause is a `Swallowing` hit.

## Settings

`AuditFault.json` is copied by hand from `TAuditFaultSetting`, and the two never read each other.
`rings` names the projects under the binder source root whose files are walked.
`ceilings` holds one count per ring, exempt hits left out.
`exempt` holds `path:EnclosingMethod` rows whose hits count against no ceiling.
`console.top` sets how many rows of each ring the console shows unless `-Top` is given.
The `report` block names the report folder, the version file, the version key and the file prefix.

## Output

Every hit prints as `path:line EnclosingType.EnclosingMethod catch (ExceptionType) swallows the fault`.
A constructor is named after its type, and an accessor after its property.
A catch with no type shows as `Exception`.
The Result table has three gates: above ceiling, stale ceilings and stale exempt rows.
A ring above its ceiling fails, and a ceiling above its count is stale and fails too.
An exempt row that matches no hit fails, so a row never outlives its catch.
The exit code is 1 when any gate is above 0 and 0 otherwise.

## Reports

Every run writes `{directory}/AuditFault-{version}.md` and `{directory}/AuditFault-{version}.html`.
The Markdown report holds the counts, then every hit ring by ring, then the exempt hits and the gate lists.
The page fills `AuditFault.html` at its `__TITLE__` and `/*__DATA__*/` markers.
The helper writes the page data, so both PowerShell versions write the same bytes.
The page opens with `Start-Process` unless `-NoOpen` is given.

## Helper

The helper program is compiled once per text, binder, framework and SDK into the temp folder.
It targets `helper.framework`, the framework of the convention tests.
