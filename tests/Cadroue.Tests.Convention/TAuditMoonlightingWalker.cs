using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Convention.Tests;

internal static class TAuditMoonlightingWalker
{
    public static IReadOnlyList<TViolation> TAuditRun(IReadOnlyList<string> sourcePaths)
    {
        List<TViolation> violations = [];
        foreach (SyntaxNode root in TAuditBinder.TAuditWalkRead(sourcePaths).Where(TAuditBinder.TAuditWalkCheck))
        {
            TAuditStrictWalker.TAuditHomoglyphScan(root, violations);
            TAuditMoonlightingScan(root, violations);
        }

        return violations;
    }

    private static void TAuditMoonlightingScan(SyntaxNode root, List<TViolation> violations)
    {
        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (SyntaxNode node in root.DescendantNodes())
        {
            string? reason = node switch
            {
                BinaryExpressionSyntax binary
                    when !binary.IsKind(SyntaxKind.CoalesceExpression)
                         && !binary.IsKind(SyntaxKind.LogicalAndExpression)
                         && !binary.IsKind(SyntaxKind.LogicalOrExpression)
                         && !TAuditStrictCondition.TAuditNullCheck(binary.Left)
                         && !TAuditStrictCondition.TAuditNullCheck(binary.Right)
                         && !TAuditSetterCheck(binary)
                         && (TAuditStrictCondition.TAuditDataCheck(binary.Left)
                             || TAuditStrictCondition.TAuditDataCheck(binary.Right))
                    => $"logic value in {binary.OperatorToken.ValueText}",
                InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax access } query
                    when TAuditTruthSetting.TAuditMoonlightingVerbs.Contains(
                             access.Name.Identifier.ValueText, StringComparer.Ordinal)
                         && (TAuditStrictCondition.TAuditDataCheck(access.Expression)
                             || query.ArgumentList.Arguments.Any(argument =>
                                 TAuditStrictCondition.TAuditDataCheck(argument.Expression)))
                    => $"logic value queried by {access.Name.Identifier.ValueText}",
                CastExpressionSyntax cast when TAuditStrictCondition.TAuditDataCheck(cast.Expression)
                    => $"logic value cast to {cast.Type}",
                TypeOfExpressionSyntax reflected when TAuditBinderSide.TAuditEngineCheck(reflected.Type)
                    => "logic type taken by typeof",
                AttributeArgumentSyntax argument when TAuditStrictCondition.TAuditDataCheck(argument.Expression)
                    => "logic value in an attribute",
                IfStatementSyntax branch when TAuditConditionCheck(branch.Condition)
                    => "logic value decides an if",
                ConditionalExpressionSyntax choice when TAuditConditionCheck(choice.Condition)
                    => "logic value decides a ternary",
                SwitchStatementSyntax select when TAuditStrictCondition.TAuditDataCheck(select.Expression)
                    => "logic value decides a switch",
                SwitchExpressionSyntax arms when TAuditStrictCondition.TAuditDataCheck(arms.GoverningExpression)
                    => "logic value decides a switch expression",
                _ => null
            };

            if (reason is null)
            {
                continue;
            }

            int line = TAuditLineRead(node);
            if (seen.Add($"{line}:{reason}"))
            {
                string excerpt = node.ToString().Split('\n')[0].Trim();
                violations.Add(new TViolation(root.SyntaxTree.FilePath, line, excerpt, "Moonlighting", reason));
            }
        }
    }

    private static bool TAuditConditionCheck(ExpressionSyntax condition)
    {
        if (condition is IsPatternExpressionSyntax { Pattern: var pattern }
            && TAuditStrictCondition.TAuditPatternCheck(pattern))
        {
            return false;
        }

        if (condition is BinaryExpressionSyntax binary
            && (binary.IsKind(SyntaxKind.EqualsExpression) || binary.IsKind(SyntaxKind.NotEqualsExpression))
            && (TAuditStrictCondition.TAuditNullCheck(binary.Left)
                || TAuditStrictCondition.TAuditNullCheck(binary.Right)
                || TAuditSetterCheck(binary)))
        {
            return false;
        }

        return !TAuditVerdictCheck(condition) && TAuditStrictCondition.TAuditDataCheck(condition);
    }

    private static bool TAuditSetterCheck(BinaryExpressionSyntax binary)
    {
        if (!binary.IsKind(SyntaxKind.EqualsExpression) && !binary.IsKind(SyntaxKind.NotEqualsExpression)
            || binary.FirstAncestorOrSelf<AccessorDeclarationSyntax>() is not { } accessor
            || !accessor.IsKind(SyntaxKind.SetAccessorDeclaration)
            && !accessor.IsKind(SyntaxKind.InitAccessorDeclaration))
        {
            return false;
        }

        return new[] { binary.Left, binary.Right }.Any(side =>
            side is IdentifierNameSyntax { Identifier.ValueText: "value" }
            && TAuditBinderSymbol.TAuditSymbolRead(side) is IParameterSymbol { IsImplicitlyDeclared: true });
    }

    private static bool TAuditVerdictCheck(ExpressionSyntax condition)
    {
        ExpressionSyntax core = TAuditStrictCondition.TAuditCoreRead(condition);
        return core is InvocationExpressionSyntax or MemberAccessExpressionSyntax or IdentifierNameSyntax
               && TAuditBinderSide.TAuditLogicCheck(TAuditBinderSymbol.TAuditSymbolRead(core));
    }

    private static int TAuditLineRead(SyntaxNode node)
    {
        return node.SyntaxTree.GetLineSpan(node.Span).StartLinePosition.Line + 1;
    }
}
