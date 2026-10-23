# Test.ps1

## Why this copy differs

The header says the script is identical in every project.
Every project carries the same copy.
The generation stays, since no test is selected or run differently.

Full runs ended with "being used by another process" on the test output file.
They printed no failure summary and no exit code, and they left their `Test-<guid>` folders in the temp folder.
Some process still held the output file open for writing after its project finished.
That process was never identified.
The runs use `--no-build`, so a build server is unlikely.

## Read-TestOutput

`File.ReadAllLines` lets no other handle write, so a live writer made it throw and end the script.
`Read-TestOutput` opens the file with share mode `ReadWrite`, so the read succeeds while the writer stays.
No retry is needed.

## Output file names

Each run writes `<project>.<run>.out` and `<project>.<run>.err`.
A later `-Repeat` run never reopens a file that a leftover process of an earlier run still holds.

## Cleanup

Removing the output folder is tried five times, 500 ms apart.
A folder that stays is named in a notice instead of being left behind in silence.
The script's exit code does not change.

## Repeat

A failing run still finishes every project, and no later run starts.
The help text said execution stops at the first failure, and now says what the loop does.
