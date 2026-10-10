using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Convention.Tests;

internal static class TAuditBorderWalker
{
    public static IReadOnlyList<TAuditHit> TAuditRun()
    {
        Dictionary<string, HashSet<string>> inner = TAuditInnerRead();
        ConcurrentBag<List<TAuditHit>> bags = [];
        Parallel.ForEach(TAuditBinder.TAuditTrees, tree =>
        {
            string relative = TAuditBinder.TAuditRelativeRead(tree.FilePath);
            string? ring = TAuditBinder.TAuditRingRead(relative, TAuditBorderSetting.TAuditBorderNeighbour.Keys);
            if (ring is not null)
            {
                bags.Add(TAuditTreeScan(TAuditBinder.TAuditModelRead(tree), relative, ring, inner));
            }
        });

        return bags.SelectMany(bag => bag)
            .OrderBy(hit => hit.TAuditHitPath, StringComparer.Ordinal)
            .ThenBy(hit => hit.TAuditHitLine)
            .ToList();
    }

    public static IReadOnlyList<TAuditHit> TAuditOfferScan()
    {
        Dictionary<string, HashSet<string>> inner = TAuditInnerRead();
        List<TAuditHit> hits = [];
        HashSet<string> taken = new(StringComparer.Ordinal);
        foreach ((string pair, string[] offer) in TAuditEngineSetting.TAuditEngineOffer
                     .Concat(TAuditDeportmentSetting.TAuditDeportmentOffer)
                     .Concat(TAuditDemeanorSetting.TAuditDemeanorOffer))
        {
            string ring = pair[..pair.IndexOf('>')];
            string neighbour = pair[(pair.IndexOf('>') + 1)..];
            if (!TAuditBorderSetting.TAuditBorderCut.Contains(ring, StringComparer.Ordinal))
            {
                continue;
            }

            IEnumerable<ISymbol> members = offer
                .SelectMany(name => TAuditBinder.TAuditCompilation.GetSymbolsWithName(name, SymbolFilter.Type))
                .OfType<INamedTypeSymbol>()
                .Where(type => TAuditBinderSymbol.TAuditSourceRead(type) is { } source
                    && TAuditBinder.TAuditRingRead(source, TAuditBorderSetting.TAuditBorderNeighbour.Keys) == neighbour)
                .SelectMany(TAuditBorderDrift.TAuditPublicRead)
                .Where(member => member.DeclaredAccessibility == Accessibility.Public && !member.IsImplicitlyDeclared);
            foreach (ISymbol member in members)
            {
                Location? location = member.Locations.FirstOrDefault(place => place.IsInSource);
                if (location is null)
                {
                    continue;
                }

                string relative = TAuditBinder.TAuditRelativeRead(location.SourceTree!.FilePath);
                int line = location.GetLineSpan().StartLinePosition.Line + 1;
                foreach (INamedTypeSymbol type in TAuditSignatureRead(member))
                {
                    string? source = TAuditBinderSymbol.TAuditSourceRead(type);
                    string? target = source is null
                        ? null
                        : TAuditBinder.TAuditRingRead(source, TAuditBorderSetting.TAuditBorderNeighbour.Keys);
                    if (target is not null && inner[neighbour].Contains(target)
                        && taken.Add($"{relative}|{line}|{ring}|{target}|{type.Name}"))
                    {
                        hits.Add(new TAuditHit(relative, line, ring, "Leaking", target, type.Name));
                    }
                }
            }
        }

        return hits;
    }

    public static IReadOnlyList<TAuditHit> TAuditSealScan()
    {
        Dictionary<string, HashSet<string>> inner = TAuditInnerRead();
        List<TAuditHit> hits = [];
        HashSet<string> taken = new(StringComparer.Ordinal);
        foreach ((string ring, string[] prefixes) in TAuditBorderSetting.TAuditUnsealingPrefix)
        {
            if (TAuditBorderSetting.TAuditBorderNeighbour.GetValueOrDefault(ring) is not [string neighbour])
            {
                continue;
            }

            IEnumerable<ISymbol> members = TAuditBinder.TAuditCompilation
                .GetSymbolsWithName(name => TAuditSealCheck(name, prefixes), SymbolFilter.Type)
                .OfType<INamedTypeSymbol>()
                .Where(type => type.ContainingType is null && type.DeclaredAccessibility == Accessibility.Public
                    && TAuditBinderSymbol.TAuditSourceRead(type) is { } source
                    && TAuditBinder.TAuditRingRead(source, TAuditBorderSetting.TAuditBorderNeighbour.Keys) == ring)
                .SelectMany(TAuditBorderDrift.TAuditPublicRead)
                .Where(member => member.DeclaredAccessibility == Accessibility.Public && !member.IsImplicitlyDeclared);
            foreach (ISymbol member in members)
            {
                Location? location = member.Locations.FirstOrDefault(place => place.IsInSource);
                if (location is null)
                {
                    continue;
                }

                string relative = TAuditBinder.TAuditRelativeRead(location.SourceTree!.FilePath);
                int line = location.GetLineSpan().StartLinePosition.Line + 1;
                foreach (INamedTypeSymbol type in TAuditSignatureRead(member))
                {
                    string? source = TAuditBinderSymbol.TAuditSourceRead(type);
                    string? target = source is null
                        ? null
                        : TAuditBinder.TAuditRingRead(source, TAuditBorderSetting.TAuditBorderNeighbour.Keys);
                    if (target is not null && inner[neighbour].Contains(target)
                        && taken.Add($"{relative}|{line}|{ring}|{target}|{type.Name}"))
                    {
                        hits.Add(new TAuditHit(relative, line, ring, "Unsealing", target, type.Name));
                    }
                }
            }
        }

        return hits;
    }

