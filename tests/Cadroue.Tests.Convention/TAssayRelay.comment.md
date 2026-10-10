# TAssayRelay.cs
Hash: `c9685d15b2f10514`

## `public sealed class TAssayRelay`

Assays of the relay reading: which delegates and members count as a request.
Each gatekeeping assay tests a field in a branch that calls a delegate.
The hit appears only when the delegate is a relay.
The three control assays read the relay set through the puppeteering of a control type.
They run through `TAuditTruthWalker.TAuditRun` with the helpers of `TAssayTruth`.
The two-type assays hand a whole source, so the injected delegate crosses from one type to another.
A failing assay exposes a walker bug, never a source to fix.

## `public void AuditTruth_InjectedDelegate_ReportsGatekeeping()`

A gate delegate passed to another type's constructor and stored there is a relay when invoked.

## `public void AuditTruth_InjectedPlain_AllowsRequest()`

The same wiring with a delegate that reaches no gate makes no relay.
It only guards against an over-broad rule.
Its twin `InjectedDelegate` fails when the injected seam is no longer followed.

## `public void AuditTruth_DelegateLocal_ReportsGatekeeping()`

A local holding a gate delegate is a relay when invoked.

## `public void AuditTruth_PlainLocal_AllowsRequest()`

A local holding a delegate that reaches no gate makes no relay.
It only guards against an over-broad rule.
Its twin `DelegateLocal` fails when delegate locals are no longer traced.

## `public void AuditTruth_FieldAssigned_ReportsGatekeeping()`

A field assigned a gate delegate in a method is a relay when invoked.

## `public void AuditTruth_FieldInitializer_AllowsRequest()`

A field initializer holding a gate delegate is not a relay, since only assignments are read as wiring.

## `public void AuditTruth_RelayChain_ReportsGatekeeping()`

A delegate local initialized from another relay local is a relay too.

## `public void AuditTruth_PlainChain_AllowsRequest()`

A chain that starts from a delegate reaching no gate makes no relay.

## `public void AuditTruth_AssignedLocal_ReportsGatekeeping()`

A delegate local assigned a gate delegate after its declaration is a relay when invoked.

## `public void AuditTruth_AssignedPlain_AllowsRequest()`

A delegate local assigned a delegate that reaches no gate makes no relay.

## `public void AuditTruth_InterfaceEvent_ReportsGatekeeping()`

A gate delegate wired to an interface event makes that event a relay.
Raising the event that implements it is then a request.

## `public void AuditTruth_PlainEvent_AllowsRequest()`

The same interface event wired to a delegate that reaches no gate makes no relay.
It only guards against an over-broad rule.
Its twin `InterfaceEvent` fails when either half of the interface rule is switched off.

## `public void AuditTruth_ControlRequest_ReportsPuppeteering()`

A control type calling a gate directly and through a driver relay reports both members.
It follows the shape of the pre-fix `PMention.PMentionPieceRead`, which asked Conduct for a mention's unit and offset.
The relay type itself is no control, so its own request is not puppeteering.

## `public void AuditTruth_ControlRecord_AllowsRead()`

A control type reading a Conduct record property makes no request and reports nothing.
It only guards against an over-broad rule.
Its twin `ControlRequest` fails when the control rule is switched off.

## `public void AuditTruth_ControlSignal_AllowsRaise()`

A control raising its own event, which a driver handles with a gate call, reports nothing.
The surface signals and the driver requests, so the event is no puppet string.
It follows the shape of `PGloss.PGlossPickRaise` and the event members of `PGrasp` and `PMention`.
It fails when events join the relay set before the puppeteering snapshot is taken.

## `private static void TAssayRelayCheck(string driver, TViolation? expected)`

Holds the gatekeeping hits of a driver run beside the gate and a third source, the interface.
The interface sits in the Conduct folder, outside the shell.
So only the interface clause of the wiring rule can make its event a relay.
`TAssayTruth.TAssayTruthCheck` hands two sources only, so this helper adds the third.

## `private static void TAssayControlCheck(string driver, IReadOnlyList<TViolation> expected)`

Holds the puppeteering hits of a driver run beside the gate and a Conduct record.
The record sits in the Conduct folder, so reading its property crosses no gate.
