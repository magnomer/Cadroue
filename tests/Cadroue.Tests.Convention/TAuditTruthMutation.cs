using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Convention.Tests;

internal static partial class TAuditTruthWalker
{
    private static void TAuditMutationScan(SyntaxNode root, List<TViolation> violations)
    {
        TAuditFeedScan(root, violations);
        foreach (AssignmentExpressionSyntax assignment in root.DescendantNodes().OfType<AssignmentExpressionSyntax>())
        {
            if (!assignment.IsKind(SyntaxKind.SimpleAssignmentExpression))
            {
                continue;
            }

            if (assignment.Left is TupleExpressionSyntax tuple
                && tuple.Arguments.Any(argument => argument.Expression is ElementAccessExpressionSyntax))
            {
                ElementAccessExpressionSyntax slot = tuple.Arguments
                    .Select(argument => argument.Expression)
                    .OfType<ElementAccessExpressionSyntax>()
                    .First();
                violations.Add(new TViolation(
                    root.SyntaxTree.FilePath,
                    TAuditLineRead(assignment),
                    slot.Expression.ToString(),
                    "Mutation",
                    "reorders a collection with a swap"));
                continue;
            }

            if (assignment.Parent is InitializerExpressionSyntax { Parent: WithExpressionSyntax }
                && assignment.Left is IdentifierNameSyntax field
                && TAuditBinder.TAuditLogicCheck(TAuditBinder.TAuditSymbolRead(field)))
            {
                violations.Add(new TViolation(
                    root.SyntaxTree.FilePath,
                    TAuditLineRead(assignment),
                    field.Identifier.ValueText,
                    "Mutation",
                    "overrides a logic member in a record copy"));
                continue;
            }

            if (assignment.Left is ElementAccessExpressionSyntax { Expression: var rows }
                && TAuditStoreCheck(rows)
                && !TAuditFreshCheck(rows))
            {
                violations.Add(new TViolation(
                    root.SyntaxTree.FilePath,
                    TAuditLineRead(assignment),
                    rows.ToString(),
                    "Mutation",
                    "overwrites a slot of a row store"));
                continue;
            }

            if (assignment.Parent is InitializerExpressionSyntax
                || assignment.Left is not MemberAccessExpressionSyntax target
                || !TAuditBinder.TAuditLogicCheck(TAuditBinder.TAuditSymbolRead(target)))
            {
                continue;
            }

            violations.Add(new TViolation(
                root.SyntaxTree.FilePath,
                TAuditLineRead(assignment),
                target.ToString(),
                "Mutation",
                "assigns a logic member from the shell"));
        }

        foreach (InvocationExpressionSyntax call in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (call.Expression is not MemberAccessExpressionSyntax access
                || !TAuditTruthSetting.TAuditOrderVerbs.Contains(
                    access.Name.Identifier.ValueText, StringComparer.Ordinal)
                || !TAuditStoreCheck(access.Expression))
            {
                continue;
            }

            violations.Add(new TViolation(
                root.SyntaxTree.FilePath,
                TAuditLineRead(call),
                access.Expression.ToString(),
                "Mutation",
                $"reorders a collection with {access.Name.Identifier.ValueText}"));
        }
    }

