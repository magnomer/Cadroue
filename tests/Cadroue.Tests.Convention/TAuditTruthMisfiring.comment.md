# TAuditTruthMisfiring.cs
Hash: `3ed435cc44badaeb`

## `internal static partial class TAuditTruthWalker`

The misfiring half of the truth walker, covering what the driver's medium may not decide.
`TAuditCarriedNames` caches each member's carried names, keyed by the member node.
A control's state, console input, a clock and a deaf handler count as misfiring.
None of them is a user act or an engine fact.
It also holds the gatekeeping rules for answers a driver branches on.
Line numbers and name matching come from `TAuditTruthReference`.

## `private static void TAuditMisfiringScan(SyntaxNode root, List<TViolation> violations)`

Walks every node of a file for misfiring and reports each once per line, kind and name.
An `if` or ternary whose condition reads a control or console input and whose branch requests lets the medium decide.
A switch case or arm deciding on a control read is the same misfiring, when any section or arm requests.
Every form exempts the same reads, a presence test or an `is` test of type tests alone.
So the verdict never depends on the syntax the author picked.
An `if` that only hears whose event it is decides nothing, so a focus guard is no hit.
An `if` deciding a request on a dialog answer is gatekeeping.
The gate asks the user through a port instead.
An `if`, ternary or switch deciding a request on an engine answer is gatekeeping.
The gate owns that decision.
A clock drives a request through an event, a callback it is built with or a loop that waits.
A method or lambda taking a bulletin it never reads is misfiring, since it acts on a signal it ignored.

## `private static string TAuditExcerptRead(SyntaxNode node)`

The first line of the node, trimmed, as the hit's name.

## `private static bool TAuditAskedCheck(ExpressionSyntax condition)`

True when the condition reads logic, calls logic or calls a driver member that reads logic.
A verdict wrapped in a driver method is still the engine's answer.
A name inside `nameof` is not asked, since it reads no value.

## `private static string? TAuditControlRead(ExpressionSyntax condition)`

The first control a condition reads a member of, or the first console input it calls, or null.
A control is any expression whose type derives from a listed control base.
A member of a control that is itself logic is the path to the engine, not the control's state.
A local or parameter carrying a control read counts as that read, so a carrier hides nothing.
Last comes an `is` test on any value whose type pattern names a control and tests its state.
So `sender is TextBox { Text: "a" }` reads a control although `sender` is typed `object`.

## `private static string? TAuditControlRead(ExpressionSyntax condition, HashSet<ISymbol> carried)`

The same read against a given set of carried names.
The carrier set uses it while the set is still growing.

## `private static HashSet<ISymbol> TAuditCarriedRead(SyntaxNode scope)`

The locals and parameters of one member that carry a control read.
A declaration or assignment whose value reads a control carries it.
An argument that reads a control carries it into the local function's parameter.
The nodes are read in order, so a carrier copied from a carrier carries too.
A designation in a pattern never carries, since a type test pairs a row and reads no state.
A call that takes a control is not a read of it, so its result carries nothing.

## `private static bool TAuditShapeCheck(SyntaxNode node)`

True for a property pattern on a control type that tests more than type.
It is the pattern form of a control read when the tested value is not typed as a control.

## `private static bool TAuditHearingCheck(IfStatementSyntax branch)`

True when an `if` without `else` only drops an event the user did not cause.
Such an `if` is not misfiring.
Dropping an event the user did not cause picks no gate and decides no data.
An early exit qualifies when its body is a bare `return` and its condition negates a focus guard.
Any other `if` qualifies when its condition is a focus guard and no request follows it in the member.
So the false path drops the event, and no other arm calls a gate.
An `else` always keeps the hit, since focus would then choose between two paths.

## `private static bool TAuditFocusCheck(ExpressionSyntax condition)`

True when every control read in an `&&` chain asks only that the control has focus.
A focus read is a listed focus member read on a control.
In a control's type pattern, it is a focus member tested `true`.
Other parts of such a pattern must be type tests alone, so the row is only paired.
Parts that read no control may test anything, since they are not the medium's state.
A negated focus read, an `||` or a carried focus value keeps the hit.

## `private static string? TAuditCaseRead(ExpressionSyntax governing, SyntaxNode label)`

The control a switch label decides on, or null.
A `when` guard that reads a control decides, unless it is a presence test or type tests alone.
Otherwise a control read in the governing expression decides when the label tests its value.
A null label, a discard and a label of type tests alone test no value.
A label whose pattern tests a control's state through a type pattern names the governing expression.

## `private static bool TAuditPairCheck(PatternSyntax pattern)`

True for a pattern of type tests alone, nested property patterns included.
A bare type name parses as a constant pattern, so a constant naming a type is a type test.
`or`, `and`, `not` and parentheses keep a pattern of type tests alone.
Picking a gate by the row's type pairs the row to its record, which a driver may do.
A constant or any other value test in the pattern decides.

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
