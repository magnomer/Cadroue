using Microsoft.CodeAnalysis;
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
        List<(ISymbol TAuditRelaySymbol, MemberDeclarationSyntax TAuditRelayMember)> members = type
            .SelectMany(part => part.DescendantNodesAndSelf().OfType<TypeDeclarationSyntax>())
            .SelectMany(part => part.Members)
            .Where(member => member is MethodDeclarationSyntax or PropertyDeclarationSyntax)
            .Select(member => (TAuditBinder.TAuditSymbolRead(member), member))
            .Where(pair => pair.Item1 is not null)
            .Select(pair => (pair.Item1!, pair.member))
            .ToList();
        bool grown = true;
        while (grown)
        {
            grown = false;
            foreach ((ISymbol symbol, MemberDeclarationSyntax member) in members)
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

                if (member is MethodDeclarationSyntax hot && TAuditHotRead(symbol, hot))
                {
                    grown = true;
                }
            }
        }
    }

    private static bool TAuditHotRead(ISymbol symbol, MethodDeclarationSyntax method)
    {
        if (!TAuditHotNames.TryGetValue(symbol, out HashSet<int>? hot))
        {
            hot = [];
            TAuditHotNames[symbol] = hot;
        }

        List<ISymbol?> parameters = method.ParameterList.Parameters
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

    private static bool TAuditReadCheck(MemberDeclarationSyntax member)
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
}
