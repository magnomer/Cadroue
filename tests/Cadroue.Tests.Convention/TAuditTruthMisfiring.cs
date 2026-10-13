using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Convention.Tests;

internal static partial class TAuditTruthWalker
{
    private static void TAuditMisfiringScan(SyntaxNode root, List<TViolation> violations)
    {
        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (SyntaxNode node in root.DescendantNodes())
        {
            (string TViolationKind, string TViolationName, string TViolationReason)? hit = node switch
            {
                IfStatementSyntax branch when TAuditTruthControl.TAuditControlRead(branch.Condition) is string control
                                              && !(TAuditStrictCondition.TAuditCoreRead(branch.Condition)
                                                       is IsPatternExpressionSyntax { Pattern: var shape }
                                                   && TAuditTruthControl.TAuditPairCheck(shape))
                                              && !TAuditHearingCheck(branch)
                                              && TAuditGatekeepingCheck(branch)
                    => ("Misfiring", control, "control decides a request in an if"),
                ConditionalExpressionSyntax choice
                    when TAuditTruthControl.TAuditControlRead(choice.Condition) is string control
                         && !TAuditPresenceCheck(choice.Condition)
                         && !(TAuditStrictCondition.TAuditCoreRead(choice.Condition)
                                  is IsPatternExpressionSyntax { Pattern: var shape }
                              && TAuditTruthControl.TAuditPairCheck(shape))
                         && (TAuditRequestCheck(choice.WhenTrue)
                             || TAuditRequestCheck(choice.WhenFalse))
                    => ("Misfiring", control, "control decides a request in a ternary"),
                SwitchSectionSyntax { Parent: SwitchStatementSyntax select } section
                    when section.Labels.Select(label => TAuditCaseRead(select.Expression, label))
                             .FirstOrDefault(read => read is not null) is string control
                         && select.Sections.Any(TAuditRequestCheck)
                    => ("Misfiring", control, "control decides a request in a switch case"),
                SwitchExpressionArmSyntax { Parent: SwitchExpressionSyntax select } arm
                    when TAuditCaseRead(select.GoverningExpression, arm) is string control
                         && select.Arms.Any(other => TAuditRequestCheck(other.Expression))
                    => ("Misfiring", control, "control decides a request in a switch arm"),
                IfStatementSyntax branch when TAuditDialogRead(branch.Condition) is string dialog
                                              && TAuditGatekeepingCheck(branch)
                    => ("Gatekeeping", dialog, "a dialog answer decides a request"),
                IfStatementSyntax branch when TAuditAskedCheck(branch.Condition) && TAuditGatekeepingCheck(branch)
                    => ("Gatekeeping", TAuditExcerptRead(branch.Condition),
                        "an engine answer decides a request in an if"),
                ConditionalExpressionSyntax choice when TAuditAskedCheck(choice.Condition)
                                                        && !TAuditPresenceCheck(choice.Condition)
                                                        && (TAuditRequestCheck(choice.WhenTrue)
                                                            || TAuditRequestCheck(choice.WhenFalse))
                    => ("Gatekeeping", TAuditExcerptRead(choice.Condition),
                        "an engine answer decides a request in a ternary"),
                SwitchStatementSyntax select when TAuditAskedCheck(select.Expression) && TAuditRequestCheck(select)
                    => ("Gatekeeping", TAuditExcerptRead(select.Expression),
                        "an engine answer decides a request in a switch"),
                AssignmentExpressionSyntax
                    {
                        RawKind: (int)SyntaxKind.AddAssignmentExpression,
                        Left: MemberAccessExpressionSyntax clock
                    } wired when TAuditClockCheck(clock.Expression) && TAuditDriveCheck(wired.Right)
                    => ("Misfiring", clock.Expression.ToString(), "a clock drives a request"),
                BaseObjectCreationExpressionSyntax { ArgumentList: { } arguments } creation
                    when TAuditClockCheck(creation) && arguments.Arguments.Any(argument =>
                        TAuditDriveCheck(argument.Expression))
                    => ("Misfiring", TAuditExcerptRead(creation), "a clock built with a callback drives a request"),
                StatementSyntax loop when loop is WhileStatementSyntax or DoStatementSyntax or ForStatementSyntax
                                          && TAuditDelayCheck(loop) && TAuditDriveCheck(loop)
                    => ("Misfiring", TAuditExcerptRead(loop), "a delay loop drives a request"),
                MethodDeclarationSyntax handler when TAuditDeafRead(handler) is string bulletin
                    => ("Misfiring", handler.Identifier.ValueText,
                        $"handles the bulletin '{bulletin}' without reading it"),
                LambdaExpressionSyntax deaf when TAuditLambdaCheck(deaf)
                    => ("Misfiring", TAuditTruthSetting.TAuditBulletinType, "handles a bulletin without reading it"),
                _ => null
            };
            if (hit is null)
            {
                continue;
            }

            int line = TAuditTruthReference.TAuditLineRead(node);
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
        return TAuditAnswerCheck(condition)
               || condition.DescendantNodesAndSelf(node => !TAuditNameofCheck(node)).Any(node =>
                   node is IdentifierNameSyntax or MemberAccessExpressionSyntax or InvocationExpressionSyntax
                   && TAuditBinderSymbol.TAuditSymbolRead(node) is { } symbol
                   && TAuditReaderNames.Contains(symbol));
    }

    private static bool TAuditHearingCheck(IfStatementSyntax branch)
    {
        if (branch.Else is not null)
        {
            return false;
        }

        if (branch.Statement is ReturnStatementSyntax { Expression: null }
            or BlockSyntax { Statements: [ReturnStatementSyntax { Expression: null }] })
        {
            ExpressionSyntax condition = branch.Condition;
            while (condition is ParenthesizedExpressionSyntax wrapped)
            {
                condition = wrapped.Expression;
            }

            return condition is PrefixUnaryExpressionSyntax
                       { RawKind: (int)SyntaxKind.LogicalNotExpression } negated
                   && TAuditTruthControl.TAuditFocusCheck(negated.Operand);
        }

        MemberDeclarationSyntax? member = branch.FirstAncestorOrSelf<MemberDeclarationSyntax>();
        bool later = member is not null && member.DescendantNodes()
            .Where(node => node.SpanStart >= branch.Span.End)
            .Any(node => node is InvocationExpressionSyntax or BaseObjectCreationExpressionSyntax
                         && TAuditCallRead((ExpressionSyntax)node) is not null);
        return !later && TAuditTruthControl.TAuditFocusCheck(branch.Condition);
    }

    private static string? TAuditCaseRead(ExpressionSyntax governing, SyntaxNode label)
    {
        bool decided = label switch
        {
            CaseSwitchLabelSyntax constant => !TAuditStrictCondition.TAuditNullCheck(constant.Value),
            CasePatternSwitchLabelSyntax shape => !TAuditTruthControl.TAuditPairCheck(shape.Pattern),
            SwitchExpressionArmSyntax arm
                => arm.Pattern is not DiscardPatternSyntax && !TAuditTruthControl.TAuditPairCheck(arm.Pattern),
            _ => false
        };
        WhenClauseSyntax? guard = label switch
        {
            CasePatternSwitchLabelSyntax shape => shape.WhenClause,
            SwitchExpressionArmSyntax arm => arm.WhenClause,
            _ => null
        };
        if (guard is not null
            && TAuditTruthControl.TAuditControlRead(guard.Condition) is string control
            && !TAuditPresenceCheck(guard.Condition)
            && !(TAuditStrictCondition.TAuditCoreRead(guard.Condition)
                     is IsPatternExpressionSyntax { Pattern: var test } && TAuditTruthControl.TAuditPairCheck(test)))
        {
            return control;
        }

        PatternSyntax? pattern = label switch
        {
            CasePatternSwitchLabelSyntax shape => shape.Pattern,
            SwitchExpressionArmSyntax arm => arm.Pattern,
            _ => null
        };
        return !decided
            ? null
            : TAuditTruthControl.TAuditControlRead(governing)
              ?? (pattern?.DescendantNodesAndSelf().Any(TAuditTruthControl.TAuditShapeCheck) == true
                  ? governing.ToString()
                  : null);
    }

    private static string? TAuditDialogRead(ExpressionSyntax condition)
    {
        return condition.DescendantNodesAndSelf()
            .OfType<InvocationExpressionSyntax>()
            .FirstOrDefault(call => TAuditBinderSymbol.TAuditSymbolRead(call)?.ContainingType is { } owner
                                    && TAuditTruthSetting.TAuditDialogTypes.Contains(
                                        owner.ToDisplayString(), StringComparer.Ordinal))
            ?.Expression.ToString();
    }

    private static bool TAuditClockCheck(SyntaxNode clock)
    {
        ITypeSymbol? type = clock is BaseObjectCreationExpressionSyntax creation
            ? TAuditBinderSymbol.TAuditTypeRead(creation)
            : TAuditBinderSymbol.TAuditTypeRead(clock);
        return TAuditBinderSymbol.TAuditNamedCheck(type, TAuditTruthSetting.TAuditClockTypes);
    }

    private static bool TAuditDelayCheck(SyntaxNode loop)
    {
        return loop.DescendantNodes().OfType<InvocationExpressionSyntax>().Any(call =>
            TAuditBinderSymbol.TAuditMemberCheck(
                TAuditBinderSymbol.TAuditSymbolRead(call), TAuditTruthSetting.TAuditDelayMembers)
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
            || TAuditBinderSymbol.TAuditSymbolRead(parameter) is not IParameterSymbol symbol
            || symbol.Type.Name != TAuditTruthSetting.TAuditBulletinType)
        {
            return false;
        }

        HashSet<ISymbol> symbols = new([symbol], SymbolEqualityComparer.Default);
        return parameter.Identifier.ValueText == "_" || !TAuditTruthReference.TAuditNameCheck(lambda.Body, symbols);
    }

    private static bool TAuditDriveCheck(SyntaxNode handler)
    {
        return TAuditRequestCheck(handler)
               || handler.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>()
                   .Any(name =>
                       TAuditBinderSymbol.TAuditSymbolRead(name) is { } symbol && TAuditRelayNames.Contains(symbol));
    }

    private static string? TAuditDeafRead(MethodDeclarationSyntax handler)
    {
        foreach (ParameterSyntax parameter in handler.ParameterList.Parameters)
        {
            if (parameter.Type is null
                || TAuditBinderSymbol.TAuditTypeRead(parameter.Type)?.Name != TAuditTruthSetting.TAuditBulletinType
                || TAuditBinderSymbol.TAuditSymbolRead(parameter) is not { } symbol)
            {
                continue;
            }

            SyntaxNode? body = (SyntaxNode?)handler.Body ?? handler.ExpressionBody;
            HashSet<ISymbol> symbols = new([symbol], SymbolEqualityComparer.Default);
            if (body is not null && !TAuditTruthReference.TAuditNameCheck(body, symbols))
            {
                return parameter.Identifier.ValueText;
            }
        }

        return null;
    }
}
