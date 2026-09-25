using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Convention.Tests;

internal static class TAuditFakeWalker
{
    private const string TAuditRootReader = "";

    private static readonly CSharpParseOptions TAuditSyntaxOptions = new(
        languageVersion: LanguageVersion.Preview,
        documentationMode: DocumentationMode.None,
        kind: SourceCodeKind.Regular);

    public static IReadOnlyList<TViolation> TAuditRun(IReadOnlyList<string> testPaths, IReadOnlySet<string> markup)
    {
        Dictionary<string, TAuditFakeMember> members = new(StringComparer.Ordinal);
        foreach (SyntaxTree tree in TAuditBinder.TAuditTrees)
        {
            TAuditMemberScan(TAuditBinder.TAuditModelRead(tree), members);
        }

        HashSet<string> names = new(members.Values.Select(member => member.TAuditMemberName), StringComparer.Ordinal);
        HashSet<string> serialized = new(StringComparer.Ordinal);
        foreach (SyntaxTree tree in TAuditBinder.TAuditCompilation.SyntaxTrees)
        {
            SemanticModel model = TAuditBinder.TAuditModelRead(tree);
            TAuditUseScan(model, members, names, false);
            TAuditPersistScan(model, serialized);
        }

        CSharpCompilation tests = TAuditTestCreate(testPaths);
        foreach (SyntaxTree tree in tests.SyntaxTrees)
        {
            TAuditUseScan(tests.GetSemanticModel(tree, true), members, names, true);
        }

        TAuditLiveApply(members, markup, serialized);
        return members.Values
            .Where(member => !member.TAuditMemberLive)
            .Select(member => TAuditRowCreate(member, members))
            .OrderBy(row => row.TViolationPath, StringComparer.Ordinal)
            .ThenBy(row => row.TViolationLine)
            .ToList();
    }