    private static void TAuditFeedScan(SyntaxNode root, List<TViolation> violations)
    {
        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (SyntaxNode node in root.DescendantNodes())
        {
            IEnumerable<ExpressionSyntax> values = node switch
            {
                AssignmentExpressionSyntax { Left: MemberAccessExpressionSyntax target } assignment
                    when TAuditSurfaceRead(target.Expression)
                    => [assignment.Right],
                AssignmentExpressionSyntax
                    {
                        Left: IdentifierNameSyntax,
                        Parent: InitializerExpressionSyntax { Parent: BaseObjectCreationExpressionSyntax creation }
                    } assignment
                    when TAuditSurfaceRead(creation)
                    => [assignment.Right],
                InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax access } call
                    when TAuditSurfaceRead(access.Expression)
                    => call.ArgumentList.Arguments.Select(argument => argument.Expression),
                BaseObjectCreationExpressionSyntax { ArgumentList: { } arguments } creation
                    when TAuditSurfaceRead(creation)
                    => arguments.Arguments.Select(argument => argument.Expression),
                _ => []
            };
            foreach (ExpressionSyntax value in values)
            {
                if (value is AnonymousFunctionExpressionSyntax
                    || TAuditBinder.TAuditTypeRead(value) is not { } type
                    || !TAuditBinder.TAuditLogicCheck(type))
                {
                    continue;
                }

                int line = TAuditLineRead(value);
                string shown = type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
                if (seen.Add($"{line}:{shown}"))
                {
                    violations.Add(new TViolation(
                        root.SyntaxTree.FilePath, line, value.ToString(), "Feed", $"hands the surface a {shown}"));
                }
            }
        }
    }

    private static bool TAuditSurfaceRead(ExpressionSyntax receiver)
    {
        for (ExpressionSyntax? current = receiver; current is not null;)
        {
            ITypeSymbol? type = TAuditBinder.TAuditTypeRead(current);
            if (TAuditBinder.TAuditControlCheck(type) || TAuditBinder.TAuditSurfaceCheck(type))
            {
                return true;
            }

            current = current switch
            {
                MemberAccessExpressionSyntax access => access.Expression,
                ElementAccessExpressionSyntax element => element.Expression,
                InvocationExpressionSyntax call => call.Expression,
                _ => null
            };
        }

        return false;
    }

    private static void TAuditParityScan(List<TViolation> violations)
    {
        IReadOnlyList<string> drivers = TAuditBinder.TAuditRootRead(TAuditTruthSetting.TAuditTruthInclude);
        Dictionary<ISymbol, Dictionary<string, List<SyntaxNode>>> calls = new(SymbolEqualityComparer.Default);
        Dictionary<string, List<INamedTypeSymbol>> declared = drivers.ToDictionary(
            driver => driver, _ => new List<INamedTypeSymbol>(), StringComparer.Ordinal);
        foreach (SyntaxNode root in TAuditRoots)
        {
            string relative = TAuditBinder.TAuditRelativeRead(root.SyntaxTree.FilePath);
            string driver = drivers.First(folder =>
                relative.StartsWith(folder + "/", StringComparison.OrdinalIgnoreCase));
            declared[driver].AddRange(root.DescendantNodes().OfType<TypeDeclarationSyntax>()
                .Select(type => TAuditBinder.TAuditSymbolRead(type)).OfType<INamedTypeSymbol>());
            foreach (SimpleNameSyntax name in root.DescendantNodes().OfType<SimpleNameSyntax>())
            {
                if (TAuditBinder.TAuditSymbolRead(name) is not { } member
                    || member is not (IMethodSymbol or IPropertySymbol)
                    || !TAuditBinder.TAuditConductCheck(member.ContainingType))
                {
                    continue;
                }

                if (!calls.TryGetValue(member, out Dictionary<string, List<SyntaxNode>>? sites))
                {
                    sites = new Dictionary<string, List<SyntaxNode>>(StringComparer.Ordinal);
                    calls[member] = sites;
                }

                if (!sites.TryGetValue(driver, out List<SyntaxNode>? found))
                {
                    found = [];
                    sites[driver] = found;
                }

                found.Add(name);
            }
        }

        foreach ((ISymbol member, Dictionary<string, List<SyntaxNode>> sites) in calls)
        {
            string missing = string.Join(", ", drivers.Where(driver => !sites.ContainsKey(driver)));
            foreach (SyntaxNode site in missing.Length == 0 ? [] : sites.Values.SelectMany(found => found)
                         .GroupBy(site => site.SyntaxTree.FilePath, StringComparer.Ordinal)
                         .Select(file => file.First()))
            {
                violations.Add(new TViolation(
                    site.SyntaxTree.FilePath,
                    TAuditLineRead(site),
                    TAuditBinder.TAuditLabelRead(member),
                    "Parity",
                    $"reaches a Conduct member that {missing} never reaches"));
            }
        }

        foreach (INamedTypeSymbol port in TAuditBinder.TAuditCompilation
                     .GetSymbolsWithName(_ => true, SymbolFilter.Type)
                     .OfType<INamedTypeSymbol>()
                     .Where(type => type.TypeKind == TypeKind.Interface && TAuditBinder.TAuditConductCheck(type)))
        {
            foreach (string driver in drivers.Where(driver => !declared[driver].Any(type =>
                         type.AllInterfaces.Contains(port, SymbolEqualityComparer.Default))))
            {
                Location place = port.Locations.First(location => location.IsInSource);
                violations.Add(new TViolation(
                    place.SourceTree!.FilePath,
                    place.GetLineSpan().StartLinePosition.Line + 1,
                    port.Name,
                    "Parity",
                    $"is a Conduct port no type in {driver} implements"));
            }
        }
    }

    private static bool TAuditFreshCheck(ExpressionSyntax rows)
    {
        if (TAuditBinder.TAuditSymbolRead(rows) is not ILocalSymbol local)
        {
            return false;
        }

        return local.DeclaringSyntaxReferences
            .Select(reference => reference.GetSyntax())
            .OfType<VariableDeclaratorSyntax>()
            .Any(declarator => declarator.Initializer?.Value
                is CollectionExpressionSyntax { Elements.Count: 0 }
                or BaseObjectCreationExpressionSyntax { ArgumentList.Arguments.Count: 0, Initializer: null });
    }

    private static bool TAuditStoreCheck(ExpressionSyntax rows)
    {
        ITypeSymbol? type = TAuditBinder.TAuditTypeRead(rows);
        if (type is null || type.TypeKind == TypeKind.Error)
        {
            return true;
        }

        IEnumerable<ITypeSymbol> held = type switch
        {
            IArrayTypeSymbol array => [array.ElementType],
            INamedTypeSymbol named => named.TypeArguments,
            _ => []
        };
        return held.Any(part => TAuditBinder.TAuditLogicCheck(part)
                                || TAuditBinder.TAuditShellCheck(part)
                                || part.SpecialType == SpecialType.System_Object);
    }
}
