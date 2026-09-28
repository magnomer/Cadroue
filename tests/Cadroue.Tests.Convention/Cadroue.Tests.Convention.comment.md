# Cadroue.Tests.Convention.csproj

Builds tests that inspect source structure and enforce repository conventions.

## Project ring

Uses xUnit and Roslyn without referencing runtime projects.
The tests inspect project files and source text rather than invoking application assemblies.

## Counterpart scripts

Each audit here has a counterpart script in `scripts/`, listed in `scripts/principles.md`.
A test and its counterpart never depend on each other, so either still tells the truth when the other breaks.
A test never reads a script or its configuration to learn a rule or a setting.
Each test carries its own settings, copied by hand, in its `TAudit*Setting.cs` file or ledger.
Both sides still reach the same verdict, count the same hits and word each hit the same way.
A rule, threshold, ceiling or ledger row changes on both sides in the same change.
The `Microsoft.CodeAnalysis.CSharp` version here is the one every script helper pins.