    private static CSharpCompilation TAuditTestCreate(IReadOnlyList<string> testPaths)
    {
        CSharpCompilation source = TAuditBinder.TAuditCompilation;
        List<SyntaxTree> trees = testPaths
            .AsParallel()
            .AsOrdered()
            .Select(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path), TAuditSyntaxOptions, path))
            .ToList();
        trees.Add(CSharpSyntaxTree.ParseText(
            string.Concat(TAuditFakeSetting.TAuditFakeUsing.Select(space => $"global using {space};\n")),
            TAuditSyntaxOptions));
        return CSharpCompilation.Create(
            "AuditFake",
            trees,
            source.References.Append(source.ToMetadataReference()),
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                allowUnsafe: true,
                nullableContextOptions: NullableContextOptions.Enable));
    }

    private static void TAuditMemberScan(SemanticModel model, Dictionary<string, TAuditFakeMember> members)
    {
        string path = TAuditBinder.TAuditRelativeRead(model.SyntaxTree.FilePath);
        foreach (TypeDeclarationSyntax declaration in
                 model.SyntaxTree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>())
        {
            if (model.GetDeclaredSymbol(declaration) is not INamedTypeSymbol type || type.TypeKind == TypeKind.Enum)
            {
                continue;
            }

            if (declaration is RecordDeclarationSyntax { ParameterList: { } parameters })
            {
                foreach (ParameterSyntax parameter in parameters.Parameters)
                {
                    if (type.GetMembers(parameter.Identifier.ValueText).OfType<IPropertySymbol>().FirstOrDefault()
                        is IPropertySymbol property)
                    {
                        TAuditMemberAdd(members, property, parameter, path, true);
                    }
                }
            }

            foreach (MemberDeclarationSyntax member in declaration.Members)
            {
                foreach (ISymbol symbol in TAuditDeclaredRead(model, member))
                {
                    if (TAuditCandidateCheck(symbol))
                    {
                        TAuditMemberAdd(members, symbol, member, path, TAuditStoredCheck(member));
                    }
                }
            }
        }
    }

    private static void TAuditMemberAdd(
        Dictionary<string, TAuditFakeMember> members, ISymbol symbol, SyntaxNode node, string path, bool stored)
    {
        string key = TAuditKeyRead(symbol);
        if (members.ContainsKey(key))
        {
            return;
        }

        int line = node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
        members.Add(key, new TAuditFakeMember(key, symbol.Name, path, line, stored));
    }

    private static IEnumerable<ISymbol> TAuditDeclaredRead(SemanticModel model, MemberDeclarationSyntax member)
    {
        return member switch
        {
            BaseTypeDeclarationSyntax or DelegateDeclarationSyntax => [],
            BaseFieldDeclarationSyntax field => field.Declaration.Variables
                .Select(variable => model.GetDeclaredSymbol(variable))
                .OfType<ISymbol>(),
            _ => model.GetDeclaredSymbol(member) is ISymbol symbol ? [symbol] : [],
        };
    }

    private static bool TAuditStoredCheck(MemberDeclarationSyntax member)
    {
        return member switch
        {
            BaseFieldDeclarationSyntax => true,
            PropertyDeclarationSyntax { ExpressionBody: null, AccessorList: { } accessors } =>
                accessors.Accessors.All(accessor => accessor.Body is null && accessor.ExpressionBody is null),
            _ => false,
        };
    }

    private static bool TAuditCandidateCheck(ISymbol symbol)
    {
        if (symbol.IsImplicitlyDeclared || symbol.IsOverride || symbol.IsExtern)
        {
            return false;
        }

        bool kind = symbol switch
        {
            IMethodSymbol method => method.MethodKind == MethodKind.Ordinary
                && !(method.IsStatic && method.Name == "Main"),
            IPropertySymbol property => !property.IsIndexer && property.ExplicitInterfaceImplementations.Length == 0,
            IEventSymbol eventSymbol => eventSymbol.ExplicitInterfaceImplementations.Length == 0,
            IFieldSymbol field => field.AssociatedSymbol is null,
            _ => false,
        };
        return kind && TAuditContractRead(symbol).Count == 0;
    }

    private static List<ISymbol> TAuditContractRead(ISymbol symbol)
    {
        if (symbol.ContainingType is not INamedTypeSymbol type || symbol is IFieldSymbol)
        {
            return [];
        }

        return type.AllInterfaces
            .SelectMany(face => face.GetMembers())
            .Where(member => SymbolEqualityComparer.Default.Equals(
                type.FindImplementationForInterfaceMember(member)?.OriginalDefinition, symbol.OriginalDefinition))
            .ToList();
    }

    private static void TAuditUseScan(
        SemanticModel model,
        Dictionary<string, TAuditFakeMember> members,
        HashSet<string> names,
        bool test)
    {
        SyntaxNode root = model.SyntaxTree.GetRoot();
        foreach (SimpleNameSyntax name in root.DescendantNodes().OfType<SimpleNameSyntax>())
        {
            if (!names.Contains(name.Identifier.ValueText))
            {
                continue;
            }

            SymbolInfo info = model.GetSymbolInfo(name);
            IEnumerable<ISymbol> bound = info.Symbol is ISymbol symbol ? [symbol] : info.CandidateSymbols;
            foreach (ISymbol target in bound)
            {
                TAuditUseAdd(model, name, target, members, test);
            }
        }

        foreach (CommonForEachStatementSyntax loop in root.DescendantNodes().OfType<CommonForEachStatementSyntax>())
        {
            ForEachStatementInfo info = model.GetForEachStatementInfo(loop);
            ISymbol?[] targets = [info.GetEnumeratorMethod, info.MoveNextMethod, info.CurrentProperty];
            foreach (ISymbol target in targets.OfType<ISymbol>())
            {
                TAuditUseAdd(model, loop, target, members, test);
            }
        }
    }

    private static void TAuditUseAdd(
        SemanticModel model,
        SyntaxNode site,
        ISymbol target,
        Dictionary<string, TAuditFakeMember> members,
        bool test)
    {
        ISymbol normal = TAuditNormalRead(target);
        List<TAuditFakeMember> reached = new[] { normal }
            .Concat(TAuditContractRead(normal))
            .Select(symbol => members.GetValueOrDefault(TAuditKeyRead(symbol)))
            .OfType<TAuditFakeMember>()
            .ToList();
        if (reached.Count == 0)
        {
            return;
        }

        string? owner = TAuditOwnerRead(model, site);
        foreach (TAuditFakeMember member in reached)
        {
            if (owner == member.TAuditMemberKey || !TAuditReadCheck(site, normal, member))
            {
                continue;
            }

            if (test)
            {
                member.TAuditMemberTesters.Add(TAuditLabelRead(model, site));
            }
            else
            {
                member.TAuditMemberReaders.Add(owner ?? TAuditRootReader);
            }
        }
    }

    private static bool TAuditReadCheck(SyntaxNode site, ISymbol target, TAuditFakeMember member)
    {
        if (site is not ExpressionSyntax expression || target is IMethodSymbol)
        {
            return true;
        }

        while (expression.Parent is MemberAccessExpressionSyntax access && access.Name == expression)
        {
            expression = access;
        }

        if (expression.Parent is MemberBindingExpressionSyntax binding && binding.Name == expression)
        {
            expression = binding;
        }

        SyntaxNode? parent = expression.Parent;
        if (target is IEventSymbol)
        {
            return !member.TAuditMemberStored
                || parent is AssignmentExpressionSyntax
                {
                    RawKind: (int)SyntaxKind.AddAssignmentExpression or (int)SyntaxKind.SubtractAssignmentExpression,
                } subscription && subscription.Left == expression;
        }

        if (!member.TAuditMemberStored)
        {
            return true;
        }

        return parent switch
        {
            AssignmentExpressionSyntax assignment when assignment.Left == expression =>
                assignment.IsKind(SyntaxKind.CoalesceAssignmentExpression),
            PostfixUnaryExpressionSyntax or PrefixUnaryExpressionSyntax => parent.Kind() is not (
                SyntaxKind.PostIncrementExpression or SyntaxKind.PostDecrementExpression
                or SyntaxKind.PreIncrementExpression or SyntaxKind.PreDecrementExpression),
            ArgumentSyntax argument when argument.RefKindKeyword.IsKind(SyntaxKind.OutKeyword) => false,
            ArgumentSyntax { Parent: TupleExpressionSyntax tuple } =>
                !(tuple.Parent is AssignmentExpressionSyntax deconstruction && deconstruction.Left == tuple),
            _ => true,
        };
    }

    private static string? TAuditOwnerRead(SemanticModel model, SyntaxNode site)
    {
        foreach (SyntaxNode ancestor in site.Ancestors())
        {
            switch (ancestor)
            {
                case BaseMethodDeclarationSyntax or BasePropertyDeclarationSyntax:
                case VariableDeclaratorSyntax { Parent.Parent: BaseFieldDeclarationSyntax }:
                    return model.GetDeclaredSymbol(ancestor) is ISymbol owner ? TAuditKeyRead(owner) : null;
                case BaseTypeDeclarationSyntax:
                    return null;
            }
        }

        return null;
    }

    private static string TAuditLabelRead(SemanticModel model, SyntaxNode site)
    {
        foreach (SyntaxNode ancestor in site.Ancestors())
        {
            if (ancestor is BaseMethodDeclarationSyntax or BasePropertyDeclarationSyntax
                && model.GetDeclaredSymbol(ancestor) is ISymbol owner)
            {
                return $"{owner.ContainingType?.Name}.{owner.Name}";
            }
        }

        return Path.GetFileName(site.SyntaxTree.FilePath);
    }

    private static void TAuditPersistScan(SemanticModel model, HashSet<string> serialized)
    {
        foreach (InvocationExpressionSyntax call in
                 model.SyntaxTree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (model.GetSymbolInfo(call).Symbol is not IMethodSymbol method
                || method.ContainingType?.ToDisplayString() != "System.Text.Json.JsonSerializer")
            {
                continue;
            }

            IEnumerable<ITypeSymbol?> types = method.TypeArguments
                .Concat(call.ArgumentList.Arguments.Select(argument => model.GetTypeInfo(argument.Expression).Type));
            foreach (ITypeSymbol? type in types)
            {
                TAuditPersistAdd(type, serialized, new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default));
            }
        }
    }

    private static void TAuditPersistAdd(ITypeSymbol? type, HashSet<string> serialized, HashSet<ITypeSymbol> seen)
    {
        if (type is null || !seen.Add(type))
        {
            return;
        }

        if (type is IArrayTypeSymbol array)
        {
            TAuditPersistAdd(array.ElementType, serialized, seen);
            return;
        }

        if (type is not INamedTypeSymbol named)
        {
            return;
        }

        foreach (ITypeSymbol argument in named.TypeArguments)
        {
            TAuditPersistAdd(argument, serialized, seen);
        }

        if (named.Locations.All(location => !location.IsInSource))
        {
            return;
        }

        foreach (IPropertySymbol property in named.GetMembers().OfType<IPropertySymbol>())
        {
            serialized.Add(TAuditKeyRead(property));
            TAuditPersistAdd(property.Type, serialized, seen);
        }
    }

    private static void TAuditLiveApply(
        Dictionary<string, TAuditFakeMember> members, IReadOnlySet<string> markup, HashSet<string> serialized)
    {
        Dictionary<string, List<TAuditFakeMember>> dependents = new(StringComparer.Ordinal);
        Queue<TAuditFakeMember> queue = new();
        foreach (TAuditFakeMember member in members.Values)
        {
            foreach (string reader in member.TAuditMemberReaders.Where(members.ContainsKey))
            {
                if (!dependents.TryGetValue(reader, out List<TAuditFakeMember>? list))
                {
                    list = [];
                    dependents.Add(reader, list);
                }

                list.Add(member);
            }

            if (markup.Contains(member.TAuditMemberName)
                || serialized.Contains(member.TAuditMemberKey)
                || member.TAuditMemberReaders.Any(reader => !members.ContainsKey(reader)))
            {
                member.TAuditMemberLive = true;
                queue.Enqueue(member);
            }
        }

        while (queue.TryDequeue(out TAuditFakeMember? live))
        {
            foreach (TAuditFakeMember member in dependents.GetValueOrDefault(live.TAuditMemberKey) ?? [])
            {
                if (!member.TAuditMemberLive)
                {
                    member.TAuditMemberLive = true;
                    queue.Enqueue(member);
                }
            }
        }
    }

    private static TViolation TAuditRowCreate(TAuditFakeMember member, Dictionary<string, TAuditFakeMember> members)
    {
        List<string> readers = member.TAuditMemberReaders
            .Select(reader => members[reader].TAuditMemberName)
            .Order(StringComparer.Ordinal)
            .ToList();
        List<string> testers = member.TAuditMemberTesters.Order(StringComparer.Ordinal).ToList();
        string kind = testers.Count > 0 ? "Tested" : "Orphan";
        List<string> reasons = [];
        if (testers.Count > 0)
        {
            reasons.Add("by tests " + TAuditListFormat(testers));
        }

        if (readers.Count > 0)
        {
            reasons.Add("by fake members " + TAuditListFormat(readers));
        }

        string reason = reasons.Count == 0 ? "read by nothing" : "read only " + string.Join(" and ", reasons);
        return new TViolation(member.TAuditMemberPath, member.TAuditMemberLine, member.TAuditMemberName, kind, reason);
    }

    private static string TAuditListFormat(List<string> names)
    {
        string shown = string.Join(", ", names.Take(3));
        return names.Count > 3 ? $"{shown} and {names.Count - 3} more" : shown;
    }

    private static ISymbol TAuditNormalRead(ISymbol symbol)
    {
        if (symbol is IMethodSymbol method)
        {
            method = method.ReducedFrom ?? method;
            method = method.PartialDefinitionPart ?? method;
            if (method.AssociatedSymbol is ISymbol associated)
            {
                return associated.OriginalDefinition;
            }

            return method.OriginalDefinition;
        }

        return symbol.OriginalDefinition;
    }

    private static string TAuditKeyRead(ISymbol symbol)
    {
        ISymbol normal = TAuditNormalRead(symbol);
        return normal.GetDocumentationCommentId() ?? normal.ToDisplayString();
    }
}
