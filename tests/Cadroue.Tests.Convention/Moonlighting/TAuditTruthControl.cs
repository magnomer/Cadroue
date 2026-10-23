using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Convention.Tests;

internal static class TAuditTruthControl
{
    private static readonly ConditionalWeakTable<SyntaxNode, HashSet<ISymbol>> TAuditCarriedNames = new();

    public static string? TAuditControlRead(ExpressionSyntax condition)
    {
        SyntaxNode? scope = condition.FirstAncestorOrSelf<MemberDeclarationSyntax>();
        return TAuditControlRead(condition, scope is null
            ? new HashSet<ISymbol>(SymbolEqualityComparer.Default)
            : TAuditCarriedNames.GetValue(scope, TAuditCarriedRead));
    }

    private static string? TAuditControlRead(ExpressionSyntax condition, HashSet<ISymbol> carried)
    {
        foreach (SyntaxNode node in condition.DescendantNodesAndSelf())
        {
            if (node is MemberAccessExpressionSyntax access
                && TAuditBinderSymbol.TAuditControlCheck(TAuditBinderSymbol.TAuditTypeRead(access.Expression))
                && !TAuditBinderSide.TAuditLogicCheck(access))
            {
                return access.Expression.ToString();
            }

            if (node is IdentifierNameSyntax name
                && carried.Count > 0
                && TAuditBinderSymbol.TAuditSymbolRead(name) is { } symbol
                && carried.Contains(symbol))
            {
                return name.Identifier.ValueText;
            }

            if (node is InvocationExpressionSyntax call && TAuditConsoleCheck(call))
            {
                return call.Expression.ToString();
            }
        }

        return condition.DescendantNodesAndSelf().OfType<IsPatternExpressionSyntax>()
            .FirstOrDefault(test => test.Pattern.DescendantNodesAndSelf().Any(TAuditShapeCheck))
            ?.Expression.ToString();
    }

    private static HashSet<ISymbol> TAuditCarriedRead(SyntaxNode scope)
    {
        HashSet<ISymbol> carried = new(SymbolEqualityComparer.Default);
        foreach (SyntaxNode node in scope.DescendantNodes())
        {
            switch (node)
            {
                case VariableDeclaratorSyntax { Initializer.Value: var value } declarator
                    when TAuditControlRead(value, carried) is not null:
                    TAuditTruthReference.TAuditSymbolAdd(declarator, carried);
                    break;
                case AssignmentExpressionSyntax { Left: IdentifierNameSyntax target } assignment
                    when TAuditBinderSymbol.TAuditSymbolRead(target) is ILocalSymbol or IParameterSymbol
                         && TAuditControlRead(assignment.Right, carried) is not null:
                    TAuditTruthReference.TAuditSymbolAdd(target, carried);
                    break;
                case ArgumentSyntax
                    {
                        Parent: ArgumentListSyntax { Parent: InvocationExpressionSyntax call } list
                    } argument
                    when TAuditBinderSymbol.TAuditSymbolRead(call) is IMethodSymbol
                             { MethodKind: MethodKind.LocalFunction } local
                         && TAuditControlRead(argument.Expression, carried) is not null:
                    int index = argument.NameColon is { Name.Identifier.ValueText: var label }
                        ? local.Parameters.FirstOrDefault(parameter => parameter.Name == label)?.Ordinal ?? -1
                        : list.Arguments.IndexOf(argument);
                    if (index >= 0 && index < local.Parameters.Length)
                    {
                        carried.Add(local.Parameters[index]);
                    }

                    break;
            }
        }

        return carried;
    }

    public static bool TAuditShapeCheck(SyntaxNode node)
    {
        return node is RecursivePatternSyntax { Type: { } type, PropertyPatternClause: { } clause }
               && TAuditBinderSymbol.TAuditControlCheck(TAuditBinderSymbol.TAuditTypeRead(type))
               && !clause.Subpatterns.All(part => TAuditPairCheck(part.Pattern));
    }

    public static bool TAuditFocusCheck(ExpressionSyntax condition)
    {
        ExpressionSyntax core = condition;
        while (core is ParenthesizedExpressionSyntax wrapped)
        {
            core = wrapped.Expression;
        }

        return core switch
        {
            BinaryExpressionSyntax chain when chain.IsKind(SyntaxKind.LogicalAndExpression)
                => TAuditFocusCheck(chain.Left) && TAuditFocusCheck(chain.Right),
            _ when TAuditControlRead(core) is null => true,
            MemberAccessExpressionSyntax access
                => TAuditTruthSetting.TAuditFocusMembers.Contains(
                       access.Name.Identifier.ValueText, StringComparer.Ordinal)
                   && TAuditControlRead(access.Expression) is null,
            IsPatternExpressionSyntax
                {
                    Pattern: RecursivePatternSyntax
                    {
                        Type: { } type, PositionalPatternClause: null, PropertyPatternClause: { } clause
                    }
                } test
                => TAuditControlRead(test.Expression) is null
                   && TAuditBinderSymbol.TAuditControlCheck(TAuditBinderSymbol.TAuditTypeRead(type))
                   && clause.Subpatterns.All(part =>
                       TAuditPairCheck(part.Pattern)
                       || (part.NameColon is { Name.Identifier.ValueText: var name }
                           && TAuditTruthSetting.TAuditFocusMembers.Contains(name, StringComparer.Ordinal)
                           && part.Pattern is ConstantPatternSyntax
                           {
                               Expression.RawKind: (int)SyntaxKind.TrueLiteralExpression
                           })),
            _ => false
        };
    }

    public static bool TAuditPairCheck(PatternSyntax pattern)
    {
        return TAuditStrictCondition.TAuditPatternCheck(pattern) || pattern switch
        {
            ConstantPatternSyntax { Expression: var name } => TAuditBinderSymbol.TAuditSymbolRead(name) is ITypeSymbol,
            ParenthesizedPatternSyntax { Pattern: var inner } => TAuditPairCheck(inner),
            UnaryPatternSyntax { Pattern: var negated } => TAuditPairCheck(negated),
            BinaryPatternSyntax { Left: var left, Right: var right }
                => TAuditPairCheck(left) && TAuditPairCheck(right),
            RecursivePatternSyntax { PositionalPatternClause: null, PropertyPatternClause: { } clause }
                => clause.Subpatterns.All(part => TAuditPairCheck(part.Pattern)),
            _ => false
        };
    }

    private static bool TAuditConsoleCheck(InvocationExpressionSyntax call)
    {
        ISymbol? callee = TAuditBinderSymbol.TAuditSymbolRead(call);
        return TAuditBinderSymbol.TAuditMemberCheck(callee, TAuditTruthSetting.TAuditConsoleInput);
    }
}
