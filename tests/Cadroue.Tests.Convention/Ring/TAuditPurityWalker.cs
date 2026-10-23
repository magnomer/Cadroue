using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Convention.Tests;

internal static class TAuditPurityWalker
{
    private static readonly string TAuditPurityProject = TAuditNameSetting.TAuditProject + ".";

    public static IReadOnlyList<TAuditHit> TAuditRun()
    {
        ConcurrentBag<List<TAuditHit>> bags = [];
        Parallel.ForEach(TAuditBinder.TAuditTrees, tree =>
        {
            string relative = TAuditBinder.TAuditRelativeRead(tree.FilePath);
            string? ring = TAuditBinder.TAuditRingRead(relative, TAuditPuritySetting.TAuditPurityRing);
            if (ring is not null)
            {
                bags.Add(TAuditTreeScan(TAuditBinder.TAuditModelRead(tree), relative, ring));
            }
        });

        return bags.SelectMany(bag => bag)
            .OrderBy(hit => hit.TAuditHitPath, StringComparer.Ordinal)
            .ThenBy(hit => hit.TAuditHitLine)
            .ToList();
    }

    private static List<TAuditHit> TAuditTreeScan(SemanticModel model, string relative, string ring)
    {
        List<TAuditHit> hits = [];
        HashSet<string> taken = new(StringComparer.Ordinal);

        void TAuditHitAdd(int line, string kind, string name)
        {
            if (taken.Add($"{line}|{kind}|{name}"))
            {
                hits.Add(new TAuditHit(relative, line, ring, kind, name, name));
            }
        }

        foreach (SyntaxNode node in model.SyntaxTree.GetRoot().DescendantNodes())
        {
            int line = node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            if (node is UsingDirectiveSyntax directive && directive.Name is not null)
            {
                string named = directive.Name.ToString();
                if (!named.StartsWith(TAuditPurityProject, StringComparison.Ordinal) && TAuditOutsideCheck(ring, named))
                {
                    TAuditHitAdd(line, "Foraging", named);
                }

                continue;
            }

            if (node is not SimpleNameSyntax name || TAuditBinderSymbol.TAuditHeaderCheck(name))
            {
                continue;
            }

            ISymbol? symbol = TAuditBinderSymbol.TAuditSymbolRead(model, name);
            if (symbol is null || TAuditBinderSymbol.TAuditTypeRead(symbol) is not INamedTypeSymbol type)
            {
                continue;
            }

            if (TAuditBinderSymbol.TAuditSourceRead(type) is not null)
            {
                continue;
            }

            string space = type.ContainingNamespace is { IsGlobalNamespace: false } container
                ? container.ToDisplayString()
                : "";
            if (space.Length > 0 && TAuditOutsideCheck(ring, space))
            {
                TAuditHitAdd(line, "Foraging", space);
            }

            string? eavesdropping = TAuditEavesdroppingRead(symbol, type, space);
            if (eavesdropping is not null)
            {
                TAuditHitAdd(line, "Eavesdropping", eavesdropping);
            }
        }

        return hits;
    }

    private static bool TAuditOutsideCheck(string ring, string space) =>
        !TAuditPuritySetting.TAuditPurityFrame.Contains(space, StringComparer.Ordinal)
        && !TAuditPuritySetting.TAuditPurityExtra.GetValueOrDefault(ring, []).Contains(space, StringComparer.Ordinal);

    private static string? TAuditEavesdroppingRead(ISymbol symbol, INamedTypeSymbol type, string space)
    {
        string full = space.Length > 0 ? space + "." + type.Name : type.Name;
        if (symbol is not ITypeSymbol and not IAliasSymbol and not IMethodSymbol { MethodKind: MethodKind.Constructor })
        {
            full += "." + symbol.Name;
        }

        return TAuditPuritySetting.TAuditEavesdroppingMember.FirstOrDefault(
            candidate => full == candidate || full.StartsWith(candidate + ".", StringComparison.Ordinal));
    }
}
