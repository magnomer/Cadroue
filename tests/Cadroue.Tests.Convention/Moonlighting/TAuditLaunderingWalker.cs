using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Convention.Tests;

internal static class TAuditLaunderingWalker
{
    private const string TAuditLogicColour = "logic value";

    private const string TAuditTextColour = "control input";

    private static readonly AsyncLocal<IReadOnlySet<ISymbol>?> TAuditReaderNames = new();

    public static IReadOnlyList<TViolation> TAuditRun(IReadOnlyList<string> sourcePaths, IReadOnlySet<ISymbol> readers)
    {
        TAuditReaderNames.Value = readers;
        List<TViolation> violations = [];
        try
        {
            foreach (SyntaxNode root in TAuditBinder.TAuditWalkRead(sourcePaths).Where(TAuditBinder.TAuditWalkCheck))
            {
                foreach (MemberDeclarationSyntax member in root.DescendantNodes().OfType<MemberDeclarationSyntax>())
                {
                    if (member is BaseTypeDeclarationSyntax or FieldDeclarationSyntax
                        or BaseNamespaceDeclarationSyntax)
                    {
                        continue;
                    }

                    TAuditMemberScan(member, violations);
                }
            }
        }
        finally
        {
            TAuditReaderNames.Value = null;
        }

        return violations;
    }

