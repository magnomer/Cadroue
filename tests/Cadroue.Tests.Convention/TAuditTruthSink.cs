using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Convention.Tests;

internal static partial class TAuditTruthWalker
{
    private static (string TViolationKind, string TViolationReason)? TAuditSinkRead(SyntaxNode reference)
    {
        SyntaxNode receiver = reference;
        while (receiver.Parent is ParenthesizedExpressionSyntax
               or PostfixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.SuppressNullableWarningExpression })
        {
            receiver = receiver.Parent;
        }

        if (receiver.Parent is MemberAccessExpressionSyntax { Expression: var owner } && owner == receiver)
        {
            return null;
        }

        if (reference.Parent is ConditionalAccessExpressionSyntax { Expression: var target } access
            && target == reference)
        {
            return TAuditRequestCheck(access.WhenNotNull) ? ("Gatekeeping", "decides a request through ?.") : null;
        }

        foreach (SyntaxNode ancestor in reference.Ancestors())
        {
            switch (ancestor)
            {
                case MemberDeclarationSyntax:
                    return null;
                case ArgumentSyntax { Parent.Parent: ExpressionSyntax call } argument
                    when TAuditCallRead(call) is { } callee && TAuditHotCheck(callee, argument):
                    return ("Replaying", $"passed to {callee.Name}");
                case InitializerExpressionSyntax { Parent: WithExpressionSyntax }:
                    return ("Replaying", "written into a record copy");
                case InitializerExpressionSyntax { Parent: BaseObjectCreationExpressionSyntax creation }
                    when TAuditCallRead(creation) is { } built:
                    return ("Replaying", $"written into new {built.ContainingType?.Name ?? built.Name}");
                case IfStatementSyntax branch
                    when branch.Condition.Span.Contains(reference.Span) && TAuditGatekeepingCheck(branch):
                    return ("Gatekeeping", "decides a request in an if");
                case ConditionalExpressionSyntax choice
                    when choice.Condition.Span.Contains(reference.Span)
                         && !TAuditPresenceCheck(choice.Condition)
                         && (TAuditRequestCheck(choice.WhenTrue) || TAuditRequestCheck(choice.WhenFalse)):
                    return ("Gatekeeping", "decides a request in a ternary");
                case SwitchStatementSyntax select
                    when select.Expression.Span.Contains(reference.Span) && TAuditRequestCheck(select):
                    return ("Gatekeeping", "decides a request in a switch");
                case SwitchExpressionSyntax arms
                    when arms.GoverningExpression.Span.Contains(reference.Span) && TAuditRequestCheck(arms):
                    return ("Gatekeeping", "decides a request in a switch expression");
                case WhileStatementSyntax loop
                    when loop.Condition.Span.Contains(reference.Span) && TAuditRequestCheck(loop.Statement):
                    return ("Gatekeeping", "decides a request in a while");
                case DoStatementSyntax loop
                    when loop.Condition.Span.Contains(reference.Span) && TAuditRequestCheck(loop.Statement):
                    return ("Gatekeeping", "decides a request in a do");
                case ForStatementSyntax { Condition: { } condition } loop
                    when condition.Span.Contains(reference.Span) && TAuditRequestCheck(loop.Statement):
                    return ("Gatekeeping", "decides a request in a for");
                case WhenClauseSyntax { Parent: { } label } clause
                    when TAuditRequestCheck(label.Parent is SwitchSectionSyntax section ? section : label)
                         && clause.Condition.Span.Contains(reference.Span):
                    return ("Gatekeeping", "decides a request in a when clause");
                case CatchFilterClauseSyntax filter
                    when filter.Parent is CatchClauseSyntax { Block: var handler } && TAuditRequestCheck(handler):
                    return ("Gatekeeping", "decides a request in a catch filter");
                case BinaryExpressionSyntax gate
                    when (gate.IsKind(SyntaxKind.LogicalAndExpression)
                          || gate.IsKind(SyntaxKind.LogicalOrExpression)
                          || gate.IsKind(SyntaxKind.CoalesceExpression))
                         && gate.Left.Span.Contains(reference.Span)
                         && TAuditRequestCheck(gate.Right):
                    return ("Gatekeeping", $"decides a request through {gate.OperatorToken.ValueText}");
            }
        }

