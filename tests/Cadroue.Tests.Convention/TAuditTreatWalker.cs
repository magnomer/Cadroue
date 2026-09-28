using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Convention.Tests;

internal static class TAuditTreatWalker
{
    public static IReadOnlyList<TViolation> TAuditRun(IReadOnlyList<string> sourcePaths)
    {
        List<TViolation> violations = [];
        foreach (SyntaxNode root in TAuditBinder.TAuditWalkRead(sourcePaths).Where(TAuditBinder.TAuditWalkCheck))
        {
            TAuditStrictWalker.TAuditGlyphScan(root, violations);
            TAuditTreatScan(root, violations);
        }

        return violations;
    }

    private static void TAuditTreatScan(SyntaxNode root, List<TViolation> violations)
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
                         && !TAuditStrictWalker.TAuditNullCheck(binary.Left)
                         && !TAuditStrictWalker.TAuditNullCheck(binary.Right)
                         && !TAuditSetterCheck(binary)
                         && (TAuditStrictWalker.TAuditDataCheck(binary.Left)
                             || TAuditStrictWalker.TAuditDataCheck(binary.Right))
                    => $"logic value in {binary.OperatorToken.ValueText}",
                InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax access } query
                    when TAuditTruthSetting.TAuditTreatVerbs.Contains(
                             access.Name.Identifier.ValueText, StringComparer.Ordinal)
                         && (TAuditStrictWalker.TAuditDataCheck(access.Expression)
                             || query.ArgumentList.Arguments.Any(argument =>
                                 TAuditStrictWalker.TAuditDataCheck(argument.Expression)))
                    => $"logic value queried by {access.Name.Identifier.ValueText}",
                CastExpressionSyntax cast when TAuditStrictWalker.TAuditDataCheck(cast.Expression)
                    => $"logic value cast to {cast.Type}",
                TypeOfExpressionSyntax reflected when TAuditBinder.TAuditEngineCheck(reflected.Type)
                    => "logic type taken by typeof",
                AttributeArgumentSyntax argument when TAuditStrictWalker.TAuditDataCheck(argument.Expression)
                    => "logic value in an attribute",
                IfStatementSyntax branch when TAuditConditionCheck(branch.Condition)
                    => "logic value decides an if",
                ConditionalExpressionSyntax choice when TAuditConditionCheck(choice.Condition)
                    => "logic value decides a ternary",
                SwitchStatementSyntax select when TAuditStrictWalker.TAuditDataCheck(select.Expression)
                    => "logic value decides a switch",
                SwitchExpressionSyntax arms when TAuditStrictWalker.TAuditDataCheck(arms.GoverningExpression)
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
                violations.Add(new TViolation(root.SyntaxTree.FilePath, line, excerpt, "Treat", reason));
            }
        }
    }

    private static bool TAuditConditionCheck(ExpressionSyntax condition)
    {
        if (condition is IsPatternExpressionSyntax { Pattern: var pattern }
            && TAuditStrictWalker.TAuditPatternCheck(pattern))
        {
            return false;
        }

        if (condition is BinaryExpressionSyntax binary
            && (binary.IsKind(SyntaxKind.EqualsExpression) || binary.IsKind(SyntaxKind.NotEqualsExpression))
            && (TAuditStrictWalker.TAuditNullCheck(binary.Left)
                || TAuditStrictWalker.TAuditNullCheck(binary.Right)
                || TAuditSetterCheck(binary)))
        {
            return false;
        }

        return !TAuditVerdictCheck(condition) && TAuditStrictWalker.TAuditDataCheck(condition);
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
            && TAuditBinder.TAuditSymbolRead(side) is IParameterSymbol { IsImplicitlyDeclared: true });
    }

    private static bool TAuditVerdictCheck(ExpressionSyntax condition)
    {
        ExpressionSyntax core = TAuditStrictWalker.TAuditCoreRead(condition);
        return core is InvocationExpressionSyntax or MemberAccessExpressionSyntax or IdentifierNameSyntax
               && TAuditBinder.TAuditLogicCheck(TAuditBinder.TAuditSymbolRead(core));
    }

    private static int TAuditLineRead(SyntaxNode node)
    {
        return node.SyntaxTree.GetLineSpan(node.Span).StartLinePosition.Line + 1;
    }
}
