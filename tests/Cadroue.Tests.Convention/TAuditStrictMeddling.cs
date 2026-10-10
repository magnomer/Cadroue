using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Convention.Tests;

internal static class TAuditStrictMeddling
{
    public static void TAuditMeddlingScan(
        string owner, IEnumerable<MemberDeclarationSyntax> members, List<TViolation> violations)
    {
        foreach (MemberDeclarationSyntax member in members)
        {
            List<SyntaxNode> breaches = [];
            foreach (SyntaxNode body in TAuditBodyRead(member))
            {
                TAuditBodyScan(body, breaches);
            }

            HashSet<int> seen = [];
            foreach (SyntaxNode breach in breaches.SelectMany(TAuditNestRead))
            {
                int line = TAuditStrictWalker.TAuditLineRead(breach);
                if (seen.Add(line))
                {
                    violations.Add(new TViolation(
                        member.SyntaxTree.FilePath,
                        line,
                        $"{owner}.{TAuditStrictWalker.TAuditMemberRead(member)}",
                        "Meddling",
                        $"{breach.Kind()} where only a call may stand"));
                }
            }
        }
    }

    private static IEnumerable<SyntaxNode> TAuditNestRead(SyntaxNode breach)
    {
        return breach.DescendantNodesAndSelf().Where(node =>
            node == breach
            || node is StatementSyntax and not BlockSyntax
            || node is SwitchExpressionArmSyntax);
    }

    private static IEnumerable<SyntaxNode> TAuditBodyRead(MemberDeclarationSyntax member)
    {
        IEnumerable<SyntaxNode?> bodies = member switch
        {
            ConstructorDeclarationSyntax constructor =>
                [constructor.Initializer?.ArgumentList, constructor.Body, constructor.ExpressionBody],
            BaseMethodDeclarationSyntax method => [method.Body, method.ExpressionBody],
            BaseFieldDeclarationSyntax field => field.Declaration.Variables
                .Select(variable => (SyntaxNode?)variable.Initializer?.Value),
            PropertyDeclarationSyntax { ExpressionBody: { } arrow } => [arrow],
            IndexerDeclarationSyntax { ExpressionBody: { } arrow } => [arrow],
            BasePropertyDeclarationSyntax { AccessorList: { } accessors } property => accessors.Accessors
                .SelectMany(accessor => new SyntaxNode?[] { accessor.Body, accessor.ExpressionBody })
                .Append((property as PropertyDeclarationSyntax)?.Initializer?.Value),
            _ => []
        };
        return bodies.OfType<SyntaxNode>();
    }

    private static void TAuditBodyScan(SyntaxNode body, List<SyntaxNode> breaches)
    {
        switch (body)
        {
            case BlockSyntax block:
                foreach (StatementSyntax statement in block.Statements)
                {
                    TAuditStatementScan(statement, breaches);
                }

                break;
            case ArrowExpressionClauseSyntax arrow:
                TAuditInvocationScan(arrow.Expression, breaches);
                break;
            case ArgumentListSyntax arguments:
                TAuditArgumentScan(arguments, breaches);
                break;
            case ExpressionSyntax expression:
                TAuditInvocationScan(expression, breaches);
                break;
            default:
                breaches.Add(body);
                break;
        }
    }

    private static void TAuditStatementScan(StatementSyntax statement, List<SyntaxNode> breaches)
    {
        switch (statement)
        {
            case ExpressionStatementSyntax { Expression: var expression }:
                TAuditInvocationScan(expression, breaches);
                break;
            case ReturnStatementSyntax { Expression: { } expression }:
                TAuditInvocationScan(expression, breaches);
                break;
            default:
                breaches.Add(statement);
                break;
        }
    }

    private static void TAuditInvocationScan(ExpressionSyntax expression, List<SyntaxNode> breaches)
    {
        if (expression is not InvocationExpressionSyntax call)
        {
            breaches.Add(expression);
            return;
        }

        switch (call.Expression)
        {
            case SimpleNameSyntax:
                break;
            case MemberAccessExpressionSyntax access when access.IsKind(SyntaxKind.SimpleMemberAccessExpression):
                TAuditOperandScan(access.Expression, breaches);
                break;
            default:
                breaches.Add(call.Expression);
                break;
        }

        if (TAuditBinderSymbol.TAuditSymbolRead(call) is IMethodSymbol method
            && TAuditStrictSetting.TAuditQueryTypes.Contains(
                (method.ReducedFrom ?? method).ContainingType.ToDisplayString(), StringComparer.Ordinal))
        {
            breaches.Add(call);
        }

        TAuditArgumentScan(call.ArgumentList, breaches);
    }

    private static void TAuditArgumentScan(ArgumentListSyntax arguments, List<SyntaxNode> breaches)
    {
        foreach (ArgumentSyntax argument in arguments.Arguments)
        {
            if (!argument.RefKindKeyword.IsKind(SyntaxKind.None))
            {
                breaches.Add(argument);
                continue;
            }

            TAuditOperandScan(argument.Expression, breaches);
        }
    }

    private static void TAuditOperandScan(ExpressionSyntax operand, List<SyntaxNode> breaches)
    {
        switch (operand)
        {
            case SimpleNameSyntax or ThisExpressionSyntax or BaseExpressionSyntax or PredefinedTypeSyntax
                or LiteralExpressionSyntax:
                break;
            case MemberAccessExpressionSyntax access when access.IsKind(SyntaxKind.SimpleMemberAccessExpression):
                TAuditOperandScan(access.Expression, breaches);
                break;
            case InvocationExpressionSyntax call:
                TAuditInvocationScan(call, breaches);
                break;
            case AnonymousFunctionExpressionSyntax lambda:
                TAuditBodyScan(lambda.Body, breaches);
                break;
            default:
                breaches.Add(operand);
                break;
        }
    }
}
