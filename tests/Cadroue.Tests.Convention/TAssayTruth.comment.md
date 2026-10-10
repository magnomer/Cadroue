# TAssayTruth.cs
Hash: `a84ecd2050c4a8c7`

## `public sealed class TAssayTruth`

Assays of the driver walker: small hand-written sources with an expected hit or no hit.
Each assay binds through `TAuditBinder` and runs `TAuditTruthWalker.TAuditRun`, the walker's real entry.
Assays check the walker, not the tree, so they gate no tree result and touch no ledger.
They guard rules the tracked tree never reaches, which the tree audit alone would let break unseen.
A failing assay exposes a walker bug, never a source to fix.
`TAuditTruth` holds the facts over the tree.
Its helpers and stand-in gate are internal, so sibling truth assays such as `TAssaySink` and `TAssayRelay` reuse them.

## `internal const string TAssayDriverPath = "src/Cadroue.UIDeportment/Assay/QAssay.cs";`

The virtual path of the assayed driver, under a driver root so the walker reads it.

## `internal const string TAssayGatePath = "src/Cadroue.Conduct/Assay/CAssay.cs";`

The virtual path of the stand-in gate, under Conduct so its type takes the Conduct side.

## `internal const string TAssayGateText = """`

A Conduct type with three instance methods, so each call to any is a send.
`CAssayApply` returns nothing and `CAssayRead` returns an int, so a ternary or an assignment can take its answer.
`CAssayRemove` takes a nullable id, so a Zeroing assay can send a literal zero or a null.

## `private const string TAssayRemoveText = """`

A driver helper that forwards its id to `CAssayRemove`, so its id position is hot.
It is four member lines, so body line k of a driver holding it is line 14 + k.

## `private const string TAssayItemText = """`

A driver type appended after `QAssay`, whose constructor stores its id into a getter-only property.
Appended text leaves every line of `QAssay` where it was.
The constructor is hot only when the stored property is read inside a hot argument.

## `public void AuditTruth_NestedReturn_ReportsMisfiring()`

A `return` under a nested `if` in the branch may not run, so the branch does not leave.

## `public void AuditTruth_BareReturn_AllowsSecondSend()`

A bare `return` in a branch without `else` leaves it, so the later send is exclusive.

## `public void AuditTruth_LambdaReturn_ReportsMisfiring()`

A `return` inside a lambda leaves the lambda, not the branch.

## `public void AuditTruth_AnonymousReturn_ReportsMisfiring()`

A `return` inside an anonymous method leaves the method, not the branch.

## `public void AuditTruth_LocalThrow_ReportsMisfiring()`

A `throw` inside a local function runs only if the function is called, so the branch does not leave.

## `public void AuditTruth_BareThrow_AllowsSecondSend()`

A bare `throw` statement in a branch without `else` leaves it.

## `public void AuditTruth_ThrowExpression_ReportsMisfiring()`

A `throw` expression such as `text ?? throw` is not a jump, so the branch does not leave.

## `public void AuditTruth_InnerLoopBreak_ReportsMisfiring()`

A `break` whose loop lies inside the branch ends the loop, not the branch.

## `public void AuditTruth_InnerLoopContinue_ReportsMisfiring()`

A `continue` whose loop lies inside the branch stays in the branch.

## `public void AuditTruth_InnerSwitchBreak_ReportsMisfiring()`

A `break` whose `switch` lies inside the branch ends the switch, not the branch.

## `public void AuditTruth_OuterLoopBreak_AllowsSecondSend()`

A `break` directly in a branch inside a loop leaves the branch, so the later send is exclusive.

## `public void AuditTruth_OuterLoopContinue_AllowsSecondSend()`

A `continue` directly in a branch inside a loop leaves the branch too.

## `public void AuditTruth_ZeroForward_ReportsZeroing()`

A literal zero passed to a driver helper whose id reaches the gate is Zeroing.
The hit is named after the helper and lands on the literal's line.

## `public void AuditTruth_NullForward_AllowsNull()`

A `null` at the same hot position is no Zeroing, since absence is spelled in the type.

## `public void AuditTruth_ZeroConstructor_ReportsZeroing()`

A literal zero handed to a driver constructor is Zeroing when the stored id later reaches the gate.
It needs the wider Zeroing map, since the hot map holds no constructor.

## `public void AuditTruth_NullConstructor_AllowsNull()`

A `null` handed to the same constructor is no Zeroing.

## `public void AuditTruth_ZeroCompare_ReportsZeroing()`

A stored id compared with zero is Zeroing when that id is read inside a hot argument.
The hit is named after the compared member.

## `public void AuditTruth_NullCompare_AllowsNull()`

The same id compared with `null` is no Zeroing.

## `public void AuditTruth_ZeroHelper_AllowsZero()`

A literal zero passed to a driver helper that never reaches a gate is no Zeroing.
The helper's position is not hot, so a medium value such as a width stays free.

## `internal static void TAssayTruthCheck(string driver, string kind, TViolation? expected)`

Holds the hits of one kind against the expected hit, or against none when `expected` is null.
The expected hit names the driver path, the line, the member, the kind and the reason.
Hits of other kinds are left to their own assays.

## `internal static IReadOnlyList<TViolation> TAssayTruthRun(string driver)`

Walks a whole driver source beside the stand-in gate and returns every hit.
Hit paths are made repo-relative, as the tree audit makes them.

## `internal static string TAssayDriverFormat(string body)`

Wraps a method body in the assayed driver as the body of `QAssayRun`.
The body starts on line 10, so its line k is line 9 + k of the driver.
Every Misfiring case here sends first on body line 3, so the reason names line 12.

## `internal static string TAssayDriverFormat(string members, string body)`

Wraps the same method body in a driver that also holds the given members, such as a field.
The members start on line 8 and the method follows after one blank line.
So with m member lines, body line k is driver line 10 + m + k.
