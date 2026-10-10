# TAuditOriginSite.cs
Hash: `0313b780b83a82fa`

## `internal static class TAuditOriginSite`

Finds where a holder or a method is used in the shell and Capsule sources.
It reads the values passed or set at those sites for `TAuditOriginHolder`.

## `private static Dictionary<string, List<SyntaxNode>> TAuditSiteNames = new(StringComparer.Ordinal);`

Every identifier in the shell and Capsule sources, keyed by its text.
Implicit `new(...)` creations and `this(...)` or `base(...)` calls sit under the key `new`, which no identifier can have.
Binding happens on demand, only for names that match a holder.

## `internal static void TAuditSiteResolve()`

Rebuilds the index from the shell and Capsule sources of the current compilation.
`TAuditOriginWalker` calls it whenever the compilation changes.

## `internal static IEnumerable<IdentifierNameSyntax> TAuditSiteRead(ISymbol held)`

Every use of the holder in the shell and Capsule sources, outside `nameof`.

## `internal static List<ExpressionSyntax?> TAuditPropertyRead(ISymbol held, SyntaxNode declaration)`

The writers of a dependency property field.
A non-neutral `defaultValue` in its metadata counts as a writer.
`SetValue` and `SetCurrentValue` write their second argument.
Reads, clears and coercions write nothing.
Any other call that takes the field, such as `SetBinding`, is unseen.

## `internal static List<ExpressionSyntax?> TAuditArgumentRead(IMethodSymbol? method, string name)`

The arguments passed for one parameter at every call site.
A constructor's sites are its creations and chained calls.
An ordinary method's sites are the invocations of that method in the indexed sources.
Its callers are unseen, as one null, when any caller may be hidden.
That holds when the method is used as a method group or has no seen call.
It holds when the method is generic, `override`, `virtual`, `abstract` or implements an interface member.
A neutral argument writes nothing, whether given or defaulted.
A site whose arguments cannot be read is unseen.

## `private static bool TAuditNeutralCheck(ExpressionSyntax value)`

True for a blank value, `false` or zero.
A neutral default writes nothing.
