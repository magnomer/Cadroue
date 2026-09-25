# TAuditTruthShape.cs

## `internal static partial class TAuditTruthWalker`

The shape half of the truth walker: what the shell's own shape may not decide.
A control's state, a clock and a deaf handler are shape, not a user act or an engine fact.

## `private static void TAuditShapeScan(SyntaxNode root, List<TViolation> violations)`

Walks every node of a file for the shapes and reports each once per line and name.
An `if` or ternary whose condition reads a control and whose branch requests lets the control decide.
An event subscribed on a clock whose handler requests lets the clock decide.
A method taking the bulletin type it never reads shows from shell state on a signal it ignored.
An observer built on a lambda that ignores its bulletin is the same silence.

## `private static string? TAuditControlRead(ExpressionSyntax condition)`

The first control a condition reads a member of, or null.
A control is any expression whose type derives from a listed control base.
A member of a control that is itself logic is the path to the engine, not the control's state.

## `private static bool TAuditClockCheck(ExpressionSyntax clock)`

True when the subscribed expression is of one of the clock types.

## `private static bool TAuditLambdaCheck(LambdaExpressionSyntax lambda)`

True for a lambda handed to the observer type whose parameter is discarded or never read.

## `private static bool TAuditDriveCheck(ExpressionSyntax handler)`

True when a timer handler requests inline or names a relay.

## `private static string? TAuditDeafRead(MethodDeclarationSyntax handler)`

The name of a bulletin parameter the body never mentions, or null.
