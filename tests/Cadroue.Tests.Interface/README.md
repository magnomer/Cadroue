# Cadroue.Tests.Interface

Test layout and policy for the behaviour-focused test suite.

## Projects

- `Cadroue.Tests.Interface` holds the relays, fakes and test workspace: the facade every test drives production through.
- `Cadroue.Tests.Engine` tests Core, Application, Infrastructure, Media and ShellEngine, and references only this project.
- `Cadroue.Tests.Windows` tests the Deportment, and references this project and `Cadroue.UIDeportment`.
- `Cadroue.Tests.Convention` audits the source tree as text, and references no project.

## Interface boundary

Every production operation used by a test is relayed through a `T`-prefixed adapter or helper in this project, or under `Interface/` in `Cadroue.Tests.Windows` for the Deportment. This includes pure value operations and production object construction; test bodies do not invoke methods or constructors from `src` directly.

An adapter may translate, invoke, observe, and clean up. It must not repair the behaviour under test: each operation transparently delegates to the production path, never reimplements production logic, fakes notifications, or returns success the production path did not produce.

`TInterfaceBoundary` enforces this boundary for future test changes. Tests inside this project sit beside the relays and are exempt.

## UI assemblies

`Cadroue.UIDeportment` holds no WPF reference and is testable through adapters like any logic assembly. `Cadroue.UIVeneer` is WPF and stays untested; the convention suite gates what each may contain (`TAuditGateSetting`).

Deportment tests live in `Cadroue.Tests.Windows` under `/Deportment`, one file per behaviour (`TInspectorGating`, `TStripTab`, `TOptionsPending`, and peers). Their adapters are `Interface/TInterfaceDeportment.cs` (`L`) and `Interface/TInterfaceSubwindow.cs` (`LS`) in that project. A Deportment owner gains a test when it gains state or a decision, not when it gains a getter.

## Naming rule

Name test files and classes after behaviour, not production type. Name test methods after observable behaviour (`SectionPastMediaDuration_IsRejected`), not the production method that ran.
