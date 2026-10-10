# TAuditStrictCondition.cs
Hash: `920bd6f4febc550a`

## `internal static class TAuditStrictCondition`

Shared syntax predicates on conditions, patterns and their operands.
The strict audit, the moonlighting and laundering walkers and the truth sink read them.
So no single walker owns them.

## `public static ExpressionSyntax TAuditCoreRead(ExpressionSyntax condition)`

The condition unwrapped from parentheses and negation.
Any other prefix operator stops the unwrapping and is returned as the core.

## `public static bool TAuditPatternCheck(PatternSyntax pattern)`

True for a null constant, its negation, an empty property pattern or a type pattern.
A declaration or `var` pattern that only names the value also passes.

## `public static bool TAuditNullCheck(ExpressionSyntax expression)`

True for the `null` or `default` literal.

## `public static bool TAuditDataCheck(SyntaxNode node)`

True when any name inside the node resolves below Conduct, by its symbol or by its type.
A Conduct value is the driver's to reshape, so it is not data here.
