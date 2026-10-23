using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Convention.Tests;

internal static class TAuditObjectWalker
{
    public static IReadOnlyList<TAuditObjectRow> TAuditRun(IReadOnlyList<string> sourcePaths)
    {
        HashSet<string> chosen = new(sourcePaths.Select(Path.GetFullPath), StringComparer.OrdinalIgnoreCase);
        List<SyntaxTree> trees = TAuditBinder.TAuditTrees
            .Where(tree => chosen.Contains(Path.GetFullPath(tree.FilePath)))
            .ToList();

        Dictionary<INamedTypeSymbol, TAuditObjectType> types = new(SymbolEqualityComparer.Default);
        foreach (SyntaxTree tree in trees)
        {
            TAuditMemberScan(TAuditBinder.TAuditModelRead(tree), types);
        }

        foreach (SyntaxTree tree in trees)
        {
            TAuditLinkScan(TAuditBinder.TAuditModelRead(tree), types);
        }

        HashSet<INamedTypeSymbol> codebase = new(
            trees.SelectMany(tree => tree.GetRoot().DescendantNodes()
                .Where(node => node is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax)
                .Select(node => TAuditBinder.TAuditModelRead(tree).GetDeclaredSymbol(node))
                .OfType<INamedTypeSymbol>()),
            SymbolEqualityComparer.Default);
        Dictionary<INamedTypeSymbol, HashSet<INamedTypeSymbol>> uses = new(SymbolEqualityComparer.Default);
        foreach (SyntaxTree tree in trees)
        {
            TAuditUseScan(TAuditBinder.TAuditModelRead(tree), codebase, uses);
        }

        Dictionary<INamedTypeSymbol, int> incoming = new(SymbolEqualityComparer.Default);
        foreach (INamedTypeSymbol used in uses.Values.SelectMany(used => used))
        {
            incoming[used] = incoming.GetValueOrDefault(used) + 1;
        }

        return types.Values
            .Where(type => type.TAuditTypeMembers.Count > 0)
            .Select(type => TAuditObjectRow.TAuditRowCreate(
                type,
                uses.GetValueOrDefault(type.TAuditTypeSymbol)?.Count ?? 0,
                incoming.GetValueOrDefault(type.TAuditTypeSymbol)))
            .OrderByDescending(row => row.TAuditObjectLines)
            .ThenBy(row => row.TAuditObjectName, StringComparer.Ordinal)
            .ToList();
    }

    private static void TAuditMemberScan(SemanticModel model, Dictionary<INamedTypeSymbol, TAuditObjectType> types)
    {
        foreach (TypeDeclarationSyntax declaration in
                 model.SyntaxTree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>())
        {
            if (model.GetDeclaredSymbol(declaration) is not INamedTypeSymbol symbol)
            {
                continue;
            }

            if (!types.TryGetValue(symbol, out TAuditObjectType? type))
            {
                type = new TAuditObjectType(symbol);
                types.Add(symbol, type);
            }

            FileLinePositionSpan span = declaration.GetLocation().GetLineSpan();
            TAuditObjectPart part = new(span.EndLinePosition.Line - span.StartLinePosition.Line + 1);
            type.TAuditTypeParts.Add(part);

            foreach (MemberDeclarationSyntax member in declaration.Members)
            {
                foreach ((ISymbol declared, bool state, bool mutable) in TAuditDeclaredRead(model, member))
                {
                    if (type.TAuditTypeMembers.ContainsKey(declared))
                    {
                        continue;
                    }

                    type.TAuditTypeMembers.Add(
                        declared, new TAuditObjectMember(type.TAuditTypeMembers.Count, declared, part, state, mutable));
                }
            }
        }
    }

    private static void TAuditLinkScan(SemanticModel model, Dictionary<INamedTypeSymbol, TAuditObjectType> types)
    {
        foreach (TypeDeclarationSyntax declaration in
                 model.SyntaxTree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>())
        {
            if (model.GetDeclaredSymbol(declaration) is not INamedTypeSymbol symbol
                || !types.TryGetValue(symbol, out TAuditObjectType? type))
            {
                continue;
            }

            foreach (MemberDeclarationSyntax member in declaration.Members)
            {
                List<TAuditObjectMember> sources = TAuditDeclaredRead(model, member)
                    .Select(pair => type.TAuditTypeMembers.GetValueOrDefault(pair.TAuditSymbol))
                    .OfType<TAuditObjectMember>()
                    .ToList();
                if (sources.Count == 0)
                {
                    continue;
                }

                foreach (SimpleNameSyntax name in member.DescendantNodes().OfType<SimpleNameSyntax>())
                {
                    TAuditObjectMember? target = TAuditTargetResolve(model, name, type);
                    if (target is null)
                    {
                        continue;
                    }

                    foreach (TAuditObjectMember source in sources.Where(source => !ReferenceEquals(source, target)))
                    {
                        source.TAuditMemberUses.Add(target);
                        target.TAuditMemberUsers.Add(source);
                    }
                }
            }
        }
    }

