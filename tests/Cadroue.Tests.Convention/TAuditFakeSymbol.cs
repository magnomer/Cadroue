using Microsoft.CodeAnalysis;

namespace Convention.Tests;

internal static class TAuditFakeSymbol
{
    public static List<ISymbol> TAuditContractRead(ISymbol symbol)
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

    public static ISymbol TAuditNormalRead(ISymbol symbol)
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

    public static string TAuditKeyRead(ISymbol symbol)
    {
        ISymbol normal = TAuditNormalRead(symbol);
        return normal.GetDocumentationCommentId() ?? normal.ToDisplayString();
    }
}
