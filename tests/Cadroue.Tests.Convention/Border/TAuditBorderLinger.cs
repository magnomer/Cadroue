using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Convention.Tests;

internal static class TAuditBorderLinger
{
    public static IReadOnlyList<TAuditHit> TAuditLingerScan()
    {
        const string conduct = "Cadroue.Conduct";
        HashSet<string> below = TAuditBorderWalker.TAuditInnerRead().GetValueOrDefault(conduct, []);
        HashSet<string> removed = new(StringComparer.Ordinal);
        List<(TAuditHit TAuditLingerHit, string TAuditLingerKey)> subscribed = [];
        foreach (SyntaxTree tree in TAuditBinder.TAuditTrees)
        {
            string relative = TAuditBinder.TAuditRelativeRead(tree.FilePath);
            if (TAuditBinder.TAuditRingRead(relative, TAuditBorderSetting.TAuditBorderNeighbour.Keys) != conduct)
            {
                continue;
            }

            SemanticModel model = TAuditBinder.TAuditModelRead(tree);
            foreach (AssignmentExpressionSyntax assignment in
                     tree.GetRoot().DescendantNodes().OfType<AssignmentExpressionSyntax>())
            {
                bool adding = assignment.IsKind(SyntaxKind.AddAssignmentExpression);
                if ((!adding && !assignment.IsKind(SyntaxKind.SubtractAssignmentExpression))
                    || model.GetSymbolInfo(assignment.Left).Symbol is not IEventSymbol handled
                    || assignment.FirstAncestorOrSelf<TypeDeclarationSyntax>() is not { } holder
                    || model.GetDeclaredSymbol(holder) is not INamedTypeSymbol owner)
                {
                    continue;
                }

                IEventSymbol handledEvent = handled.OriginalDefinition;
                INamedTypeSymbol declaring = handledEvent.ContainingType.OriginalDefinition;
                if (SymbolEqualityComparer.Default.Equals(declaring, owner.OriginalDefinition))
                {
                    continue;
                }

                ExpressionSyntax handler = assignment.Right;
                while (handler is ParenthesizedExpressionSyntax parenthesized)
                {
                    handler = parenthesized.Expression;
                }

                bool anonymous = handler is AnonymousFunctionExpressionSyntax;
                SymbolInfo handlerInfo = model.GetSymbolInfo(handler);
                ISymbol? handlerSymbol = anonymous
                    ? null
                    : (handlerInfo.Symbol ?? handlerInfo.CandidateSymbols.FirstOrDefault())?.OriginalDefinition;
                string key = owner.OriginalDefinition.ToDisplayString() + "|" + handledEvent.ToDisplayString() + "|"
                    + (handlerSymbol?.ToDisplayString() ?? handler.ToString());
                if (!adding)
                {
                    if (!anonymous)
                    {
                        removed.Add(key);
                    }

                    continue;
                }

                string? source = TAuditBinderSymbol.TAuditSourceRead(declaring);
                string? target = source is null
                    ? null
                    : TAuditBinder.TAuditRingRead(source, TAuditBorderSetting.TAuditBorderNeighbour.Keys);
                if (target is null || !below.Contains(target))
                {
                    continue;
                }

                int line = assignment.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                string label = anonymous ? "(lambda)" : handlerSymbol?.Name ?? "(expression)";
                TAuditHit hit = new(relative, line, conduct, "Lingering", target,
                    $"{owner.Name} {declaring.Name}.{handledEvent.Name} {label}");
                subscribed.Add((hit, anonymous ? "" : key));
            }
        }

        HashSet<string> lingered = new(StringComparer.Ordinal);
        return subscribed
            .Where(item => item.TAuditLingerKey.Length == 0 || !removed.Contains(item.TAuditLingerKey))
            .Select(item => item.TAuditLingerHit)
            .Where(hit => lingered.Add($"{hit.TAuditHitPath}|{hit.TAuditHitLine}|{hit.TAuditHitName}"))
            .OrderBy(hit => hit.TAuditHitPath, StringComparer.Ordinal)
            .ThenBy(hit => hit.TAuditHitLine)
            .ToList();
    }
}