    private static void TAuditUseScan(
        SemanticModel model,
        HashSet<INamedTypeSymbol> codebase,
        Dictionary<INamedTypeSymbol, HashSet<INamedTypeSymbol>> uses)
    {
        foreach (SyntaxNode declaration in model.SyntaxTree.GetRoot().DescendantNodes()
                     .Where(node => node is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax))
        {
            if (model.GetDeclaredSymbol(declaration) is not INamedTypeSymbol user)
            {
                continue;
            }

            if (!uses.TryGetValue(user, out HashSet<INamedTypeSymbol>? used))
            {
                used = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
                uses.Add(user, used);
            }

            IEnumerable<SimpleNameSyntax> names = declaration
                .DescendantNodes(node => node == declaration
                    || node is not (BaseTypeDeclarationSyntax or DelegateDeclarationSyntax))
                .OfType<SimpleNameSyntax>();
            foreach (SimpleNameSyntax name in names)
            {
                used.UnionWith(TAuditUseRead(TAuditBinderSymbol.TAuditSymbolRead(model, name)).Where(target =>
                    codebase.Contains(target)
                    && !SymbolEqualityComparer.Default.Equals(target, user)
                    && !TAuditNestCheck(user, target)));
            }
        }
    }

    private static IEnumerable<INamedTypeSymbol> TAuditUseRead(ISymbol? symbol) => symbol switch
    {
        null => [],
        IAliasSymbol alias => TAuditUseRead(alias.Target),
        IArrayTypeSymbol array => TAuditUseRead(array.ElementType),
        IPointerTypeSymbol pointer => TAuditUseRead(pointer.PointedAtType),
        INamedTypeSymbol named => (named.IsAnonymousType ? [] : new[] { named.OriginalDefinition })
            .Concat(named.TypeArguments.SelectMany(TAuditUseRead)),
        ITypeSymbol or INamespaceSymbol or ILocalSymbol or IParameterSymbol => [],
        IRangeVariableSymbol or IDiscardSymbol or ILabelSymbol or IPreprocessingSymbol => [],
        _ => symbol.ContainingType is null ? [] : [symbol.ContainingType.OriginalDefinition],
    };

    private static bool TAuditNestCheck(INamedTypeSymbol first, INamedTypeSymbol second)
    {
        for (INamedTypeSymbol? outer = first.ContainingType; outer is not null; outer = outer.ContainingType)
        {
            if (SymbolEqualityComparer.Default.Equals(outer.OriginalDefinition, second))
            {
                return true;
            }
        }

        for (INamedTypeSymbol? outer = second.ContainingType; outer is not null; outer = outer.ContainingType)
        {
            if (SymbolEqualityComparer.Default.Equals(outer.OriginalDefinition, first))
            {
                return true;
            }
        }

        return false;
    }

    private static TAuditObjectMember? TAuditTargetResolve(
        SemanticModel model, SimpleNameSyntax name, TAuditObjectType type)
    {
        SymbolInfo info = model.GetSymbolInfo(name);
        ISymbol? bound = info.Symbol ?? info.CandidateSymbols.FirstOrDefault();
        if (bound is IMethodSymbol { AssociatedSymbol: not null } accessor)
        {
            bound = accessor.AssociatedSymbol;
        }

        if (bound is not (IMethodSymbol or IFieldSymbol or IPropertySymbol or IEventSymbol))
        {
            return null;
        }

        return type.TAuditTypeMembers.GetValueOrDefault(bound.OriginalDefinition);
    }

    private static IEnumerable<(ISymbol TAuditSymbol, bool TAuditState, bool TAuditMutable)> TAuditDeclaredRead(
        SemanticModel model, MemberDeclarationSyntax member)
    {
        switch (member)
        {
            case BaseTypeDeclarationSyntax or DelegateDeclarationSyntax:
                yield break;
            case FieldDeclarationSyntax field:
                foreach (VariableDeclaratorSyntax variable in field.Declaration.Variables)
                {
                    if (model.GetDeclaredSymbol(variable) is IFieldSymbol declared)
                    {
                        yield return (declared, !declared.IsConst, !declared.IsConst && !declared.IsReadOnly);
                    }
                }

                break;
            case EventFieldDeclarationSyntax eventField:
                foreach (VariableDeclaratorSyntax variable in eventField.Declaration.Variables)
                {
                    if (model.GetDeclaredSymbol(variable) is IEventSymbol declared)
                    {
                        yield return (declared, true, false);
                    }
                }

                break;
            case PropertyDeclarationSyntax property:
                if (model.GetDeclaredSymbol(property) is IPropertySymbol declaredProperty
                    && declaredProperty.PartialImplementationPart is null)
                {
                    bool stored = property.ExpressionBody is null
                        && property.AccessorList is not null
                        && property.AccessorList.Accessors.All(
                            accessor => accessor.Body is null && accessor.ExpressionBody is null);
                    bool backed = declaredProperty.ContainingType.GetMembers().OfType<IFieldSymbol>().Any(
                        backing => SymbolEqualityComparer.Default.Equals(backing.AssociatedSymbol, declaredProperty));
                    bool settable = declaredProperty.SetMethod is { IsInitOnly: false };
                    ISymbol key = declaredProperty.PartialDefinitionPart ?? declaredProperty;
                    yield return (key, stored, backed && settable);
                }

                break;
            default:
                if (model.GetDeclaredSymbol(member) is ISymbol declaredMember
                    and not IMethodSymbol { PartialImplementationPart: not null })
                {
                    ISymbol key = declaredMember is IMethodSymbol { PartialDefinitionPart: { } definition }
                        ? definition
                        : declaredMember;
                    yield return (key, false, false);
                }

                break;
        }
    }
}
