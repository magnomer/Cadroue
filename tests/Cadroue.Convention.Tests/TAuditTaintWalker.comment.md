# TAuditTaintWalker.cs

## `internal static class TAuditTaintWalker`

Follows a logic value into a local or a shell member and reports the line that computes over it.
The treat rule sees only a line that names logic, so a value copied into a local escaped it.
A control's input is followed the same way, since the shell passes it raw and never shapes it.

## `private const string TAuditLogicColour = "logic value";`

The colour a taint carries when it came from logic.

## `private const string TAuditTextColour = "control input";`

The colour a taint carries when it came from an input member of a control.

## `private static IReadOnlySet<ISymbol> TAuditReaderNames`

The shell members that read logic or request, read by the truth walker.

## `public static IReadOnlyList<TViolation> TAuditRun(`

Compiles every file and scans each walked member on its own, since a taint lives inside one member.

## `private static void TAuditMemberScan(MemberDeclarationSyntax member, List<TViolation> violations)`

Reads the member's taints, then looks at every operator, query verb and condition for one.
A line that already names logic is the treat rule's and is skipped here.
A hit is reported once per line and reason, with the first line of the node as its name.

## `private static Dictionary<ISymbol, string> TAuditTaintRead(MemberDeclarationSyntax member)`

The locals of a member, by symbol, and the colour each carries.
A parameter of logic type is tainted from the start.
An initialiser, an assignment, a loop variable and a pattern designation each carry the colour of their source.
A local built from a tainted local is followed, so the hop count is not limited to one.

## `private static void TAuditColourAdd(SyntaxNode node, string colour, Dictionary<ISymbol, string> tainted)`

Colours the symbol a declaration or name resolves to.

## `private static string? TAuditColourRead(ExpressionSyntax value, Dictionary<ISymbol, string> tainted)`

The colour an expression carries, or null when it is clean.

## `private static (string TViolationName, string TViolationColour)? TAuditTaintFind(`

The first tainted name inside a node and its colour.
A tainted local, a logic symbol, a reader member and an input member of a control-typed expression each count.

## `private static bool TAuditConditionCheck(ExpressionSyntax condition)`

False for a null check, a type test and a bare verdict, which decide nothing about a value.

## `private static bool TAuditVerdictCheck(ExpressionSyntax condition)`

True when the condition is only a logic call or a reader call, whose answer is audited elsewhere.

## `private static int TAuditLineRead(SyntaxNode node)`

The one-based line of the node.
