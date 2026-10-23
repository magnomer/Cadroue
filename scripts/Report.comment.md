# Report.ps1
Hash: `3221dc5c085d26ed`

Writes every analysis document afresh in one call, from the steps `Report.json` lists.
The pages land in the folder `report.directory` names in `Report.json`.

## Usage

```
report [-ConfigPath <path>] [-Keep] [-NoOpen] [-Help]
```

`-Keep` skips the deletion of older pages.
`-NoOpen` writes every page without opening any.

## Run

First the older pages of every step are deleted.
A step owns the files named after its script and a dash.
Its `clear` list adds prefixes.
Any other file stays, since the folder holds every script family's reports.
Subfolders are never touched.

Then each script runs in order, with `-NoOpen` and `-NoPause` where it declares them.
Its console is captured, not shown, so the terminal holds only progress.
A script's own progress lines, those starting with `[n/m]`, show indented below its step.
That is how a script that runs many parts shows each one as it finishes.
A missing or throwing script is recorded as failed, and the run continues.

## Steps

`Report.json` lists the steps in order.
A `script` step runs the one named script.
A `family` step runs every script whose header names that family, in ordinal order.

A family step finds its scripts by the header line `# <NAME> - <FAMILY> GENERATION <n>.`.
A new script of a listed family therefore joins the report without a configuration change.

## Result

Each script ends its progress line with its status, the pages it left and its duration.
A failing script also shows the last lines of its captured console.
The verdict line follows, and the run exits 1 when any script fails.

## Open

`open` in `Report.json` names the script whose newest page opens at the end.
The newest `.html` page whose name starts with that script name and a dash is the one that opens, unless `-NoOpen` is given.
