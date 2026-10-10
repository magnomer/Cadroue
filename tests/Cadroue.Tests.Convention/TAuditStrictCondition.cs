using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Convention.Tests;

internal static class TAuditStrictCondition
{
    public static ExpressionSyntax TAuditCoreRead(ExpressionSyntax condition)
    {
        ExpressionSyntax core = condition;
        while (true)
        {
            ExpressionSyntax? peeled = core switch
            {
                ParenthesizedExpressionSyntax wrapped => wrapped.Expression,
                PrefixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.LogicalNotExpression } negated
                    => negated.Operand,
                _ => null
            };
            if (peeled is null)
            {
                return core;
            }

            core = peeled;
        }
    }

    public static bool TAuditPatternCheck(PatternSyntax pattern)
    {
        return pattern switch
        {
            ConstantPatternSyntax constant => TAuditNullCheck(constant.Expression),
            UnaryPatternSyntax not => TAuditPatternCheck(not.Pattern),
            DeclarationPatternSyntax => true,
            VarPatternSyntax => true,
            TypePatternSyntax => true,
            RecursivePatternSyntax { PositionalPatternClause: null } shape
                => shape.PropertyPatternClause?.Subpatterns.Count is null or 0,
            _ => false
        };
    }

    public static bool TAuditNullCheck(ExpressionSyntax expression)
    {
        return expression.IsKind(SyntaxKind.NullLiteralExpression)
               || expression.IsKind(SyntaxKind.DefaultLiteralExpression);
    }

    public static bool TAuditDataCheck(SyntaxNode node)
    {
        foreach (SyntaxNode child in node.DescendantNodesAndSelf())
        {
            bool logic = child switch
            {
                IdentifierNameSyntax or MemberBindingExpressionSyntax => TAuditBinderSide.TAuditEngineCheck(child),
                _ => false
            };
            if (logic)
            {
                return true;
            }
        }

        return false;
    }
}
