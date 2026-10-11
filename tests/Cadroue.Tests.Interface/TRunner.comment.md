# TRunner.cs
Hash: `1b1705db7071820a`

## `internal sealed record TRunnerWork(`

A test-side snapshot of one work item, so tests never hold the live production record.
The output flag reports whether the output file exists at the moment of the read.

## `internal sealed class TRunner : IDisposable`

Drives real LRunner instances against a controlled stand-in for FFmpeg.
Each worker spawns PowerShell running a script that reports progress the way FFmpeg does.
The depot root moves to a fresh temp folder, so no test touches the real workspace.
The info, error and FFmpeg trace seams are captured locally for timeout reports.
Verbose is forced on, so the FFmpeg stand-in output reaches that capture.

## `internal TRunner(int workerCount = 1)`

Every runner posts through one shared lock, so runner and test schedule changes never interleave.
The argument transform swaps the encode command for the script, keeping only the output path.

## `internal Guid TWorkAdd(string name, int steps = 40, int delayMilliseconds = 50)`

Writes a plan file that sets how many progress steps the script emits and how fast.
A sequence prefix keeps names unique and creation times ordered.
Throws when the schedule refuses the item, so a test never waits on work never admitted.

## `internal int TRunnerExecutionRead(Guid workId)`

Counts script starts from a file the script appends to, retrying briefly while it is held.

## `internal double TRunnerPausedRead(Guid workId, int milliseconds = TRunnerWaitMilliseconds)`

Returns the progress once it has held still for 400 milliseconds, the sign a pause took effect.

## `public void Dispose()`

Runners get a bounded wait, so a stuck child cannot hang the test run.
Reattaches the test trace seams and removes every scheduled item before releasing the index.
The previous depot root is restored, so later tests run unaffected.

## `private string TRunnerDiagnosticRead()`

Prefers the script's own error log, else the last four recorded trace messages.

## `private const string TRunnerScript = """`

Writes a partial output first, so the cleanup of partial outputs can be observed.
Exit code 65 marks a script failure, with the reason left in the error log.
