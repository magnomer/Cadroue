using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Convention.Tests;

internal static partial class TAuditTruthWalker
{
    private static HashSet<ISymbol> TAuditRelayNames = new(SymbolEqualityComparer.Default);

    private static HashSet<ISymbol> TAuditReaderNames = new(SymbolEqualityComparer.Default);

    private static Dictionary<ISymbol, HashSet<int>> TAuditHotNames = new(SymbolEqualityComparer.Default);

    private static void TAuditRelayRead(IReadOnlyList<TypeDeclarationSyntax> type)
    {
        TAuditRelayNames = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
        TAuditReaderNames = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
        TAuditHotNames = new Dictionary<ISymbol, HashSet<int>>(SymbolEqualityComparer.Default);
        List<SyntaxNode> declared = type
            .SelectMany(part => part.DescendantNodesAndSelf().OfType<TypeDeclarationSyntax>())
            .SelectMany(part => part.Members)
            .Where(member => member is MethodDeclarationSyntax or PropertyDeclarationSyntax)
            .Cast<SyntaxNode>()
            .ToList();
        declared.AddRange(declared.SelectMany(member => member.DescendantNodes().OfType<LocalFunctionStatementSyntax>())
            .ToList());
        List<(ISymbol TAuditRelaySymbol, SyntaxNode TAuditRelayMember)> members = declared
            .Select(member => (TAuditBinder.TAuditSymbolRead(member), member))
            .Where(pair => pair.Item1 is not null)
            .Select(pair => (pair.Item1!, pair.member))
            .ToList();
        List<AssignmentExpressionSyntax> wiring = type
            .SelectMany(part => part.DescendantNodes().OfType<AssignmentExpressionSyntax>())
            .Where(assignment => assignment.IsKind(SyntaxKind.SimpleAssignmentExpression)
                                 || assignment.IsKind(SyntaxKind.AddAssignmentExpression))
            .ToList();
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

            foreach (AssignmentExpressionSyntax assignment in wiring)
            {
                if (TAuditBinder.TAuditSymbolRead(assignment.Left) is { } held
                    && held switch
                    {
                        IFieldSymbol field => field.Type,
                        IEventSymbol happening => happening.Type,
                        IPropertySymbol property => property.Type,
                        _ => null
                    } is { TypeKind: TypeKind.Delegate }
                    && TAuditBinder.TAuditShellCheck(held.ContainingType)
                    && !TAuditRelayNames.Contains(held)
                    && TAuditDelegateCheck(assignment.Right))
                {
                    TAuditRelayNames.Add(held);
                    grown = true;
                }
            }
        }
    }

    private static bool TAuditDelegateCheck(ExpressionSyntax value)
    {
        return TAuditRequestCheck(value)
               || value.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().Any(name =>
                   name.Parent is not InvocationExpressionSyntax
                   && TAuditBinder.TAuditSymbolRead(name) is IMethodSymbol method
                   && (TAuditRelayNames.Contains(method) || TAuditBinder.TAuditLogicCheck(method)));
    }

    private static bool TAuditHotRead(ISymbol symbol, SyntaxNode method, ParameterListSyntax list)
    {
        if (!TAuditHotNames.TryGetValue(symbol, out HashSet<int>? hot))
        {
            hot = [];
            TAuditHotNames[symbol] = hot;
        }

        List<ISymbol?> parameters = list.Parameters
            .Select(parameter => TAuditBinder.TAuditSymbolRead(parameter))
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
                ISymbol? usedSymbol = TAuditBinder.TAuditSymbolRead(used);
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

    private static bool TAuditReadCheck(SyntaxNode member)
    {
        return member.DescendantNodes().Any(node => node switch
        {
            MemberAccessExpressionSyntax or MemberBindingExpressionSyntax
                => TAuditBinder.TAuditLogicCheck(TAuditBinder.TAuditSymbolRead(node)),
            InvocationExpressionSyntax call
                => TAuditBinder.TAuditSymbolRead(call) is { } callee && TAuditReaderNames.Contains(callee),
            _ => false
        });
    }

    private static bool TAuditHandleCheck(ITypeSymbol type)
    {
        string shown = type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        return TAuditBinder.TAuditConductCheck(type)
               || TAuditTruthSetting.TAuditTruthHandles.Contains(shown, StringComparer.Ordinal)
               || TAuditTruthSetting.TAuditTruthHandles.Contains(shown.TrimEnd('?'), StringComparer.Ordinal);
    }

    private static HashSet<ISymbol> TAuditAliasRead(IFieldSymbol field, IReadOnlyList<TypeDeclarationSyntax> type)
    {
        HashSet<ISymbol> symbols = new([field], SymbolEqualityComparer.Default);
        List<PropertyDeclarationSyntax> getters = type
            .SelectMany(part => part.DescendantNodesAndSelf().OfType<TypeDeclarationSyntax>())
            .SelectMany(part => part.Members.OfType<PropertyDeclarationSyntax>())
            .Where(property => property.AccessorList?.Accessors.All(accessor =>
                accessor.IsKind(SyntaxKind.GetAccessorDeclaration)) != false)
            .ToList();
        bool grown = true;
        while (grown)
        {
            grown = false;
            foreach (PropertyDeclarationSyntax property in getters)
            {
                if (TAuditBinder.TAuditSymbolRead(property) is IPropertySymbol alias
                    && !symbols.Contains(alias)
                    && !TAuditRequestCheck(property)
                    && TAuditNameCheck(property, symbols))
                {
                    symbols.Add(alias);
                    grown = true;
                }
            }
        }

        return symbols;
    }
}