        return null;
    }

    private static bool TAuditGatekeepingCheck(IfStatementSyntax branch)
    {
        if (TAuditPresenceCheck(branch.Condition))
        {
            return false;
        }

        if (TAuditRequestCheck(branch.Statement)
            || (branch.Else is not null && TAuditRequestCheck(branch.Else)))
        {
            return true;
        }

        MemberDeclarationSyntax? member = branch.FirstAncestorOrSelf<MemberDeclarationSyntax>();
        return branch.Statement.DescendantNodesAndSelf()
            .Select(node => node switch
            {
                ReturnStatementSyntax or ThrowStatementSyntax => member,
                ContinueStatementSyntax => node.Ancestors().FirstOrDefault(loop =>
                    loop is CommonForEachStatementSyntax or ForStatementSyntax or WhileStatementSyntax
                        or DoStatementSyntax),
                BreakStatementSyntax => node.Ancestors().FirstOrDefault(loop =>
                    loop is CommonForEachStatementSyntax or ForStatementSyntax or WhileStatementSyntax
                        or DoStatementSyntax or SwitchSectionSyntax),
                _ => null
            })
            .OfType<SyntaxNode>()
            .Distinct()
            .Any(scope => scope.DescendantNodes()
                .Where(node => node.SpanStart >= branch.SpanStart)
                .Any(node => node is ExpressionSyntax call
                             && (call is InvocationExpressionSyntax || call is BaseObjectCreationExpressionSyntax)
                             && TAuditCallRead(call) is not null));
    }

    private static bool TAuditPresenceCheck(ExpressionSyntax condition)
    {
        ExpressionSyntax core = TAuditStrictCondition.TAuditCoreRead(condition);
        return core switch
        {
            BinaryExpressionSyntax chain
                when chain.IsKind(SyntaxKind.LogicalAndExpression) || chain.IsKind(SyntaxKind.LogicalOrExpression)
                => TAuditPresenceCheck(chain.Left) && TAuditPresenceCheck(chain.Right),
            IsPatternExpressionSyntax { Pattern: var pattern } => TAuditStrictCondition.TAuditPatternCheck(pattern),
            BinaryExpressionSyntax binary
                when binary.IsKind(SyntaxKind.EqualsExpression) || binary.IsKind(SyntaxKind.NotEqualsExpression)
                => TAuditStrictCondition.TAuditNullCheck(binary.Left)
                   || TAuditStrictCondition.TAuditNullCheck(binary.Right),
            _ => false
        };
    }

    private static bool TAuditRequestCheck(SyntaxNode node)
    {
        return node.DescendantNodesAndSelf().Any(child =>
            child is ExpressionSyntax call
            && (call is InvocationExpressionSyntax || call is BaseObjectCreationExpressionSyntax)
            && TAuditCallRead(call) is not null);
    }

    private static bool TAuditHotCheck(ISymbol callee, ArgumentSyntax argument)
    {
        if (TAuditBinderSide.TAuditLogicCheck(callee))
        {
            return true;
        }

        if (argument.Parent is not BaseArgumentListSyntax list)
        {
            return false;
        }

        int index = argument.NameColon is { Name.Identifier.ValueText: var name }
            ? (callee as IMethodSymbol)?.Parameters.FirstOrDefault(parameter => parameter.Name == name)?.Ordinal ?? -1
            : list.Arguments.IndexOf(argument);
        return TAuditHotNames.TryGetValue(callee, out HashSet<int>? hot) && hot.Contains(index);
    }

    private static bool TAuditZeroingCheck(ArgumentSyntax argument, out ISymbol? callee)
    {
        callee = argument.Parent is ArgumentListSyntax { Parent: ExpressionSyntax call }
                 && call is InvocationExpressionSyntax or BaseObjectCreationExpressionSyntax
            ? TAuditCallRead(call) ?? TAuditBinderSymbol.TAuditSymbolRead(call)
            : null;
        if (callee is null || argument.Parent is not ArgumentListSyntax list)
        {
            return false;
        }

        if (TAuditBinderSide.TAuditLogicCheck(callee))
        {
            return true;
        }

        int index = argument.NameColon is { Name.Identifier.ValueText: var name }
            ? (callee as IMethodSymbol)?.Parameters.FirstOrDefault(parameter => parameter.Name == name)?.Ordinal ?? -1
            : list.Arguments.IndexOf(argument);
        return TAuditZeroingNames.TryGetValue(callee, out HashSet<int>? hot) && hot.Contains(index);
    }

    private static void TAuditZeroingScan(SyntaxNode root, List<TViolation> violations)
    {
        foreach (ExpressionSyntax literal in root.DescendantNodes().OfType<ExpressionSyntax>().Where(TAuditZeroCheck))
        {
            ExpressionSyntax position = literal;
            while (position.Parent is ParenthesizedExpressionSyntax or CastExpressionSyntax
                   || (position.Parent is ConditionalExpressionSyntax choice && choice.Condition != position)
                   || (position.Parent is BinaryExpressionSyntax
                       {
                           RawKind: (int)SyntaxKind.CoalesceExpression
                       } fallback
                       && fallback.Right == position))
            {
                position = (ExpressionSyntax)position.Parent;
            }

            (string TViolationName, string TViolationReason)? hit = position.Parent switch
            {
                ArgumentSyntax argument when TAuditZeroingCheck(argument, out ISymbol? callee)
                    => callee is IMethodSymbol { MethodKind: MethodKind.Constructor, ContainingType: { } built }
                        ? (built.Name, $"passes {literal} to new {built.Name}")
                        : (callee!.Name, $"passes {literal} to {callee.Name}"),
                BinaryExpressionSyntax compare
                    when compare.Kind() is SyntaxKind.EqualsExpression or SyntaxKind.NotEqualsExpression
                             or SyntaxKind.LessThanExpression or SyntaxKind.GreaterThanExpression
                             or SyntaxKind.LessThanOrEqualExpression or SyntaxKind.GreaterThanOrEqualExpression
                         && TAuditComparedRead(compare.Left == position ? compare.Right : compare.Left) is { } member
                    => (member.Name, $"compares {member.Name} with {literal}"),
                ConstantPatternSyntax pattern
                    when (pattern.Parent is UnaryPatternSyntax negated ? negated.Parent : pattern.Parent)
                             is IsPatternExpressionSyntax test
                         && TAuditComparedRead(test.Expression) is { } member
                    => (member.Name, $"compares {member.Name} with {literal}"),
                _ => null
            };
            if (hit is not null)
            {
                violations.Add(new TViolation(
                    root.SyntaxTree.FilePath,
                    TAuditTruthReference.TAuditLineRead(literal),
                    hit.Value.TViolationName,
                    "Zeroing",
                    hit.Value.TViolationReason));
            }
        }
    }

    private static bool TAuditZeroCheck(ExpressionSyntax node)
    {
        return node switch
        {
            LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NumericLiteralExpression, Token.Value: 0 or 0L } => true,
            LiteralExpressionSyntax { RawKind: (int)SyntaxKind.DefaultLiteralExpression } => true,
            DefaultExpressionSyntax => true,
            _ => false
        };
    }

    private static ISymbol? TAuditComparedRead(ExpressionSyntax side)
    {
        ExpressionSyntax core = side;
        while (core is ParenthesizedExpressionSyntax or CastExpressionSyntax or ConditionalAccessExpressionSyntax
               or PostfixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.SuppressNullableWarningExpression })
        {
            core = core switch
            {
                ParenthesizedExpressionSyntax inner => inner.Expression,
                CastExpressionSyntax cast => cast.Expression,
                ConditionalAccessExpressionSyntax access => access.WhenNotNull,
                PostfixUnaryExpressionSyntax suppress => suppress.Operand,
                _ => core
            };
        }

        return TAuditBinderSymbol.TAuditSymbolRead(core) is { Kind: SymbolKind.Field or SymbolKind.Property } member
               && (TAuditZeroingReads.Contains(member) || TAuditBinderSide.TAuditLogicCheck(member))
            ? member
            : null;
    }

    internal static ISymbol? TAuditCallRead(ExpressionSyntax call)
    {
        if (call is not (InvocationExpressionSyntax or BaseObjectCreationExpressionSyntax)
            || TAuditBinderSymbol.TAuditSymbolRead(call) is not { } callee)
        {
            return null;
        }

        if (TAuditBinderSide.TAuditLogicCheck(callee) || TAuditRelayNames.Contains(callee))
        {
            return callee;
        }

        return TAuditDelegateRead(call) is { } held && (TAuditRelayNames.Contains(held) || TAuditInterfaceCheck(held))
            ? held
            : null;
    }

    private static bool TAuditInterfaceCheck(ISymbol held)
    {
        return held is IEventSymbol { ContainingType: { } owner } happening
               && owner.AllInterfaces
                   .SelectMany(face => face.GetMembers(happening.Name).OfType<IEventSymbol>())
                   .Any(member => TAuditRelayNames.Contains(member.OriginalDefinition)
                                  && SymbolEqualityComparer.Default.Equals(
                                      owner.FindImplementationForInterfaceMember(member)?.OriginalDefinition,
                                      happening));
    }

    private static ISymbol? TAuditDelegateRead(ExpressionSyntax call)
    {
        if (call is not InvocationExpressionSyntax invocation)
        {
            return null;
        }

        ExpressionSyntax? target = invocation.Expression switch
        {
            MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Invoke" } access => access.Expression,
            MemberBindingExpressionSyntax { Name.Identifier.ValueText: "Invoke" }
                => invocation.FirstAncestorOrSelf<ConditionalAccessExpressionSyntax>()?.Expression,
            IdentifierNameSyntax or MemberAccessExpressionSyntax => invocation.Expression,
            _ => null
        };
        return target is null ? null : TAuditBinderSymbol.TAuditSymbolRead(target);
    }

    internal static bool TAuditNameofCheck(SyntaxNode node)
    {
        return node is InvocationExpressionSyntax
               {
                   Expression: IdentifierNameSyntax { Identifier.ValueText: "nameof" }
               } named
               && TAuditBinderSymbol.TAuditSymbolRead(named) is null;
    }
}
