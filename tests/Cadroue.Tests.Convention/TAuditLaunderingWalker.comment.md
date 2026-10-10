# TAuditLaunderingWalker.cs
Hash: `a1fdc8f98a04be4b`

## `internal static class TAuditLaunderingWalker`

Follows an engine value into a local or a driver member and reports the line that computes over it.
The moonlighting rule sees only a line that names logic, so a value copied into a local escaped it.
A control's input and the console's input are followed the same way, since a driver passes them raw.

## `private const string TAuditLogicColour = "logic value";`

The colour a taint carries when it came from logic.

## `private const string TAuditTextColour = "control input";`

The colour a taint carries when it came from an input member of a control or a console read.

## `private static readonly AsyncLocal<IReadOnlySet<ISymbol>?> TAuditReaderNames = new();`

The shell members that read logic or request, read by the truth walker.
It is local to the run's own flow, like the assay compilation in `TAuditBinder`.
So an assay walking beside the tracked walk never swaps the tracked readers for its own.
Outside a run it is null.

## `public static IReadOnlyList<TViolation> TAuditRun(IReadOnlyList<string> sourcePaths, IReadOnlySet<ISymbol> readers)`

Walks every driver file and scans each member on its own, since a taint lives inside one member.
The caller's reader members are held for the walk and cleared when the walk returns, even on a failure.

## `private static void TAuditMemberScan(MemberDeclarationSyntax member, List<TViolation> violations)`

Reads the member's taints, then looks at every operator, query verb and condition for one.
A line that already names logic is the moonlighting rule's and is skipped here.
A comparison with a `true` or `false` literal only coerces a value, so it is no operator sink.
A hit is reported once per line and reason, with the first line of the node as its name.

## `private static Dictionary<ISymbol, string> TAuditTaintRead(MemberDeclarationSyntax member)`

The locals of a member, by symbol, and the colour each carries.
A parameter of a type from below Conduct is tainted from the start.
An initialiser, an assignment, a loop variable and a pattern designation each carry the colour of their source.
A local built from a tainted local is followed, so the hop count is not limited to one.

## `private static void TAuditColourAdd(SyntaxNode node, string colour, Dictionary<ISymbol, string> tainted)`

Colours the symbol a declaration or name resolves to.

## `private static string? TAuditColourRead(ExpressionSyntax value, Dictionary<ISymbol, string> tainted)`

The colour an expression carries, or null when it is clean.

## `private static (string TViolationName, string TViolationColour)`

The first tainted name inside a node and its colour.
A tainted local, an engine symbol, a reader member, a control input member and a console read each count.
A control input member counts only when `TAuditInputCheck` accepts its receiver.
A static Conduct field or property is a logic value too, as a driver method returning it is.
An enum member is not, since comparing to one only dispatches on a verdict.
A name inside `nameof` reads no value and is skipped.

## `private static bool TAuditInputCheck(ExpressionSyntax receiver)`

True when the receiver of an input member is a control the user can change.
Its type must derive from `TAuditTruthSetting.TAuditInputBase`, resolved through the semantic model.
A `TextBlock`, a `Run` or any other display element holds only what the driver wrote.
An error type, a `dynamic` receiver or a missing type keeps the colour, so no unresolved read escapes.
A namespace receiver, as in `System.Text`, reads no value and is false.

## `private static bool TAuditConditionCheck(ExpressionSyntax condition)`

False for a null check, a type test and a bare verdict, which decide nothing about a value.
An `&&` or `||` chain counts as presence only when every operand is a null check or type test.

## `private static bool TAuditVerdictCheck(ExpressionSyntax condition)`

True when the condition is only a logic call or a reader call, whose answer is audited elsewhere.

## `private static int TAuditLineRead(SyntaxNode node)`

The one-based line of the node.
