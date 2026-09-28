# TAuditTruthShape.cs

## `internal static partial class TAuditTruthWalker`

The shape half of the truth walker: what the driver's medium may not decide.
A control's state, console input, a clock and a deaf handler are shape.
None of them is a user act or an engine fact.
It also holds the guard rules for answers a driver branches on.

## `private static void TAuditShapeScan(SyntaxNode root, List<TViolation> violations)`

Walks every node of a file for the shapes and reports each once per line, kind and name.
An `if` or ternary whose condition reads a control or console input and whose branch requests lets the medium decide.
An `if` deciding a request on a dialog answer is a guard.
The gate asks the user through a port instead.
An `if`, ternary or switch deciding a request on an engine answer is a guard.
The gate owns that decision.
A clock drives a request through an event, a callback it is built with or a loop that waits.
A method or lambda taking the bulletin type it never reads shows from driver state on a signal it ignored.

## `private static string TAuditExcerptRead(SyntaxNode node)`

The first line of the node, trimmed, as the hit's name.

## `private static bool TAuditAskedCheck(ExpressionSyntax condition)`

True when the condition reads logic, calls logic or calls a driver member that reads logic.
A verdict wrapped in a driver method is still the engine's answer.

## `private static string? TAuditControlRead(ExpressionSyntax condition)`

The first control a condition reads a member of, or the first console input it calls, or null.
A control is any expression whose type derives from a listed control base.
A member of a control that is itself logic is the path to the engine, not the control's state.

## `private static bool TAuditConsoleCheck(InvocationExpressionSyntax call)`

True when the call reads console input.

## `private static string? TAuditDialogRead(ExpressionSyntax condition)`

The first call on a dialog type inside the condition, or null.

## `private static bool TAuditClockCheck(SyntaxNode clock)`

True when the expression, or the object a creation builds, is of one of the clock types.

## `private static bool TAuditDelayCheck(SyntaxNode loop)`

True when a loop waits on a delay member or on a clock.

## `private static bool TAuditLambdaCheck(LambdaExpressionSyntax lambda)`

True for a lambda whose first parameter is a bulletin that is discarded or never read.
The parameter's type decides, so the rule holds for every medium and every observer helper.

## `private static bool TAuditDriveCheck(SyntaxNode handler)`

True when a handler, a callback or a loop requests inline or names a relay.

## `private static string? TAuditDeafRead(MethodDeclarationSyntax handler)`

The name of a bulletin parameter the body never mentions, or null.
