# TAuditTreatWalker.cs

## `internal static class TAuditTreatWalker`

Finds the deportment lines that compute over engine data, since the deportment must not decide the data.

## `public static IReadOnlyList<TViolation> TAuditRun(IReadOnlyList<string> sourcePaths)`

Scans every UI source for look-alike glyphs and every deportment source for treatment.

## `private static void TAuditTreatScan(SyntaxNode root, List<TViolation> violations)`

One hit per line and reason where a logic value is an operand, query source, cast, `typeof`, attribute or condition.
Null checks, coalescing and the logical connectives are not treatment.
Nor is a setter's equality between its value and the stored one.

## `private static bool TAuditConditionCheck(ExpressionSyntax condition)`

True when the condition treats a logic value, and false for a null check or a bare verdict.
A setter comparing its value with the stored one is false too.

## `private static bool TAuditSetterCheck(BinaryExpressionSyntax binary)`

True for an equality inside a set or init accessor with the implicit `value` on one side.
That comparison only skips a change that changes nothing, so it treats no logic.

## `private static bool TAuditVerdictCheck(ExpressionSyntax condition)`

True when the unwrapped condition is one logic call, member or name.
Asking the engine a question is not treating its data.

## `private static void TAuditGlyphScan(SyntaxNode root, List<TViolation> violations)`

One hit per identifier holding a character outside ASCII.
A look-alike glyph would let a name pass every prefix rule while reading as another.

## `private static int TAuditLineRead(SyntaxNode node)`

The one-based line of the node.
