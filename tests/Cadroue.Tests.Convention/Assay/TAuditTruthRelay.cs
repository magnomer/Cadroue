using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Convention.Tests;

internal static partial class TAuditTruthWalker
{
    private static HashSet<ISymbol> TAuditRelayNames = new(SymbolEqualityComparer.Default);

    private static HashSet<ISymbol>? TAuditPuppetNames;

    internal static HashSet<ISymbol> TAuditReaderNames = new(SymbolEqualityComparer.Default);

    private static Dictionary<ISymbol, HashSet<int>> TAuditHotNames = new(SymbolEqualityComparer.Default);

    private static Dictionary<ISymbol, HashSet<int>> TAuditZeroingNames = new(SymbolEqualityComparer.Default);

    private static HashSet<ISymbol> TAuditZeroingReads = new(SymbolEqualityComparer.Default);

    private static void TAuditRelayRead(IReadOnlyList<TypeDeclarationSyntax> type)
    {
        TAuditRelayNames = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
        TAuditReaderNames = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
        TAuditHotNames = new Dictionary<ISymbol, HashSet<int>>(SymbolEqualityComparer.Default);
        TAuditPuppetNames = null;
        List<SyntaxNode> declared = type
            .SelectMany(part => part.DescendantNodesAndSelf().OfType<TypeDeclarationSyntax>())
            .SelectMany(part => part.Members)
            .Where(member => member is MethodDeclarationSyntax or PropertyDeclarationSyntax)
            .Cast<SyntaxNode>()
            .ToList();
        declared.AddRange(declared.SelectMany(member => member.DescendantNodes().OfType<LocalFunctionStatementSyntax>())
            .ToList());
        List<(ISymbol TAuditRelaySymbol, SyntaxNode TAuditRelayMember)> members = declared
            .Select(member => (TAuditBinderSymbol.TAuditSymbolRead(member), member))
            .Where(pair => pair.Item1 is not null)
            .Select(pair => (pair.Item1!, pair.member))
            .ToList();
        List<SyntaxNode> roots = TAuditBinder.TAuditTrees.Select(tree => tree.GetRoot()).ToList();
        List<AssignmentExpressionSyntax> wiring = roots
            .SelectMany(root => root.DescendantNodes().OfType<AssignmentExpressionSyntax>())
            .Where(assignment => assignment.IsKind(SyntaxKind.SimpleAssignmentExpression)
                                 || assignment.IsKind(SyntaxKind.AddAssignmentExpression))
            .ToList();
        List<VariableDeclaratorSyntax> locals = roots
            .SelectMany(root => root.DescendantNodes().OfType<VariableDeclaratorSyntax>())
            .Where(declarator => declarator.Initializer is not null)
            .ToList();
        Dictionary<ISymbol, List<ExpressionSyntax>> seams = TAuditSeamRead(roots);
        bool grown = true;
        while (grown)
        {
            grown = false;
            foreach ((ISymbol symbol, SyntaxNode member) in members)
            {
                if (!TAuditRelayNames.Contains(symbol) && TAuditRequestCheck(member))
                {
                    TAuditRelayNames.Add(symbol);
                    grown = true;
                }

                if (!TAuditReaderNames.Contains(symbol) && (TAuditReadCheck(member) || TAuditRequestCheck(member)))
                {
                    TAuditReaderNames.Add(symbol);
                    grown = true;
                }

                ParameterListSyntax? parameters = member switch
                {
                    MethodDeclarationSyntax method => method.ParameterList,
                    LocalFunctionStatementSyntax local => local.ParameterList,
                    _ => null
                };
                if (parameters is not null && TAuditHotRead(symbol, member, parameters))
                {
                    grown = true;
                }
            }

            foreach (VariableDeclaratorSyntax local in locals)
            {
                if (TAuditBinderSymbol.TAuditSymbolRead(local) is ILocalSymbol { Type.TypeKind: TypeKind.Delegate } held
                    && !TAuditRelayNames.Contains(held)
                    && TAuditSeamCheck(local.Initializer!.Value, seams, new(SymbolEqualityComparer.Default)))
                {
                    TAuditRelayNames.Add(held);
                    grown = true;
                }
            }

            foreach (AssignmentExpressionSyntax assignment in wiring)
            {
                if (TAuditBinderSymbol.TAuditSymbolRead(assignment.Left) is { } held
                    && held switch
                    {
                        IFieldSymbol field => field.Type,
                        IEventSymbol happening => happening.Type,
                        IPropertySymbol property => property.Type,
                        ILocalSymbol local => local.Type,
                        _ => null
                    } is { TypeKind: TypeKind.Delegate }
                    && (TAuditBinderSide.TAuditShellCheck(held.ContainingType)
                        || held is IEventSymbol { ContainingType.TypeKind: TypeKind.Interface })
                    && (held is not IEventSymbol || TAuditPuppetNames is not null)
                    && !TAuditRelayNames.Contains(held)
                    && TAuditSeamCheck(assignment.Right, seams, new(SymbolEqualityComparer.Default)))
                {
                    TAuditRelayNames.Add(held);
                    grown = true;
                }
            }

            if (!grown && TAuditPuppetNames is null)
            {
                TAuditPuppetNames = new HashSet<ISymbol>(TAuditRelayNames, SymbolEqualityComparer.Default);
                grown = true;
            }
        }

        TAuditZeroingRead(type);
    }