    private static void TAuditMemberScan(MemberDeclarationSyntax member, List<TViolation> violations)
    {
        Dictionary<ISymbol, string> tainted = TAuditTaintRead(member);
        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (SyntaxNode node in member.DescendantNodes())
        {
            if (node is MemberDeclarationSyntax)
            {
                continue;
            }

            (ExpressionSyntax TViolationSource, string TViolationReason)? sink = node switch
            {
                BinaryExpressionSyntax binary
                    when !binary.IsKind(SyntaxKind.CoalesceExpression)
                         && !binary.IsKind(SyntaxKind.LogicalAndExpression)
                         && !binary.IsKind(SyntaxKind.LogicalOrExpression)
                         && !TAuditStrictCondition.TAuditNullCheck(binary.Left)
                         && !TAuditStrictCondition.TAuditNullCheck(binary.Right)
                         && !((binary.IsKind(SyntaxKind.EqualsExpression)
                               || binary.IsKind(SyntaxKind.NotEqualsExpression))
                              && (binary.Left.Kind()
                                      is SyntaxKind.TrueLiteralExpression or SyntaxKind.FalseLiteralExpression
                                  || binary.Right.Kind()
                                      is SyntaxKind.TrueLiteralExpression or SyntaxKind.FalseLiteralExpression))
                    => (binary, $"in {binary.OperatorToken.ValueText}"),
                InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax access } query
                    when TAuditTruthSetting.TAuditMoonlightingVerbs.Contains(
                        access.Name.Identifier.ValueText, StringComparer.Ordinal)
                    => (query, $"queried by {access.Name.Identifier.ValueText}"),
                IfStatementSyntax branch when TAuditConditionCheck(branch.Condition)
                    => (branch.Condition, "decides an if"),
                ConditionalExpressionSyntax choice when TAuditConditionCheck(choice.Condition)
                    => (choice.Condition, "decides a ternary"),
                SwitchStatementSyntax select => (select.Expression, "decides a switch"),
                SwitchExpressionSyntax arms => (arms.GoverningExpression, "decides a switch expression"),
                _ => null
            };
            if (sink is null || TAuditStrictCondition.TAuditDataCheck(sink.Value.TViolationSource))
            {
                continue;
            }

            if (TAuditTaintFind(sink.Value.TViolationSource, tainted) is not (string name, string colour))
            {
                continue;
            }

            int line = TAuditLineRead(node);
            string reason = $"{colour} {sink.Value.TViolationReason} through '{name}'";
            if (seen.Add($"{line}:{reason}"))
            {
                string excerpt = node.ToString().Split('\n')[0].Trim();
                violations.Add(new TViolation(node.SyntaxTree.FilePath, line, excerpt, "Laundering", reason));
            }
        }
    }

    private static Dictionary<ISymbol, string> TAuditTaintRead(MemberDeclarationSyntax member)
    {
        Dictionary<ISymbol, string> tainted = new(SymbolEqualityComparer.Default);
        foreach (ParameterSyntax parameter in member.DescendantNodesAndSelf().OfType<ParameterSyntax>())
        {
            if (TAuditBinderSymbol.TAuditSymbolRead(parameter) is IParameterSymbol symbol
                && TAuditBinderSide.TAuditEngineCheck(symbol.Type))
            {
                tainted[symbol] = TAuditLogicColour;
            }
        }

        foreach (SyntaxNode node in member.DescendantNodes())
        {
            switch (node)
            {
                case VariableDeclaratorSyntax { Initializer.Value: var value } declarator
                    when TAuditColourRead(value, tainted) is string colour:
                    TAuditColourAdd(declarator, colour, tainted);
                    break;
                case AssignmentExpressionSyntax { Left: IdentifierNameSyntax local } assignment
                    when TAuditBinderSymbol.TAuditSymbolRead(local) is ILocalSymbol
                         && TAuditColourRead(assignment.Right, tainted) is string colour:
                    TAuditColourAdd(local, colour, tainted);
                    break;
                case ForEachStatementSyntax loop when TAuditColourRead(loop.Expression, tainted) is string colour:
                    TAuditColourAdd(loop, colour, tainted);
                    break;
                case IsPatternExpressionSyntax pattern
                    when TAuditColourRead(pattern.Expression, tainted) is string colour:
                    foreach (SingleVariableDesignationSyntax designation in
                             pattern.Pattern.DescendantNodesAndSelf().OfType<SingleVariableDesignationSyntax>())
                    {
                        TAuditColourAdd(designation, colour, tainted);
                    }

                    break;
            }
        }

        return tainted;
    }

    private static void TAuditColourAdd(SyntaxNode node, string colour, Dictionary<ISymbol, string> tainted)
    {
        if (TAuditBinderSymbol.TAuditSymbolRead(node) is { } symbol)
        {
            tainted[symbol] = colour;
        }
    }

    private static string? TAuditColourRead(ExpressionSyntax value, Dictionary<ISymbol, string> tainted)
    {
        return TAuditTaintFind(value, tainted)?.TViolationColour;
    }

    private static (string TViolationName, string TViolationColour)? TAuditTaintFind(
        SyntaxNode node, Dictionary<ISymbol, string> tainted)
    {
        foreach (SyntaxNode child in node.DescendantNodesAndSelf(parent =>
                     parent is not InvocationExpressionSyntax
                     {
                         Expression: IdentifierNameSyntax { Identifier.ValueText: "nameof" }
                     } named
                     || TAuditBinderSymbol.TAuditSymbolRead(named) is not null))
        {
            switch (child)
            {
                case IdentifierNameSyntax name
                    when TAuditBinderSymbol.TAuditSymbolRead(name) is { } symbol
                         && tainted.TryGetValue(symbol, out string? colour):
                    return (name.Identifier.ValueText, colour);
                case IdentifierNameSyntax or MemberBindingExpressionSyntax
                    when TAuditBinderSide.TAuditEngineCheck(child):
                    return (child.ToString(), TAuditLogicColour);
                case IdentifierNameSyntax name
                    when TAuditBinderSymbol.TAuditSymbolRead(name) is
                         {
                             IsStatic: true,
                             Kind: SymbolKind.Field or SymbolKind.Property,
                             ContainingType: { TypeKind: not TypeKind.Enum } owner
                         }
                         && TAuditBinderSide.TAuditConductCheck(owner):
                    return (name.Identifier.ValueText, TAuditLogicColour);
                case IdentifierNameSyntax name
                    when TAuditBinderSymbol.TAuditSymbolRead(name) is { } symbol
                         && TAuditReaderNames.Value!.Contains(symbol):
                    return (name.Identifier.ValueText, TAuditLogicColour);
                case InvocationExpressionSyntax input
                    when TAuditBinderSymbol.TAuditMemberCheck(
                        TAuditBinderSymbol.TAuditSymbolRead(input), TAuditTruthSetting.TAuditConsoleInput):
                    return (input.Expression.ToString(), TAuditTextColour);
                case MemberAccessExpressionSyntax input
                    when TAuditTruthSetting.TAuditInputMembers.Contains(
                             input.Name.Identifier.ValueText, StringComparer.Ordinal)
                         && TAuditInputCheck(input.Expression):
                    return (input.Expression.ToString(), TAuditTextColour);
            }
        }

        return null;
    }

    private static bool TAuditInputCheck(ExpressionSyntax receiver)
    {
        if (TAuditBinderSymbol.TAuditSymbolRead(receiver) is INamespaceSymbol)
        {
            return false;
        }

        ITypeSymbol? type = TAuditBinderSymbol.TAuditTypeRead(receiver);
        for (ITypeSymbol? current = type; current is not null; current = current.BaseType)
        {
            if (current.TypeKind is TypeKind.Error or TypeKind.Dynamic
                || current.ToDisplayString() == TAuditTruthSetting.TAuditInputBase)
            {
                return true;
            }
        }

        return type is null;
    }

    private static bool TAuditConditionCheck(ExpressionSyntax condition)
    {
        bool presence = condition
            .DescendantNodesAndSelf(parent => parent is BinaryExpressionSyntax chain
                                              && (chain.IsKind(SyntaxKind.LogicalAndExpression)
                                                  || chain.IsKind(SyntaxKind.LogicalOrExpression)))
            .OfType<ExpressionSyntax>()
            .Where(operand => !operand.IsKind(SyntaxKind.LogicalAndExpression)
                              && !operand.IsKind(SyntaxKind.LogicalOrExpression))
            .All(operand => operand switch
            {
                IsPatternExpressionSyntax { Pattern: var pattern } => TAuditStrictCondition.TAuditPatternCheck(pattern),
                BinaryExpressionSyntax binary
                    when binary.IsKind(SyntaxKind.EqualsExpression) || binary.IsKind(SyntaxKind.NotEqualsExpression)
                    => TAuditStrictCondition.TAuditNullCheck(binary.Left)
                       || TAuditStrictCondition.TAuditNullCheck(binary.Right),
                _ => false
            });
        return !presence && !TAuditVerdictCheck(condition);
    }

    private static bool TAuditVerdictCheck(ExpressionSyntax condition)
    {
        ExpressionSyntax core = TAuditStrictCondition.TAuditCoreRead(condition);
        if (core is not (InvocationExpressionSyntax or MemberAccessExpressionSyntax or IdentifierNameSyntax)
            || TAuditBinderSymbol.TAuditSymbolRead(core) is not { } symbol)
        {
            return false;
        }

        return TAuditBinderSide.TAuditLogicCheck(symbol) || TAuditReaderNames.Value!.Contains(symbol);
    }

    private static int TAuditLineRead(SyntaxNode node)
    {
        return node.SyntaxTree.GetLineSpan(node.Span).StartLinePosition.Line + 1;
    }
}
