# Cadroue.Tests

Test layout and policy for the test-restructure jobs. Each later job converts one feature and is independent of the others; each deletes its own source `L*Tests.cs` file after moving its tests.

## Two-tier policy

- **Tier A — needs a `T`-prefixed adapter under `/Interface`.** Any test whose path touches file I/O, the OS, process-global mutable static state, or a static seam. The adapter owns setup/teardown and hides production types.
- **Tier B — direct call, reorganise only.** Pure value/math functions (no state, no I/O) and inspection of an external contract file. These keep calling `Cadroue.*` directly; only the file/class/method names change to behaviour names.

## Adapter rule (Tier A only)

An adapter may translate, invoke, observe, and clean up. It must not repair the behaviour under test: call at most one production entry point per operation; never manually initialise state, fake notifications, or return success the production path did not produce.

## Naming rule

Name test files and classes after behaviour, not production type. Name test methods after observable behaviour (`SectionPastMediaDuration_IsRejected`), not the production method that ran.
