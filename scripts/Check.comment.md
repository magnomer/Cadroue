# Check.ps1

Runs the whole done gate in one call and prints one tally line.
It covers the build, the full `Test.ps1`, and the audits its `$audits` list names.

## Usage

```
check [-Grep <pattern>[,<pattern>...]] [-NoBuild] [-NoTest] [-NoAudit] [-Open] [-Verbose] [-Help]
```

`-Grep` searches `src` and `tests` `.cs` and `.xaml` files for leftover names.
Every grep hit counts as a failure.
`-NoBuild`, `-NoTest` and `-NoAudit` skip a stage for quick loops.
A run with any skip flag never counts as done.
`-Open` lets each audit that writes a page open it.
Without it, every audit that declares `-NoOpen` gets that flag, so a check run opens nothing.
`-Verbose` prints up to 200 lines of an audit's output when it fails a counter or is unparsed.
`-Help` prints the usage and exits without running anything.
`-?` prints only the PowerShell syntax line and also runs nothing.
`--help` binds to `-Grep` as a pattern and runs the whole gate.

## Output

The last line reads `OK` or `FAIL`, then one counter per stage.
The exit code is 0 on `OK` and 1 on `FAIL`.

## Resilience

Audit calls pass only optional `-NoOpen`, but changes to required parameters or output grammar can still break them.
It passes `-NoOpen` only to an audit that declares it.
It reads only the `Result` line of each audit's output and the table of rows below it.
Every `OK` or `FAIL` row adds to the audit's sum, and any sum above zero fails the run.
A `WARN` row adds to a separate warning sum, shown as `WARN n` after the audit's counter.
A warning never fails the run, since each audit decides itself when a warning becomes `FAIL`.
An output without a `Result` line or with an unreadable row shows `UNPARSED` and fails the run.
A missing script, a thrown error, or a non-zero exit also fails the run.
An audit exits 1 when it finds violations, which shows as counters and not as a script error.
The build takes the one `.slnx` or `.sln` file at the root, so no project name is written in.
The build is judged by the exit code and by warning or error codes.
Tests are judged by the exit code and the pass or fail summary lines.
A new audit needs one name added to `$audits`, and a renamed counter needs no edit.
Korean match words are built from character codes to keep the source ASCII.
The console reads and writes UTF-8, so native output and grep hits keep their characters.
