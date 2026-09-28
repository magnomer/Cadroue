using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Convention.Tests;

internal static class TAuditChainWalker
{
    public static IReadOnlyList<TAuditHit> TAuditRun()
    {
        Dictionary<string, HashSet<string>> inner = TAuditInnerRead();
        ConcurrentBag<List<TAuditHit>> bags = [];
        Parallel.ForEach(TAuditBinder.TAuditTrees, tree =>
        {
            string relative = TAuditBinder.TAuditRelativeRead(tree.FilePath);
            string? ring = TAuditBinder.TAuditRingRead(relative, TAuditChainSetting.TAuditChainReach.Keys);
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

    public static IReadOnlyList<TAuditHit> TAuditDeclaredRead()
    {
        List<TAuditHit> declared = [];
        foreach (SyntaxTree tree in TAuditBinder.TAuditTrees)
        {
            string relative = TAuditBinder.TAuditRelativeRead(tree.FilePath);
            string? ring = TAuditBinder.TAuditRingRead(relative, TAuditChainSetting.TAuditChainReach.Keys);
            if (ring is null)
            {
                continue;
            }

            IEnumerable<BaseTypeDeclarationSyntax> declarations = tree.GetRoot()
                .DescendantNodes()
                .OfType<BaseTypeDeclarationSyntax>();
            foreach (BaseTypeDeclarationSyntax declaration in declarations)
            {
                int line = declaration.Identifier.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                declared.Add(new TAuditHit(relative, line, ring, "declared", ring, declaration.Identifier.Text));
            }
        }

        return declared;
    }

    public static IReadOnlyDictionary<string, int> TAuditFileRead()
    {
        return TAuditBinder.TAuditTrees
            .Select(tree => TAuditBinder.TAuditRingRead(
                TAuditBinder.TAuditRelativeRead(tree.FilePath), TAuditChainSetting.TAuditChainReach.Keys))
            .Where(ring => ring is not null)
            .GroupBy(ring => ring!, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
    }

    public static IReadOnlyList<TAuditHit> TAuditSurfaceScan()
    {
        Dictionary<string, HashSet<string>> inner = TAuditInnerRead();
        List<TAuditHit> hits = [];
        HashSet<string> taken = new(StringComparer.Ordinal);
        foreach ((string pair, string[] surface) in TAuditChainSetting.TAuditChainSurface)
        {
            string ring = pair[..pair.IndexOf('>')];
            string neighbour = pair[(pair.IndexOf('>') + 1)..];
            if (!TAuditChainSetting.TAuditChainCut.Contains(ring, StringComparer.Ordinal))
            {
                continue;
            }

            IEnumerable<ISymbol> members = surface
                .SelectMany(name => TAuditBinder.TAuditCompilation.GetSymbolsWithName(name, SymbolFilter.Type))
                .OfType<INamedTypeSymbol>()
                .Where(type => TAuditBinder.TAuditSourceRead(type) is { } source
                    && TAuditBinder.TAuditRingRead(source, TAuditChainSetting.TAuditChainReach.Keys) == neighbour)
                .SelectMany(TAuditPublicRead)
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
                    string? source = TAuditBinder.TAuditSourceRead(type);
                    string? target = source is null
                        ? null
                        : TAuditBinder.TAuditRingRead(source, TAuditChainSetting.TAuditChainReach.Keys);
                    if (target is not null && inner[neighbour].Contains(target)
                        && taken.Add($"{relative}|{line}|{ring}|{target}|{type.Name}"))
                    {
                        hits.Add(new TAuditHit(relative, line, ring, "expose", target, type.Name));
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
        foreach ((string ring, string[] prefixes) in TAuditChainSetting.TAuditSealPrefix)
        {
            if (TAuditChainSetting.TAuditChainReach.GetValueOrDefault(ring) is not [string neighbour])
            {
                continue;
            }

            IEnumerable<ISymbol> members = TAuditBinder.TAuditCompilation
                .GetSymbolsWithName(name => TAuditSealCheck(name, prefixes), SymbolFilter.Type)
                .OfType<INamedTypeSymbol>()
                .Where(type => type.ContainingType is null && type.DeclaredAccessibility == Accessibility.Public
                    && TAuditBinder.TAuditSourceRead(type) is { } source
                    && TAuditBinder.TAuditRingRead(source, TAuditChainSetting.TAuditChainReach.Keys) == ring)
                .SelectMany(TAuditPublicRead)
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
                    string? source = TAuditBinder.TAuditSourceRead(type);
                    string? target = source is null
                        ? null
                        : TAuditBinder.TAuditRingRead(source, TAuditChainSetting.TAuditChainReach.Keys);
                    if (target is not null && inner[neighbour].Contains(target)
                        && taken.Add($"{relative}|{line}|{ring}|{target}|{type.Name}"))
                    {
                        hits.Add(new TAuditHit(relative, line, ring, "seal", target, type.Name));
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

    private static IEnumerable<ISymbol> TAuditPublicRead(INamedTypeSymbol type)
    {
        foreach (ISymbol member in type.GetMembers())
        {
            yield return member;
            if (member is INamedTypeSymbol { DeclaredAccessibility: Accessibility.Public } nested)
            {
                foreach (ISymbol inner in TAuditPublicRead(nested))
                {
                    yield return inner;
                }
            }
        }
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

    private static Dictionary<string, HashSet<string>> TAuditInnerRead()
    {
        Dictionary<string, HashSet<string>> inner = new(StringComparer.Ordinal);
        foreach ((string ring, string[] reach) in TAuditChainSetting.TAuditChainReach)
        {
            HashSet<string> seen = new(StringComparer.Ordinal);
            Queue<string> pending = new(reach);
            while (pending.Count > 0)
            {
                string name = pending.Dequeue();
                if (!seen.Add(name))
                {
                    continue;
                }

                foreach (string deeper in TAuditChainSetting.TAuditChainReach.GetValueOrDefault(name, []))
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
        string[] reach = TAuditChainSetting.TAuditChainReach[ring];
        bool isCut = TAuditChainSetting.TAuditChainCut.Contains(ring, StringComparer.Ordinal);

        void TAuditHitAdd(SyntaxNode name, string target, string label, bool data)
        {
            if (target == ring)
            {
                return;
            }

            string kind = reach.Contains(target, StringComparer.Ordinal)
                || TAuditChainSetting.TAuditChainCapsule.GetValueOrDefault(ring) == target ? "neighbour"
                : !inner[ring].Contains(target) ? "outward"
                : isCut && !TAuditChainSetting.TAuditChainCut.Contains(target, StringComparer.Ordinal) ? "cross"
                : data ? "carry"
                : "reach";
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
                && !reach.Contains(target, StringComparer.Ordinal)
                && TAuditChainSetting.TAuditChainCapsule.GetValueOrDefault(ring) != target)
            {
                TAuditHitAdd(directive, target, "using " + space.ToDisplayString(), true);
            }
        }

        foreach (SimpleNameSyntax name in root.DescendantNodes().OfType<SimpleNameSyntax>())
        {
            UsingDirectiveSyntax? header = name.FirstAncestorOrSelf<UsingDirectiveSyntax>();
            if (TAuditBinder.TAuditHeaderCheck(name) && header?.StaticKeyword.IsKind(SyntaxKind.StaticKeyword) != true)
            {
                continue;
            }

            ISymbol? symbol = TAuditBinder.TAuditSymbolRead(model, name);
            if (symbol is null || TAuditBinder.TAuditTypeRead(symbol) is not INamedTypeSymbol type)
            {
                continue;
            }

            string? source = TAuditBinder.TAuditSourceRead(type);
            string? target = source is null
                ? null
                : TAuditBinder.TAuditRingRead(source, TAuditChainSetting.TAuditChainReach.Keys);
            if (target is not null)
            {
                bool behaviour = symbol is IMethodSymbol
                {
                    MethodKind: MethodKind.Ordinary or MethodKind.ReducedExtension
                };
                TAuditHitAdd(name, target, type.Name, TAuditBinder.TAuditDataCheck(type) && !behaviour);
            }
        }

        return hits;
    }

    private static string? TAuditSpaceRead(string space)
    {
        return TAuditChainSetting.TAuditChainReach.Keys
            .Where(ring => space == ring || space.StartsWith(ring + ".", StringComparison.Ordinal))
            .MaxBy(ring => ring.Length);
    }
}
