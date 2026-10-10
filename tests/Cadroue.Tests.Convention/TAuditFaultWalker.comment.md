# TAuditFaultWalker.cs
Hash: `a9c101c8ddd998bc`

## `internal static class TAuditFaultWalker`

Finds the catch clauses below Conduct that keep a fault from ever reaching the user.
A clause hands its fault out by throwing, by carrying its variable out, or by invoking a delegate or event.
Recording the fault through an interface or into another ring is not handing it out.
Every other clause is a `Swallowing` hit, unless it catches a cancellation.
`AuditFault.ps1` applies the same rule from its own helper, and neither reads the other.

## `public const string TAuditFaultKind = "Swallowing";`

The one kind this audit reports.

## `private const string TAuditCancelName = "System.OperationCanceledException";`

The cancellation type whose catch, or the catch of a type derived from it, is exempt by shape.
A cancelled operation is an answer, not a fault.

## `public static IReadOnlyList<TViolation> TAuditFaultScan()`

Walks every catch clause in the ring sources of the shared binder.
Settles the carried parameters first, so a fault passed to a ring helper is judged by what the helper does.
Returns one row per swallowing clause, in path and line order, exempt rows included.

## `public static TViolation? TAuditFaultRead(SemanticModel model, CatchClauseSyntax clause, IReadOnlySet<ISymbol> carried)`

Judges one clause and returns its hit, or null when the clause hands its fault out.
It reads nothing but its arguments, so the same clause always gets the same verdict.
A throw, a delegate call or an event raise in the block hands the fault out.
Nested lambdas and local functions are skipped, since they may never run.
The name is the enclosing member, so a catch in a lambda belongs to the method holding it.
A constructor is named after its type and an accessor after its property.
A catch with no type shows as `Exception`.

## `public static bool TAuditCarryCheck(SemanticModel model, SyntaxNode body, ISymbol variable, IReadOnlySet<ISymbol> carried)`

True when the body carries the variable out of its own locals.
A way out is an assignment to a field, a property, or an `out` or `ref` parameter.
A `return`, an argument to a delegate call, or an argument to a carried ring parameter is one too.
Assigning a local or passing the variable to any other call is not.

## `private static IReadOnlySet<ISymbol> TAuditCarriedRead(IReadOnlyList<SemanticModel> models)`

The ring parameters whose argument the method carries out, settled to a fixed point.
A parameter passed on to another carried parameter is carried, however long the chain.
An expression body of a method returning a value carries what it returns.
A partial method keys its parameters by the defining part, which a call binds to.

## `private static bool TAuditHoldCheck(SemanticModel model, ExpressionSyntax? expression, ISymbol variable)`

True when the value of the expression is the variable or holds it.
The variable, a cast or `as` of it and either arm of a conditional count.
So does a creation, tuple, collection, array or `with` holding it as an argument or initializer.
A call result never holds the variable, since the callee decides what it returns.

## `private static IEnumerable<SyntaxNode> TAuditNodeRead(SyntaxNode body)`

Every node of the body, without the insides of its lambdas and local functions.