    private static bool TAuditSealCheck(string name, string[] prefixes)
    {
        return prefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal));
    }

    private static IEnumerable<INamedTypeSymbol> TAuditSignatureRead(ISymbol member)
    {
        IEnumerable<ITypeSymbol> types = member switch
        {
            IMethodSymbol { AssociatedSymbol: null } method =>
                method.Parameters.Select(parameter => parameter.Type).Prepend(method.ReturnType),
            IPropertySymbol property => property.Parameters.Select(parameter => parameter.Type).Prepend(property.Type),
            IEventSymbol handler => [handler.Type],
            IFieldSymbol field => [field.Type],
            INamedTypeSymbol nested => nested.BaseType is null
                ? nested.Interfaces
                : nested.Interfaces.Prepend(nested.BaseType),
            _ => [],
        };
        Stack<ITypeSymbol> pending = new(types);
        while (pending.Count > 0)
        {
            ITypeSymbol type = pending.Pop();
            if (type is IArrayTypeSymbol array)
            {
                pending.Push(array.ElementType);
            }
            else if (type is INamedTypeSymbol named)
            {
                yield return named.OriginalDefinition;
                foreach (ITypeSymbol argument in named.TypeArguments)
                {
                    pending.Push(argument);
                }
            }
        }
    }

    internal static Dictionary<string, HashSet<string>> TAuditInnerRead()
    {
        Dictionary<string, HashSet<string>> inner = new(StringComparer.Ordinal);
        foreach ((string ring, string[] neighbour) in TAuditBorderSetting.TAuditBorderNeighbour)
        {
            HashSet<string> seen = new(StringComparer.Ordinal);
            Queue<string> pending = new(neighbour);
            while (pending.Count > 0)
            {
                string name = pending.Dequeue();
                if (!seen.Add(name))
                {
                    continue;
                }

                foreach (string deeper in TAuditBorderSetting.TAuditBorderNeighbour.GetValueOrDefault(name, []))
                {
                    pending.Enqueue(deeper);
                }
            }

            inner[ring] = seen;
        }

        return inner;
    }

    private static List<TAuditHit> TAuditTreeScan(
        SemanticModel model, string relative, string ring, Dictionary<string, HashSet<string>> inner)
    {
        List<TAuditHit> hits = [];
        HashSet<string> taken = new(StringComparer.Ordinal);
        string[] neighbour = TAuditBorderSetting.TAuditBorderNeighbour[ring];
        bool isCut = TAuditBorderSetting.TAuditBorderCut.Contains(ring, StringComparer.Ordinal);

        void TAuditHitAdd(SyntaxNode name, string target, string label, bool data)
        {
            if (target == ring)
            {
                return;
            }

            bool under = isCut && !TAuditBorderSetting.TAuditBorderCut.Contains(target, StringComparer.Ordinal);
            string kind = neighbour.Contains(target, StringComparer.Ordinal)
                || TAuditBorderSetting.TAuditBorderCapsule.GetValueOrDefault(ring) == target ? "Commuting"
                : !inner[ring].Contains(target) ? "Trespassing"
                : under ? "Undercutting"
                : data ? "Ferrying"
                : "Leapfrogging";
            int line = name.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            if (taken.Add($"{line}|{kind}|{target}|{label}"))
            {
                hits.Add(new TAuditHit(relative, line, ring, kind, target, label));
            }
        }

        SyntaxNode root = model.SyntaxTree.GetRoot();
        foreach (UsingDirectiveSyntax directive in root.DescendantNodes().OfType<UsingDirectiveSyntax>())
        {
            if (directive.NamespaceOrType is { } named
                && model.GetSymbolInfo(named).Symbol is INamespaceSymbol space
                && TAuditSpaceRead(space.ToDisplayString()) is { } target
                && !neighbour.Contains(target, StringComparer.Ordinal)
                && TAuditBorderSetting.TAuditBorderCapsule.GetValueOrDefault(ring) != target)
            {
                TAuditHitAdd(directive, target, "using " + space.ToDisplayString(), true);
            }
        }

        foreach (SimpleNameSyntax name in root.DescendantNodes().OfType<SimpleNameSyntax>())
        {
            UsingDirectiveSyntax? header = name.FirstAncestorOrSelf<UsingDirectiveSyntax>();
            if (TAuditBinderSymbol.TAuditHeaderCheck(name)
                && header?.StaticKeyword.IsKind(SyntaxKind.StaticKeyword) != true)
            {
                continue;
            }

            ISymbol? symbol = TAuditBinderSymbol.TAuditSymbolRead(model, name);
            if (symbol is null || TAuditBinderSymbol.TAuditTypeRead(symbol) is not INamedTypeSymbol type)
            {
                continue;
            }

            string? source = TAuditBinderSymbol.TAuditSourceRead(type);
            string? target = source is null
                ? null
                : TAuditBinder.TAuditRingRead(source, TAuditBorderSetting.TAuditBorderNeighbour.Keys);
            if (target is not null)
            {
                bool behaviour = symbol is IMethodSymbol
                {
                    MethodKind: MethodKind.Ordinary or MethodKind.ReducedExtension
                };
                TAuditHitAdd(name, target, type.Name, TAuditBinderSymbol.TAuditDataCheck(type) && !behaviour);
            }
        }

        return hits;
    }

    private static string? TAuditSpaceRead(string space)
    {
        return TAuditBorderSetting.TAuditBorderNeighbour.Keys
            .Where(ring => space == ring || space.StartsWith(ring + ".", StringComparison.Ordinal))
            .MaxBy(ring => ring.Length);
    }
}
