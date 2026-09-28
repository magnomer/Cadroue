using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Convention.Tests;

internal static class TAuditHostWalker
{
    public static IReadOnlyList<TViolation> TAuditRun(IReadOnlyList<string> sourcePaths)
    {
        List<TViolation> violations = [];
        foreach (SyntaxNode root in TAuditBinder.TAuditWalkRead(sourcePaths))
        {
            List<SyntaxNode> breaches = [];
            TAuditBlockScan(root.ChildNodes().OfType<GlobalStatementSyntax>().Select(global => global.Statement)
                .ToList(), breaches);
            foreach (MemberDeclarationSyntax member in root.DescendantNodes().OfType<MemberDeclarationSyntax>())
            {
                switch (member)
                {
                    case BaseMethodDeclarationSyntax { Body: { } body }:
                        TAuditBlockScan(body.Statements, breaches);
                        break;
                    case BaseMethodDeclarationSyntax { ExpressionBody: { } arrow }:
                        TAuditExpressionScan(arrow.Expression, breaches);
                        break;
                    case PropertyDeclarationSyntax { ExpressionBody: { } arrow }:
                        TAuditExpressionScan(arrow.Expression, breaches);
                        break;
                    case BasePropertyDeclarationSyntax { AccessorList: { } accessors }:
                        breaches.AddRange(accessors.Accessors.Where(accessor =>
                            accessor.Body is not null || accessor.ExpressionBody is not null));
                        break;
                }
            }

            HashSet<int> seen = [];
            foreach (SyntaxNode breach in breaches.SelectMany(breach => breach.DescendantNodesAndSelf()
                         .Where(node => node == breach || node is StatementSyntax and not BlockSyntax)))
            {
                int line = TAuditStrictWalker.TAuditLineRead(breach);
                if (seen.Add(line))
                {
                    MemberDeclarationSyntax? owner = breach.FirstAncestorOrSelf<MemberDeclarationSyntax>(node =>
                        node is not GlobalStatementSyntax);
                    violations.Add(new TViolation(
                        root.SyntaxTree.FilePath,
                        line,
                        owner is null ? Path.GetFileNameWithoutExtension(root.SyntaxTree.FilePath)
                            : TAuditStrictWalker.TAuditMemberRead(owner),
                        "Wiring",
                        $"{breach.Kind()} where only construction and wiring may stand"));
                }
            }
        }

        return violations;
    }

    private static void TAuditBlockScan(IReadOnlyList<StatementSyntax> statements, List<SyntaxNode> breaches)
    {
        for (int index = 0; index < statements.Count; index++)
        {
            switch (statements[index])
            {
                case LocalDeclarationStatementSyntax local:
                    foreach (VariableDeclaratorSyntax variable in local.Declaration.Variables)
                    {
                        if (variable.Initializer is { Value: var value })
                        {
                            TAuditExpressionScan(value, breaches);
                        }
                    }

                    break;
                case ExpressionStatementSyntax { Expression: var expression }:
                    TAuditExpressionScan(expression, breaches);
                    break;
                case ReturnStatementSyntax closing when index == statements.Count - 1:
                    if (closing.Expression is not null)
                    {
                        TAuditExpressionScan(closing.Expression, breaches);
                    }

                    break;
                case LocalFunctionStatementSyntax { Body: { } body }:
                    TAuditBlockScan(body.Statements, breaches);
                    break;
                default:
                    breaches.Add(statements[index]);
                    break;
            }
        }
    }

    private static void TAuditExpressionScan(ExpressionSyntax expression, List<SyntaxNode> breaches)
    {
        switch (expression)
        {
            case SimpleNameSyntax or ThisExpressionSyntax or BaseExpressionSyntax or PredefinedTypeSyntax
                or LiteralExpressionSyntax or TypeOfExpressionSyntax:
                break;
            case MemberAccessExpressionSyntax access when access.IsKind(SyntaxKind.SimpleMemberAccessExpression):
                TAuditExpressionScan(access.Expression, breaches);
                break;
            case InvocationExpressionSyntax call:
                TAuditExpressionScan(call.Expression, breaches);
                TAuditArgumentScan(call.ArgumentList.Arguments, breaches);
                break;
            case BaseObjectCreationExpressionSyntax creation:
                TAuditArgumentScan(creation.ArgumentList?.Arguments ?? [], breaches);
                foreach (ExpressionSyntax part in creation.Initializer?.Expressions ?? [])
                {
                    TAuditExpressionScan(part, breaches);
                }

                break;
            case AssignmentExpressionSyntax assignment when assignment.IsKind(SyntaxKind.SimpleAssignmentExpression):
                TAuditExpressionScan(assignment.Left, breaches);
                TAuditExpressionScan(assignment.Right, breaches);
                break;
            case AwaitExpressionSyntax waiting:
                TAuditExpressionScan(waiting.Expression, breaches);
                break;
            case AnonymousFunctionExpressionSyntax { Block: { } block }:
                TAuditBlockScan(block.Statements, breaches);
                break;
            case AnonymousFunctionExpressionSyntax { ExpressionBody: { } body }:
                TAuditExpressionScan(body, breaches);
                break;
            default:
                breaches.Add(expression);
                break;
        }
    }

    private static void TAuditArgumentScan(IEnumerable<ArgumentSyntax> arguments, List<SyntaxNode> breaches)
    {
        foreach (ArgumentSyntax argument in arguments)
        {
            if (argument.RefKindKeyword.IsKind(SyntaxKind.None))
            {
                TAuditExpressionScan(argument.Expression, breaches);
            }
            else
            {
                breaches.Add(argument);
            }
        }
    }
}