    private static Dictionary<ISymbol, List<ExpressionSyntax>> TAuditSeamRead(IReadOnlyList<SyntaxNode> roots)
    {
        Dictionary<ISymbol, List<ExpressionSyntax>> seams = new(SymbolEqualityComparer.Default);
        foreach (ArgumentSyntax argument in roots.SelectMany(root => root.DescendantNodes().OfType<ArgumentSyntax>()))
        {
            if (TAuditParameterRead(argument) is not { Type.TypeKind: TypeKind.Delegate } parameter)
            {
                continue;
            }

            if (!seams.TryGetValue(parameter, out List<ExpressionSyntax>? passed))
            {
                passed = [];
                seams[parameter] = passed;
            }

            passed.Add(argument.Expression);
        }

        return seams;
    }

    private static bool TAuditSeamCheck(
        ExpressionSyntax value, IReadOnlyDictionary<ISymbol, List<ExpressionSyntax>> seams, HashSet<ISymbol> seen)
    {
        return TAuditDelegateCheck(value)
               || (TAuditBinderSymbol.TAuditSymbolRead(value) is IParameterSymbol parameter
                   && seen.Add(parameter)
                   && seams.TryGetValue(parameter, out List<ExpressionSyntax>? passed)
                   && passed.Any(argument => TAuditSeamCheck(argument, seams, seen)));
    }

    private static IParameterSymbol? TAuditParameterRead(ArgumentSyntax argument)
    {
        if (argument.Parent is not BaseArgumentListSyntax { Parent: { } call } list
            || call is not (ExpressionSyntax or ConstructorInitializerSyntax or PrimaryConstructorBaseTypeSyntax)
            || TAuditBinderSymbol.TAuditSymbolRead(call) is not IMethodSymbol callee)
        {
            return null;
        }

        return argument.NameColon is { Name.Identifier.ValueText: var name }
            ? callee.Parameters.FirstOrDefault(parameter => parameter.Name == name)
            : callee.Parameters.ElementAtOrDefault(list.Arguments.IndexOf(argument));
    }

    private static bool TAuditDelegateCheck(ExpressionSyntax value)
    {
        return TAuditRequestCheck(value)
               || value.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().Any(name =>
                   name.Parent is not InvocationExpressionSyntax
                   && TAuditBinderSymbol.TAuditSymbolRead(name) is { } symbol
                   && (TAuditRelayNames.Contains(symbol)
                       || (symbol is IMethodSymbol method && TAuditBinderSide.TAuditLogicCheck(method))));
    }

    private static bool TAuditHotRead(ISymbol symbol, SyntaxNode method, ParameterListSyntax list)
    {
        if (!TAuditHotNames.TryGetValue(symbol, out HashSet<int>? hot))
        {
            hot = [];
            TAuditHotNames[symbol] = hot;
        }

        List<ISymbol?> parameters = list.Parameters
            .Select(parameter => TAuditBinderSymbol.TAuditSymbolRead(parameter))
            .ToList();
        bool grown = false;
        foreach (ArgumentSyntax argument in method.DescendantNodes().OfType<ArgumentSyntax>())
        {
            if (argument.Parent?.Parent is not ExpressionSyntax call
                || TAuditCallRead(call) is not { } callee
                || !TAuditHotCheck(callee, argument))
            {
                continue;
            }

            foreach (IdentifierNameSyntax used in argument.Expression.DescendantNodesAndSelf()
                         .OfType<IdentifierNameSyntax>())
            {
                ISymbol? usedSymbol = TAuditBinderSymbol.TAuditSymbolRead(used);
                int index = usedSymbol is null
                    ? -1
                    : parameters.FindIndex(parameter => SymbolEqualityComparer.Default.Equals(parameter, usedSymbol));
                if (index >= 0 && hot.Add(index))
                {
                    grown = true;
                }
            }
        }

        return grown;
    }

