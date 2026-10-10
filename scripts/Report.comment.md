# Report.ps1
Hash: `3221dc5c085d26ed`

Writes every analysis document afresh in one call: the audits, the method map and the statistics.
The pages land in `docs-analysis/`, the folder uploaded to GitHub with each version.
It is tracked, kept visible by its `!/scripts/Report.ps1` line in `.gitignore`.

## Usage

```
report [-ConfigPath <path>] [-Keep] [-NoOpen] [-Help]
```

`-Keep` skips the deletion of older pages.
`-NoOpen` writes every page without opening any.

## Run

First the older pages of every step are deleted.
A step owns the files named after its script and a dash.
Its `clear` list adds prefixes, so the audit step owns every `Audit` page.
Any other file stays, since the folder holds every script family's reports.
Subfolders such as `records` are never touched.

Then each script runs in order, with `-NoOpen` and `-NoPause` where it declares them.
Its console is captured, not shown, so the terminal holds only progress.
A script's own progress lines, those starting with `[n/m]`, show indented below its step.
That is how each audit of `Audit.ps1` shows as it finishes.
A missing or throwing script is recorded as failed, and the run continues.

## Steps

`Report.json` lists the steps in order.

| Step | Scripts |
|---|---|
| `script: Audit` | `Audit.ps1`, which runs every audit itself. |
| `script: MapMethod` | `MapMethod.ps1`. |
| `family: STATS` | Every script whose header names the Stats family, in ordinal order. |

A family step finds its scripts by the header line `# <NAME> - <FAMILY> GENERATION <n>.`.
A new Stats script therefore joins the report without a configuration change.

## Result

Each script ends its progress line with its status, the pages it left and its duration.
A failing script also shows the last lines of its captured console.
The verdict line follows, and the run exits 1 when any script fails.

## Open

`open` in `Report.json` names the script whose newest page opens at the end.
It is `Audit`, so the summary page `Audit-<version>.html` opens.
