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
            (string TViolationKind, string TViolationName, string TViolationReason)? hit = node switch
            {
                IfStatementSyntax branch when TAuditControlRead(branch.Condition) is string control
                                              && TAuditGuardCheck(branch)
                    => ("Shape", control, "control decides a request in an if"),
                ConditionalExpressionSyntax choice when TAuditControlRead(choice.Condition) is string control
                                                        && (TAuditRequestCheck(choice.WhenTrue)
                                                            || TAuditRequestCheck(choice.WhenFalse))
                    => ("Shape", control, "control decides a request in a ternary"),
                IfStatementSyntax branch when TAuditDialogRead(branch.Condition) is string dialog
                                              && TAuditGuardCheck(branch)
                    => ("Guard", dialog, "a dialog answer decides a request"),
                IfStatementSyntax branch when TAuditAskedCheck(branch.Condition) && TAuditGuardCheck(branch)
                    => ("Guard", TAuditExcerptRead(branch.Condition), "an engine answer decides a request in an if"),
                ConditionalExpressionSyntax choice when TAuditAskedCheck(choice.Condition)
                                                        && !TAuditPresenceCheck(choice.Condition)
                                                        && (TAuditRequestCheck(choice.WhenTrue)
                                                            || TAuditRequestCheck(choice.WhenFalse))
                    => ("Guard", TAuditExcerptRead(choice.Condition),
                        "an engine answer decides a request in a ternary"),
                SwitchStatementSyntax select when TAuditAskedCheck(select.Expression) && TAuditRequestCheck(select)
                    => ("Guard", TAuditExcerptRead(select.Expression),
                        "an engine answer decides a request in a switch"),
                AssignmentExpressionSyntax
                    {
                        RawKind: (int)SyntaxKind.AddAssignmentExpression,
                        Left: MemberAccessExpressionSyntax clock
                    } wired when TAuditClockCheck(clock.Expression) && TAuditDriveCheck(wired.Right)
                    => ("Shape", clock.Expression.ToString(), "a clock drives a request"),
                BaseObjectCreationExpressionSyntax { ArgumentList: { } arguments } creation
                    when TAuditClockCheck(creation) && arguments.Arguments.Any(argument =>
                        TAuditDriveCheck(argument.Expression))
                    => ("Shape", TAuditExcerptRead(creation), "a clock built with a callback drives a request"),
                StatementSyntax loop when loop is WhileStatementSyntax or DoStatementSyntax or ForStatementSyntax
                                          && TAuditDelayCheck(loop) && TAuditDriveCheck(loop)
                    => ("Shape", TAuditExcerptRead(loop), "a delay loop drives a request"),
                MethodDeclarationSyntax handler when TAuditDeafRead(handler) is string bulletin
                    => ("Shape", handler.Identifier.ValueText, $"handles the bulletin '{bulletin}' without reading it"),
                LambdaExpressionSyntax deaf when TAuditLambdaCheck(deaf)
                    => ("Shape", TAuditTruthSetting.TAuditBulletinType, "handles a bulletin without reading it"),
                _ => null
            };
            if (hit is null)
            {
                continue;
            }

            int line = TAuditLineRead(node);
            if (seen.Add($"{line}:{hit.Value.TViolationKind}:{hit.Value.TViolationName}"))
            {
                violations.Add(new TViolation(
                    root.SyntaxTree.FilePath,
                    line,
                    hit.Value.TViolationName,
                    hit.Value.TViolationKind,
                    hit.Value.TViolationReason));
            }
        }
    }

    private static string TAuditExcerptRead(SyntaxNode node)
    {
        return node.ToString().Split('\n')[0].Trim();
    }

    private static bool TAuditAskedCheck(ExpressionSyntax condition)
    {
        return TAuditAnswerCheck(condition) || condition.DescendantNodesAndSelf().Any(node =>
            node is IdentifierNameSyntax or MemberAccessExpressionSyntax or InvocationExpressionSyntax
            && TAuditBinder.TAuditSymbolRead(node) is { } symbol
            && TAuditReaderNames.Contains(symbol));
    }

    private static string? TAuditControlRead(ExpressionSyntax condition)
    {
        foreach (SyntaxNode node in condition.DescendantNodesAndSelf())
        {
            if (node is MemberAccessExpressionSyntax access
                && TAuditBinder.TAuditControlCheck(TAuditBinder.TAuditTypeRead(access.Expression))
                && !TAuditBinder.TAuditLogicCheck(access))
            {
                return access.Expression.ToString();
            }

            if (node is InvocationExpressionSyntax call && TAuditConsoleCheck(call))
            {
                return call.Expression.ToString();
            }
        }

        return null;
    }

    private static bool TAuditConsoleCheck(InvocationExpressionSyntax call)
    {
        ISymbol? callee = TAuditBinder.TAuditSymbolRead(call);
        return TAuditBinder.TAuditMemberCheck(callee, TAuditTruthSetting.TAuditConsoleInput);
    }

    private static string? TAuditDialogRead(ExpressionSyntax condition)
    {
        return condition.DescendantNodesAndSelf()
            .OfType<InvocationExpressionSyntax>()
            .FirstOrDefault(call => TAuditBinder.TAuditSymbolRead(call)?.ContainingType is { } owner
                                    && TAuditTruthSetting.TAuditDialogTypes.Contains(
                                        owner.ToDisplayString(), StringComparer.Ordinal))
            ?.Expression.ToString();
    }

    private static bool TAuditClockCheck(SyntaxNode clock)
    {
        ITypeSymbol? type = clock is BaseObjectCreationExpressionSyntax creation
            ? TAuditBinder.TAuditTypeRead(creation)
            : TAuditBinder.TAuditTypeRead(clock);
        return TAuditBinder.TAuditNamedCheck(type, TAuditTruthSetting.TAuditClockTypes);
    }

    private static bool TAuditDelayCheck(SyntaxNode loop)
    {
        return loop.DescendantNodes().OfType<InvocationExpressionSyntax>().Any(call =>
            TAuditBinder.TAuditMemberCheck(TAuditBinder.TAuditSymbolRead(call), TAuditTruthSetting.TAuditDelayMembers)
            || (call.Expression is MemberAccessExpressionSyntax access && TAuditClockCheck(access.Expression)));
    }

    private static bool TAuditLambdaCheck(LambdaExpressionSyntax lambda)
    {
        ParameterSyntax? parameter = lambda switch
        {
            SimpleLambdaExpressionSyntax simple => simple.Parameter,
            ParenthesizedLambdaExpressionSyntax full => full.ParameterList.Parameters.FirstOrDefault(),
            _ => null
        };
        if (parameter is null
            || TAuditBinder.TAuditSymbolRead(parameter) is not IParameterSymbol symbol
            || symbol.Type.Name != TAuditTruthSetting.TAuditBulletinType)
        {
            return false;
        }

        HashSet<ISymbol> symbols = new([symbol], SymbolEqualityComparer.Default);
        return parameter.Identifier.ValueText == "_" || !TAuditNameCheck(lambda.Body, symbols);
    }

    private static bool TAuditDriveCheck(SyntaxNode handler)
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
