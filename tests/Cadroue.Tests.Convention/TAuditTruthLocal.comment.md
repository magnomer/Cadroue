# TAuditTruthLocal.cs

## `internal static partial class TAuditTruthWalker`

The local half of the truth walker: what a member may not do with an engine answer it briefly holds.
A Conduct type's member is an answer too, since a gate's verdict may not decide the next request.

## `private static void TAuditToggleCheck(`

A field written before and again after a request in one member is a guard the shell runs itself.
Each write must sit in a block that also holds the request, so both run around it.
Writes in branches the request never passes through do not toggle.
Only the write after the request is read past a `catch` or `finally`.
A `catch` before a later request usually leaves the member, so it does not open a hold.
A call to a method writing the field counts as a write, so a hold split into relays still toggles.
A fill of a collection is a refresh rather than a hold and does not count.
The hit lands on the last write and names the relay it went through.

## `private static BlockSyntax? TAuditBlockRead(SyntaxNode write)`

The innermost block around a write, read past a `catch` or `finally` to the block holding its `try`.
A write in a `catch` or `finally` runs after the request its `try` makes.

## `private static HashSet<ISymbol> TAuditWriterRead(TAuditTruthField field, IReadOnlyList<TypeDeclarationSyntax> type)`

The methods of the class that write this field and request nothing themselves.
A method that requests is a relay and is audited on its own.

## `private static void TAuditBaseCheck(IReadOnlyList<TypeDeclarationSyntax> type, List<TViolation> violations)`

A driver type deriving from or implementing a type from below Conduct holds logic by inheritance.
Implementing a Conduct port is what a driver is for, so it is no hit.

## `private static IEnumerable<MemberDeclarationSyntax> TAuditScopeRead(IReadOnlyList<TypeDeclarationSyntax> type)`

Every member of a class and of the types nested in it, field declarations included.
A lambda in a field initializer is scanned like a method body, so it cannot hide an answer.

## `private static void TAuditLocalScan(IReadOnlyList<TypeDeclarationSyntax> type, List<TViolation> violations)`

Locals initialised from an engine answer are followed to the sinks a field would be.
A member read off the answer is followed as the answer itself, so an id lifted from a draft counts.
An answer carried into the next request, or deciding one, is a value the engine should have kept.
A hit is reported once per line, local and kind, and named by member and local.

## `private static HashSet<ISymbol> TAuditAnsweredRead(MemberDeclarationSyntax scope)`

The locals a member fills from an engine answer, by whatever syntax.
An initialiser, an assignment, a deconstruction, a loop variable, a pattern, an `out` and a lambda parameter all count.

## `private static void TAuditDesignationAdd(SyntaxNode node, HashSet<ISymbol> answered)`

Every name a designation or a tuple on the left side introduces.

## `private static string TAuditMemberRead(MemberDeclarationSyntax scope)`

The member's own name, for the hit.

## `private static bool TAuditAnswerCheck(ExpressionSyntax value)`

True when the initialiser requests logic, calls a relay, or reads a logic member.
