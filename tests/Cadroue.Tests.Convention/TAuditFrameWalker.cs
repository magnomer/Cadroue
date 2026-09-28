using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Convention.Tests;

internal static class TAuditFrameWalker
{
    private static readonly string TAuditFrameProject = TAuditNameSetting.TAuditProject + ".";

    public static IReadOnlyList<TAuditHit> TAuditRun()
    {
        ConcurrentBag<List<TAuditHit>> bags = [];
        Parallel.ForEach(TAuditBinder.TAuditTrees, tree =>
        {
            string relative = TAuditBinder.TAuditRelativeRead(tree.FilePath);
            string? ring = TAuditBinder.TAuditRingRead(relative, TAuditFrameSetting.TAuditFramePure);
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
                if (!named.StartsWith(TAuditFrameProject, StringComparison.Ordinal) && TAuditOutsideCheck(ring, named))
                {
                    TAuditHitAdd(line, "frame", named);
                }

                continue;
            }

            if (node is not SimpleNameSyntax name || TAuditBinder.TAuditHeaderCheck(name))
            {
                continue;
            }

            ISymbol? symbol = TAuditBinder.TAuditSymbolRead(model, name);
            if (symbol is null || TAuditBinder.TAuditTypeRead(symbol) is not INamedTypeSymbol type)
            {
                continue;
            }

            if (TAuditBinder.TAuditSourceRead(type) is not null)
            {
                continue;
            }

            string space = type.ContainingNamespace is { IsGlobalNamespace: false } container
                ? container.ToDisplayString()
                : "";
            if (space.Length > 0 && TAuditOutsideCheck(ring, space))
            {
                TAuditHitAdd(line, "frame", space);
            }

            string? ambient = TAuditAmbientRead(symbol, type, space);
            if (ambient is not null)
            {
                TAuditHitAdd(line, "ambient", ambient);
            }
        }

        return hits;
    }

    private static bool TAuditOutsideCheck(string ring, string space) =>
        !TAuditFrameSetting.TAuditFrameAllowed.Contains(space, StringComparer.Ordinal)
        && !TAuditFrameSetting.TAuditFrameExtra.GetValueOrDefault(ring, []).Contains(space, StringComparer.Ordinal);

    private static string? TAuditAmbientRead(ISymbol symbol, INamedTypeSymbol type, string space)
    {
        string full = space.Length > 0 ? space + "." + type.Name : type.Name;
        if (symbol is not ITypeSymbol and not IAliasSymbol and not IMethodSymbol { MethodKind: MethodKind.Constructor })
        {
            full += "." + symbol.Name;
        }

        return TAuditFrameSetting.TAuditFrameAmbient.FirstOrDefault(
            candidate => full == candidate || full.StartsWith(candidate + ".", StringComparison.Ordinal));
    }
}