    private static void TAuditZeroingRead(IReadOnlyList<TypeDeclarationSyntax> type)
    {
        TAuditZeroingNames = TAuditHotNames.ToDictionary(
            pair => pair.Key, pair => new HashSet<int>(pair.Value), SymbolEqualityComparer.Default);
        TAuditZeroingReads = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
        List<TypeDeclarationSyntax> nested = type
            .SelectMany(part => part.DescendantNodesAndSelf().OfType<TypeDeclarationSyntax>())
            .ToList();
        List<SyntaxNode> declared = nested
            .SelectMany(part => part.Members)
            .Where(member => member is BaseMethodDeclarationSyntax or PropertyDeclarationSyntax)
            .Cast<SyntaxNode>()
            .ToList();
        declared.AddRange(declared.SelectMany(member => member.DescendantNodes().OfType<LocalFunctionStatementSyntax>())
            .ToList());
        List<ArgumentSyntax> arguments = type
            .SelectMany(part => part.DescendantNodes().OfType<ArgumentSyntax>())
            .ToList();
        Dictionary<ISymbol, HashSet<ISymbol>> aliases = new(SymbolEqualityComparer.Default);
        bool grown = true;
        while (grown)
        {
            grown = false;
            foreach (IdentifierNameSyntax used in arguments
                         .Where(argument => TAuditZeroingCheck(argument, out _))
                         .SelectMany(argument => argument.Expression.DescendantNodesAndSelf()
                             .OfType<IdentifierNameSyntax>()))
            {
                if (TAuditBinderSymbol.TAuditSymbolRead(used) is { Kind: SymbolKind.Field or SymbolKind.Property } held
                    && TAuditBinderSide.TAuditShellCheck(held.ContainingType)
                    && TAuditZeroingReads.Add(held))
                {
                    grown = true;
                }
            }

            foreach (SyntaxNode member in declared)
            {
                ParameterListSyntax? list = member switch
                {
                    BaseMethodDeclarationSyntax method => method.ParameterList,
                    LocalFunctionStatementSyntax local => local.ParameterList,
                    _ => null
                };
                if (list is null || TAuditBinderSymbol.TAuditSymbolRead(member) is not { } symbol)
                {
                    continue;
                }

                if (!TAuditZeroingNames.TryGetValue(symbol, out HashSet<int>? hot))
                {
                    hot = [];
                    TAuditZeroingNames[symbol] = hot;
                }

                List<ISymbol?> parameters = list.Parameters
                    .Select(parameter => TAuditBinderSymbol.TAuditSymbolRead(parameter))
                    .ToList();
                IEnumerable<ExpressionSyntax> carried = member.DescendantNodes()
                    .OfType<ArgumentSyntax>()
                    .Where(argument => TAuditZeroingCheck(argument, out _))
                    .Select(argument => argument.Expression)
                    .Concat(member.DescendantNodes()
                        .OfType<AssignmentExpressionSyntax>()
                        .Where(assignment => assignment.IsKind(SyntaxKind.SimpleAssignmentExpression)
                                             && TAuditStoreRead(assignment.Left, nested, aliases)
                                                 .Overlaps(TAuditZeroingReads))
                        .Select(assignment => assignment.Right));
                foreach (IdentifierNameSyntax used in carried.SelectMany(value => value.DescendantNodesAndSelf()
                             .OfType<IdentifierNameSyntax>()))
                {
                    ISymbol? usedSymbol = TAuditBinderSymbol.TAuditSymbolRead(used);
                    int index = usedSymbol is null
                        ? -1
                        : parameters.FindIndex(
                            parameter => SymbolEqualityComparer.Default.Equals(parameter, usedSymbol));
                    if (index >= 0 && hot.Add(index))
                    {
                        grown = true;
                    }
                }
            }
        }
    }

    private static HashSet<ISymbol> TAuditStoreRead(
        ExpressionSyntax target,
        IReadOnlyList<TypeDeclarationSyntax> nested,
        Dictionary<ISymbol, HashSet<ISymbol>> aliases)
    {
        ISymbol? stored = TAuditBinderSymbol.TAuditSymbolRead(target);
        if (stored is not (IFieldSymbol or IPropertySymbol))
        {
            return [];
        }

        if (!aliases.TryGetValue(stored, out HashSet<ISymbol>? names))
        {
            names = stored is IFieldSymbol field
                ? TAuditAliasRead(field, nested
                    .Where(part => SymbolEqualityComparer.Default.Equals(
                        TAuditBinderSymbol.TAuditSymbolRead(part), field.ContainingType))
                    .ToList())
                : new HashSet<ISymbol>([stored], SymbolEqualityComparer.Default);
            aliases[stored] = names;
        }

        return names;
    }

    private static bool TAuditReadCheck(SyntaxNode member)
    {
        return member.DescendantNodes(node => !TAuditNameofCheck(node)).Any(node => node switch
        {
            MemberAccessExpressionSyntax or MemberBindingExpressionSyntax
                => TAuditBinderSide.TAuditLogicCheck(TAuditBinderSymbol.TAuditSymbolRead(node)),
            InvocationExpressionSyntax call
                => TAuditBinderSymbol.TAuditSymbolRead(call) is { } callee && TAuditReaderNames.Contains(callee),
            _ => false
        });
    }
}
