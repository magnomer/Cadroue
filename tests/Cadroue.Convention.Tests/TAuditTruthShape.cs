using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Convention.Tests;

internal static partial class TAuditTruthWalker
{
    private static void TAuditShapeScan(SyntaxNode root, List<TViolation> violations)
    {
        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (SyntaxNode node in root.DescendantNodes())
        {
            (string TViolationName, string TViolationReason)? hit = node switch
            {
                IfStatementSyntax branch when TAuditControlRead(branch.Condition) is string control
                                              && TAuditGuardCheck(branch)
                    => (control, "control decides a request in an if"),
                ConditionalExpressionSyntax choice when TAuditControlRead(choice.Condition) is string control
                                                        && (TAuditRequestCheck(choice.WhenTrue)
                                                            || TAuditRequestCheck(choice.WhenFalse))
                    => (control, "control decides a request in a ternary"),
                AssignmentExpressionSyntax
                    {
                        RawKind: (int)SyntaxKind.AddAssignmentExpression,
                        Left: MemberAccessExpressionSyntax clock
                    } wired when TAuditClockCheck(clock.Expression) && TAuditDriveCheck(wired.Right)
                    => (clock.Expression.ToString(), "a clock drives a request"),
                MethodDeclarationSyntax handler when TAuditDeafRead(handler) is string bulletin
                    => (handler.Identifier.ValueText, $"handles the bulletin '{bulletin}' without reading it"),
                LambdaExpressionSyntax deaf when TAuditLambdaCheck(deaf)
                    => (TAuditTruthSetting.TAuditObserverType, "handles a bulletin without reading it"),
                _ => null
            };
            if (hit is null)
            {
                continue;
            }

            int line = TAuditLineRead(node);
            if (seen.Add($"{line}:{hit.Value.TViolationName}"))
            {
                violations.Add(new TViolation(
                    root.SyntaxTree.FilePath, line, hit.Value.TViolationName, "Shape", hit.Value.TViolationReason));
            }
        }
    }

    private static string? TAuditControlRead(ExpressionSyntax condition)
    {
        foreach (MemberAccessExpressionSyntax access in condition.DescendantNodesAndSelf()
                     .OfType<MemberAccessExpressionSyntax>())
        {
            if (TAuditBinder.TAuditControlCheck(TAuditBinder.TAuditTypeRead(access.Expression)))
            {
                return access.Expression.ToString();
            }
        }

        return null;
    }

    private static bool TAuditClockCheck(ExpressionSyntax clock)
    {
        return TAuditBinder.TAuditNamedCheck(
            TAuditBinder.TAuditTypeRead(clock), TAuditTruthSetting.TAuditClockTypes);
    }

    private static bool TAuditLambdaCheck(LambdaExpressionSyntax lambda)
    {
        if (lambda.Parent is not ArgumentSyntax { Parent.Parent: ObjectCreationExpressionSyntax creation }
            || TAuditBinder.TAuditTypeRead(creation.Type)?.Name != TAuditTruthSetting.TAuditObserverType)
        {
            return false;
        }

        ParameterSyntax? parameter = lambda switch
        {
            SimpleLambdaExpressionSyntax simple => simple.Parameter,
            ParenthesizedLambdaExpressionSyntax full => full.ParameterList.Parameters.FirstOrDefault(),
            _ => null
        };
        if (parameter is null || TAuditBinder.TAuditSymbolRead(parameter) is not { } symbol)
        {
            return false;
        }

        HashSet<ISymbol> symbols = new([symbol], SymbolEqualityComparer.Default);
        return parameter.Identifier.ValueText == "_" || !TAuditNameCheck(lambda.Body, symbols);
    }

    private static bool TAuditDriveCheck(ExpressionSyntax handler)
    {
        return TAuditRequestCheck(handler)
               || handler.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>()
                   .Any(name =>
                       TAuditBinder.TAuditSymbolRead(name) is { } symbol && TAuditRelayNames.Contains(symbol));
    }

    private static string? TAuditDeafRead(MethodDeclarationSyntax handler)
    {
        foreach (ParameterSyntax parameter in handler.ParameterList.Parameters)
        {
            if (parameter.Type is null
                || TAuditBinder.TAuditTypeRead(parameter.Type)?.Name != TAuditTruthSetting.TAuditBulletinType
                || TAuditBinder.TAuditSymbolRead(parameter) is not { } symbol)
            {
                continue;
            }

            SyntaxNode? body = (SyntaxNode?)handler.Body ?? handler.ExpressionBody;
            HashSet<ISymbol> symbols = new([symbol], SymbolEqualityComparer.Default);
            if (body is not null && !TAuditNameCheck(body, symbols))
            {
                return parameter.Identifier.ValueText;
            }
        }

        return null;
    }
}
