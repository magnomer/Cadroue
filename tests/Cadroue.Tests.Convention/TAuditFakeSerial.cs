using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Convention.Tests;

internal static class TAuditFakeSerial
{
    public static void TAuditPersistScan(SemanticModel model, HashSet<string> serialized)
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

        serialized.Add(TAuditFakeWalker.TAuditKeyRead(named));
        foreach (IPropertySymbol property in named.GetMembers().OfType<IPropertySymbol>())
        {
            serialized.Add(TAuditFakeWalker.TAuditKeyRead(property));
            TAuditPersistAdd(property.Type, serialized, seen);
        }
    }
}
