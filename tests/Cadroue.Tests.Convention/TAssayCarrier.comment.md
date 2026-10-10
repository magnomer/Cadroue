# TAssayCarrier.cs
Hash: `44fe74f05ccf40bc`

## `public sealed class TAssayCarrier`

Assays of the misfiring scan on control reads that reach a condition indirectly.
A read may travel through a local, a parameter or a local function's parameter.
It may also sit in a type pattern on a value typed `object`, as a `sender` is.
Each hit-assay has a no-hit twin that differs by one thing.
They run through `TAuditTruthWalker.TAuditRun` with the helpers of `TAssayTruth`.
Body line k is driver line 9 + k.
`CAssay` and `QAssay` serve as the row types a type test pairs.
A failing assay exposes a walker bug, never a source to fix.

## `public void AuditTruth_LocalControl_ReportsMisfiring()`

A local initialized from a control read carries it into the `if`.
The hit names the local.

## `public void AuditTruth_LocalPlain_AllowsRequest()`

A local initialized from no control read carries nothing.

## `public void AuditTruth_ParameterControl_ReportsMisfiring()`

A parameter assigned a control read carries it into the `if`.

## `public void AuditTruth_ParameterPlain_AllowsRequest()`

A parameter assigned a constant carries nothing.

## `public void AuditTruth_LocalFunctionControl_ReportsMisfiring()`

A control read passed to a local function carries into its parameter.
The hit sits inside the local function.

## `public void AuditTruth_LocalFunctionPlain_AllowsRequest()`

A constant passed to the same local function carries nothing.

## `public void AuditTruth_SenderValue_ReportsMisfiring()`

A type pattern that narrows `object` to a control and tests its text reads the control.
The hit names the tested value.

## `public void AuditTruth_SenderType_AllowsRequest()`

The same type pattern testing only the type of `DataContext` is pairing.

## `public void AuditTruth_SenderCase_ReportsMisfiring()`

A switch case on `object` whose type pattern tests a control's text decides.

## `public void AuditTruth_SenderTypeCase_AllowsRequest()`

A switch case on `object` whose type pattern tests only types is pairing.

## `public void AuditTruth_ConstantValue_ReportsMisfiring()`

A constant value in a control's type pattern decides.

## `public void AuditTruth_ConstantType_AllowsRequest()`

A bare type name parses as a constant pattern, yet it is only a type test.

## `public void AuditTruth_EitherValue_ReportsMisfiring()`

An `or` of constant values decides.

## `public void AuditTruth_EitherType_AllowsRequest()`

An `or` of type names is a type test.

## `public void AuditTruth_NegatedValue_ReportsMisfiring()`

A `not` over a constant value decides.

## `public void AuditTruth_NegatedType_AllowsRequest()`

A `not` over a type name is a type test.

## `public void AuditTruth_GroupedValue_ReportsMisfiring()`

An `or` of constant values in parentheses decides.

## `public void AuditTruth_GroupedType_AllowsRequest()`

An `or` of type names in parentheses is a type test.
