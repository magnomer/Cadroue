# TracePerformance.ps1
Hash: `a9ac71f6ed6b0d52`

Times every method of the target project and ranks them from the slowest to the fastest.
The target is the project the `target` setting names by default.
It only measures and reports.
It gates nothing and stays out of `Check.ps1`, so it has no convention test counterpart.

## Usage

```
traceperformance [-Project <name> ...] [-Filter <expression>] [-Repeat <n>] [-Top <n>] [-Configuration <name>]
traceperformance -Help
```

## Examples

```
traceperformance
traceperformance -Filter "FullyQualifiedName~<Class>" -Repeat 3
traceperformance -Project <TestProject> -Top 100
```

## Methods

No method list is kept anywhere.
The built target assembly is the list.
The hook takes every constructor and method each type of the assembly declares.

- Compiler-made types and members are left out, since they are not written logic.
  Generated bodies are not patched separately.
  Their work contributes only while an enclosing timed call or its returned Task remains active.
- Abstract members, interfaces and delegate types are left out, since they hold no code.
- A member with no body, such as an external one, is listed as skipped.
- A generic method or a member of a generic type is listed as skipped.
  The patching library cannot time an open generic definition.
- A member the patching fails on is listed as skipped with the exception name.

## Workload

The workload is every test project under the tests folder whose project references reach the target.
The references are followed transitively, so a test project reaching the target through another project counts too.
Discovery skips Windows-targeted projects on non-Windows hosts, but explicit `-Project` selections bypass that filter.
`-Project` replaces the discovered list, and `-Filter` narrows the tests inside each project.

## Timing

The hook patches matching assemblies from load notifications and scans assemblies already loaded.
A process patches the target once, at the first matching assembly it meets.
A call that runs before the patch lands goes untimed.
The runtime finds the hook through `DOTNET_STARTUP_HOOKS`, set for the measured runs only.
Every managed process those runs start inherits it.
The hook source lives inside the script, so the name audit never reads tooling code.
The script writes it to the `hook` folder under the output folder and builds it there.
That folder carries empty `Directory.Build` files, so the repository's build settings stay out of it.

- Each call is timed by a stopwatch from entry to return, including a call that throws.
- An incomplete returned Task is counted by its completion continuation.
  Already-completed Tasks use entry-to-return elapsed time.
- Self time leaves out the time spent in other timed methods on the same thread.
  For Task-returning methods, self time ends when the method returns its Task, not when that Task completes.
- Pending Tasks are excluded from call count, inclusive total and maximum, but their synchronous self time remains.
  A warning reports the pending count.

## Output

Each run writes one report and one stamped folder of raw timings.

| File | Content |
|---|---|
| `TracePerformance-<stamp>.md` | Every section of the console with every row, in the report folder. |
| `probe/` | One raw file per process that attaches to the target assembly, beneath the stamped output folder. |

The console uses grouped table headers, numeric columns and section titles.
It has no `Result` section, since nothing is gated.

| Section | Rows |
|---|---|
| Time by folder | Methods, reached methods, calls and self time per source folder of the target, most self time first. |
| Methods from the slowest to the fastest | Every reached method by mean time per call. |
| Not reached | Methods with no completed recorded calls, by folder and name. |
| Skipped | Methods the hook could not time, with the reason. |

Folder labels use the first source declaration matching the outermost type's simple name, so duplicate names can be ambiguous.
An overloaded method shows its parameter types.
`-Top` cuts each console list, and the report file keeps every row.

## Limits

- Instrumentation adds overhead, so small measured durations are not uninstrumented execution costs.
- Test-runner settings can permit parallel tests, so measured calls can include shared-resource waits.
  Workload projects and repeats run sequentially.
- A method runs only as often as the tests call it, so a rare call rests on few samples.
  `-Repeat` runs the tests again and adds their calls.
- An iterator method is timed only while it creates its enumerator.

## Settings

`TracePerformance.json` holds every project-specific value.

| Key | Meaning |
|---|---|
| `generation` | Number printed in the console heading. |
| `configuration` | Build configuration of the measured code. |
| `output` | Folder the hook and the stamped raw timing folders go to. |
| `report.directory` | Folder the report goes to, shared with every script's report. |
| `target` | Project whose methods are timed. |
| `tests` | Folder whose test projects are searched for the workload. |
| `harmony` | Version of the `Lib.Harmony` package the hook builds with. |
| `repeat` | Default of `-Repeat`. |
| `top` | Default of `-Top`. |
