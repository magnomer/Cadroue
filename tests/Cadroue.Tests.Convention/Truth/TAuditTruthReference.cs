using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Convention.Tests;

internal static class TAuditTruthReference
{
    private static Dictionary<ISymbol, List<IdentifierNameSyntax>> TAuditIndex = new(SymbolEqualityComparer.Default);

    internal static void TAuditIndexResolve(IReadOnlyList<SyntaxNode> roots)
    {
        TAuditIndex = new Dictionary<ISymbol, List<IdentifierNameSyntax>>(SymbolEqualityComparer.Default);
        foreach (IdentifierNameSyntax identifier in roots.SelectMany(root => root
                     .DescendantNodes(node => !TAuditTruthWalker.TAuditNameofCheck(node))
                     .OfType<IdentifierNameSyntax>()))
        {
            if (TAuditBinderSymbol.TAuditSymbolRead(identifier) is not { } symbol)
            {
                continue;
            }

            if (!TAuditIndex.TryGetValue(symbol, out List<IdentifierNameSyntax>? uses))
            {
                uses = [];
                TAuditIndex[symbol] = uses;
            }

            uses.Add(identifier);
        }
    }

    internal static IEnumerable<IdentifierNameSyntax> TAuditUseRead(IEnumerable<ISymbol> symbols)
    {
        return symbols.SelectMany(symbol => TAuditIndex.GetValueOrDefault(symbol) ?? []);
    }

    internal static bool TAuditInsideCheck(SyntaxNode node, IReadOnlyList<TypeDeclarationSyntax> type)
    {
        return type.Any(part => part.SyntaxTree == node.SyntaxTree && part.Span.Contains(node.Span));
    }

    internal static bool TAuditFieldCheck(IdentifierNameSyntax identifier, HashSet<ISymbol> symbols)
    {
        ISymbol? symbol = TAuditBinderSymbol.TAuditSymbolRead(identifier);
        return symbol is not null && symbols.Contains(symbol);
    }

    internal static HashSet<ISymbol> TAuditTaintRead(TAuditTruthField field, MemberDeclarationSyntax scope)
    {
        HashSet<ISymbol> tainted = new(SymbolEqualityComparer.Default);
        foreach (SyntaxNode node in scope.DescendantNodes())
        {
            switch (node)
            {
                case VariableDeclaratorSyntax { Initializer: not null } declarator
                    when TAuditNameCheck(declarator.Initializer.Value, field.TFieldSymbols):
                    TAuditSymbolAdd(declarator, tainted);
                    break;
                case AssignmentExpressionSyntax { Left: IdentifierNameSyntax local } assignment
                    when TAuditBinderSymbol.TAuditSymbolRead(local) is ILocalSymbol
                         && TAuditNameCheck(assignment.Right, field.TFieldSymbols):
                    TAuditSymbolAdd(local, tainted);
                    break;
                case IsPatternExpressionSyntax pattern when TAuditNameCheck(pattern.Expression, field.TFieldSymbols):
                    foreach (SingleVariableDesignationSyntax designation in
                             pattern.Pattern.DescendantNodesAndSelf().OfType<SingleVariableDesignationSyntax>())
                    {
                        TAuditSymbolAdd(designation, tainted);
                    }

                    break;
            }
        }

        return tainted;
    }

    internal static void TAuditSymbolAdd(SyntaxNode node, HashSet<ISymbol> symbols)
    {
        if (TAuditBinderSymbol.TAuditSymbolRead(node) is { } symbol)
        {
            symbols.Add(symbol);
        }
    }

    internal static SyntaxNode TAuditReferenceRead(IdentifierNameSyntax identifier)
    {
        return identifier.Parent is MemberAccessExpressionSyntax
               {
                   Expression: ThisExpressionSyntax or IdentifierNameSyntax
               } access
               && access.Name == identifier
            ? access
            : identifier;
    }

    internal static bool TAuditWriteCheck(SyntaxNode reference, out ExpressionSyntax? value)
    {
        value = null;
        switch (reference.Parent)
        {
            case AssignmentExpressionSyntax assignment when assignment.Left == reference:
                value = assignment.IsKind(SyntaxKind.SimpleAssignmentExpression) ? assignment.Right : null;
                return true;
            case ElementAccessExpressionSyntax { Parent: AssignmentExpressionSyntax slot } element
                when element.Expression == reference && slot.Left == element:
                value = slot.IsKind(SyntaxKind.SimpleAssignmentExpression) ? slot.Right : null;
                return true;
            case MemberAccessExpressionSyntax { Parent: InvocationExpressionSyntax fill } access
                when access.Expression == reference
                     && TAuditTruthSetting.TAuditFillVerbs.Contains(
                         access.Name.Identifier.ValueText, StringComparer.Ordinal):
                value = fill.ArgumentList.Arguments.LastOrDefault()?.Expression;
                return true;
            case PrefixUnaryExpressionSyntax or PostfixUnaryExpressionSyntax:
                return reference.Parent.IsKind(SyntaxKind.PreIncrementExpression)
                       || reference.Parent.IsKind(SyntaxKind.PreDecrementExpression)
                       || reference.Parent.IsKind(SyntaxKind.PostIncrementExpression)
                       || reference.Parent.IsKind(SyntaxKind.PostDecrementExpression);
            case ArgumentSyntax argument:
                return argument.RefKindKeyword.IsKind(SyntaxKind.OutKeyword)
                       || argument.RefKindKeyword.IsKind(SyntaxKind.RefKeyword);
            default:
                return false;
        }
    }

    internal static bool TAuditNameCheck(SyntaxNode node, HashSet<ISymbol> symbols)
    {
        return node.DescendantNodesAndSelf(parent => !TAuditTruthWalker.TAuditNameofCheck(parent))
            .OfType<IdentifierNameSyntax>()
            .Any(identifier => TAuditFieldCheck(identifier, symbols));
    }

    internal static int TAuditLineRead(SyntaxNode node)
    {
        return node.SyntaxTree.GetLineSpan(node.Span).StartLinePosition.Line + 1;
    }
}
