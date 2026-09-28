using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Convention.Tests;

internal static class TAuditObjectWalker
{
    public static IReadOnlyList<TAuditObjectRow> TAuditRun(IReadOnlyList<string> sourcePaths, string repoRoot)
    {
        HashSet<string> chosen = new(sourcePaths.Select(Path.GetFullPath), StringComparer.OrdinalIgnoreCase);
        List<SyntaxTree> trees = TAuditBinder.TAuditTrees
            .Where(tree => chosen.Contains(Path.GetFullPath(tree.FilePath)))
            .ToList();

        Dictionary<INamedTypeSymbol, TAuditObjectType> types = new(SymbolEqualityComparer.Default);
        foreach (SyntaxTree tree in trees)
        {
            TAuditMemberScan(TAuditBinder.TAuditModelRead(tree), repoRoot, types);
        }

        foreach (SyntaxTree tree in trees)
        {
            TAuditLinkScan(TAuditBinder.TAuditModelRead(tree), types);
        }

        return types.Values
            .Where(type => type.TAuditTypeMembers.Count > 0)
            .Select(TAuditRowCreate)
            .OrderByDescending(row => row.TAuditObjectLines)
            .ThenBy(row => row.TAuditObjectName, StringComparer.Ordinal)
            .ToList();
    }

    private static void TAuditMemberScan(
        SemanticModel model, string repoRoot, Dictionary<INamedTypeSymbol, TAuditObjectType> types)
    {
        string relative = Path.GetRelativePath(repoRoot, model.SyntaxTree.FilePath).Replace('\\', '/');
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
            TAuditObjectPart part = new(relative, span.EndLinePosition.Line - span.StartLinePosition.Line + 1);
            type.TAuditTypeParts.Add(part);

            foreach (MemberDeclarationSyntax member in declaration.Members)
            {
                foreach ((ISymbol declared, bool state) in TAuditDeclaredRead(model, member))
                {
                    if (type.TAuditTypeMembers.ContainsKey(declared))
                    {
                        continue;
                    }

                    type.TAuditTypeMembers.Add(
                        declared, new TAuditObjectMember(type.TAuditTypeMembers.Count, declared, part, state));
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

    private static IEnumerable<(ISymbol TAuditSymbol, bool TAuditState)> TAuditDeclaredRead(
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
                        yield return (declared, !declared.IsConst);
                    }
                }

                break;
            case EventFieldDeclarationSyntax eventField:
                foreach (VariableDeclaratorSyntax variable in eventField.Declaration.Variables)
                {
                    if (model.GetDeclaredSymbol(variable) is IEventSymbol declared)
                    {
                        yield return (declared, true);
                    }
                }

                break;
            case PropertyDeclarationSyntax property:
                if (model.GetDeclaredSymbol(property) is ISymbol declaredProperty)
                {
                    bool stored = property.ExpressionBody is null
                        && property.AccessorList is not null
                        && property.AccessorList.Accessors.All(
                            accessor => accessor.Body is null && accessor.ExpressionBody is null);
                    yield return (declaredProperty, stored);
                }

                break;
            default:
                if (model.GetDeclaredSymbol(member) is ISymbol declaredMember)
                {
                    yield return (declaredMember, false);
                }

                break;
        }
    }

    private static TAuditObjectRow TAuditRowCreate(TAuditObjectType type)
    {
        List<TAuditObjectMember> members = type.TAuditTypeMembers.Values.ToList();
        int cross = members.Sum(member => member.TAuditMemberUses.Count(
            target => !ReferenceEquals(member.TAuditMemberPart, target.TAuditMemberPart)));

        HashSet<TAuditObjectMember> hubs = [];
        List<string> hubNames = [];
        foreach (TAuditObjectMember member in members.Where(member => member.TAuditMemberState))
        {
            HashSet<TAuditObjectPart> touching = [member.TAuditMemberPart];
            touching.UnionWith(member.TAuditMemberUsers.Select(user => user.TAuditMemberPart));
            if (touching.Count >= TAuditObjectSetting.TAuditHubReach)
            {
                hubs.Add(member);
                hubNames.Add($"`{member.TAuditMemberSymbol.Name}` reaches {touching.Count} parts");
            }
        }

        int lines = type.TAuditTypeParts.Sum(part => part.TAuditPartLines);
        int partCount = type.TAuditTypeParts.Count;
        double weave = TAuditWeaveRead(members, partCount, []);
        double free = TAuditWeaveRead(members, partCount, hubs);
        double density = members.Count == 0 ? 0 : cross / (double)members.Count;
        bool monolith = partCount >= TAuditObjectSetting.TAuditPartLimit
            && lines >= TAuditObjectSetting.TAuditSpanLimit
            && (free >= TAuditObjectSetting.TAuditWeaveLimit || density >= TAuditObjectSetting.TAuditDensityLimit);
        int state = members.Count(member => member.TAuditMemberState);
        bool large = partCount == 1
            && (lines >= TAuditObjectSetting.TAuditLargeLines
                || members.Count >= TAuditObjectSetting.TAuditLargeMembers
                || state >= TAuditObjectSetting.TAuditLargeState);

        return new TAuditObjectRow(
            type.TAuditTypeSymbol.ToDisplayString(),
            type.TAuditTypeParts.Select(part => part.TAuditPartPath).ToList(),
            lines,
            members.Count,
            state,
            hubNames,
            cross,
            weave,
            free,
            density,
            monolith,
            large);
    }

    private static double TAuditWeaveRead(
        List<TAuditObjectMember> members, int partCount, HashSet<TAuditObjectMember> excluded)
    {
        if (partCount == 0 || members.Count == 0)
        {
            return 0;
        }

        int[] parent = Enumerable.Range(0, members.Count).ToArray();
        int TAuditRootFind(int index)
        {
            while (parent[index] != index)
            {
                parent[index] = parent[parent[index]];
                index = parent[index];
            }

            return index;
        }

        foreach (TAuditObjectMember member in members.Where(member => !excluded.Contains(member)))
        {
            foreach (TAuditObjectMember target in member.TAuditMemberUses.Where(target => !excluded.Contains(target)))
            {
                parent[TAuditRootFind(member.TAuditMemberIndex)] = TAuditRootFind(target.TAuditMemberIndex);
            }
        }

        int widest = members
            .Where(member => !excluded.Contains(member))
            .GroupBy(member => TAuditRootFind(member.TAuditMemberIndex))
            .Select(group => group.Select(member => member.TAuditMemberPart).Distinct().Count())
            .DefaultIfEmpty(0)
            .Max();
        return widest / (double)partCount;
    }
}
