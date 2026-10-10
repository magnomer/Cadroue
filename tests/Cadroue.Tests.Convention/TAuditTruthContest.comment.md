# TAuditTruthContest.cs
Hash: `dc0e43fe54edc327`

## `internal static class TAuditTruthContest`

Reports a field written both by the engine and by the shell, as Contesting.
`TAuditTruthWalker` sorts the writes, and this class only weighs them.

## `internal static void TAuditContestCheck(TAuditTruthField field, List<(SyntaxNode, ExpressionSyntax?, string)> sorted, List<TViolation> violations)`

Each sorted write carries its reference, its written value and its verdict.
The first engine write stands for the engine side.
Every plain write is a shell writer, kept in reference order.
A clear writes nothing and takes no side.
When both kinds of writer exist, `TAuditLookupWalker.TAuditLookupRead` may sort them again by lookup input.
A field with an engine writer and a shell writer is contesting.
The hit names the engine write by file when that write lies in another file.
The hit lands on the first shell write and lists every other shell write after it.
So a second shell writer can never hide behind the first.
