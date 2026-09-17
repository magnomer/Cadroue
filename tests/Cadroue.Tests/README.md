# Cadroue.Tests

Test layout and policy for the behaviour-focused test suite.

## Interface boundary

Every production operation used by a test is relayed through a `T`-prefixed adapter or helper under `/Interface`. This includes pure value operations and production object construction; test bodies do not invoke methods or constructors from `src` directly.

An adapter may translate, invoke, observe, and clean up. It must not repair the behaviour under test: each operation transparently delegates to the production path, never reimplements production logic, fakes notifications, or returns success the production path did not produce.

`InterfaceBoundaryTests` enforces this boundary for future test changes.

## UI assemblies

`Cadroue.UIDeportment` holds no WPF reference and is testable through `/Interface` adapters like any logic assembly. `Cadroue.UIVeneer` is WPF and stays untested; the convention suite gates what each may contain (`TAuditGateSetting`).

Deportment tests live under `/Deportment`, one file per behaviour (`TInspectorGating`, `TStripTab`, `TOptionsPending`, and peers). Their adapters are `Interface/TInterfaceDeportment.cs` (`L`) and `Interface/TInterfaceSubwindow.cs` (`LS`). A Deportment owner gains a test when it gains state or a decision, not when it gains a getter.

## Naming rule

Name test files and classes after behaviour, not production type. Name test methods after observable behaviour (`SectionPastMediaDuration_IsRejected`), not the production method that ran.
