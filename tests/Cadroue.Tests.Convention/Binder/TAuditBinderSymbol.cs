using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Convention.Tests;

internal static class TAuditBinderSymbol
{
    private static readonly Dictionary<SyntaxNode, ISymbol?> TAuditSymbols = [];

    public static bool TAuditHeaderCheck(SimpleNameSyntax name)
    {
        SyntaxNode? parent = name.Parent;
        if (parent is QualifiedNameSyntax qualified)
        {
            parent = qualified.Parent;
        }

        return parent is UsingDirectiveSyntax or NamespaceDeclarationSyntax or FileScopedNamespaceDeclarationSyntax;
    }

    public static bool TAuditDataCheck(INamedTypeSymbol type) =>
        type.TypeKind is TypeKind.Enum or TypeKind.Struct or TypeKind.Delegate || type.IsRecord;

    public static INamedTypeSymbol? TAuditTypeRead(ISymbol symbol) => symbol switch
    {
        IAliasSymbol alias => TAuditTypeRead(alias.Target),
        INamedTypeSymbol named => named.IsTupleType || named.IsAnonymousType ? null : named.OriginalDefinition,
        IArrayTypeSymbol array => TAuditTypeRead(array.ElementType),
        INamespaceSymbol or ITypeParameterSymbol or ILocalSymbol or IParameterSymbol => null,
        IRangeVariableSymbol or IDiscardSymbol or ILabelSymbol or IPreprocessingSymbol => null,
        _ => symbol.ContainingType?.OriginalDefinition,
    };

    public static ISymbol? TAuditSymbolRead(SemanticModel model, SimpleNameSyntax name)
    {
        SymbolInfo info = model.GetSymbolInfo(name);
        return info.Symbol ?? info.CandidateSymbols.FirstOrDefault();
    }

    public static ISymbol? TAuditSymbolRead(SyntaxNode node)
    {
        if (node is ArgumentSyntax argument)
        {
            node = argument.Expression;
        }

        lock (TAuditSymbols)
        {
            if (TAuditSymbols.TryGetValue(node, out ISymbol? known))
            {
                return known;
            }
        }

        ISymbol? resolved = TAuditSymbolResolve(node);
        lock (TAuditSymbols)
        {
            TAuditSymbols[node] = resolved;
        }

        return resolved;
    }

    public static ITypeSymbol? TAuditTypeRead(SyntaxNode node)
    {
        SemanticModel model = TAuditBinder.TAuditModelRead(node);
        if (node is ArgumentSyntax argument)
        {
            node = argument.Expression;
        }

        if (node is ExpressionSyntax expression)
        {
            ITypeSymbol? type = model.GetTypeInfo(expression).Type;
            if (type is not null && type.TypeKind != TypeKind.Error)
            {
                return type;
            }
        }

        return TAuditSymbolRead(node) switch
        {
            ILocalSymbol local => local.Type,
            IParameterSymbol parameter => parameter.Type,
            IFieldSymbol field => field.Type,
            IPropertySymbol property => property.Type,
            IMethodSymbol method => method.ReturnType,
            ITypeSymbol type => type,
            _ => null
        };
    }

    public static string? TAuditSourceRead(INamedTypeSymbol type)
    {
        Location? source = type.Locations.FirstOrDefault(location => location.IsInSource);
        return source?.SourceTree is null ? null : TAuditBinder.TAuditRelativeRead(source.SourceTree.FilePath);
    }

    public static bool TAuditControlCheck(ITypeSymbol? type)
    {
        for (ITypeSymbol? current = type; current is not null; current = current.BaseType)
        {
            if (TAuditTruthSetting.TAuditControlBases.Contains(current.ToDisplayString(), StringComparer.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    public static bool TAuditMemberCheck(ISymbol? symbol, IReadOnlyList<string> members)
    {
        return symbol?.ContainingType is { } owner
               && members.Contains(
                   $"{owner.OriginalDefinition.ToDisplayString()}.{symbol.Name}", StringComparer.Ordinal);
    }

    public static bool TAuditNamedCheck(ITypeSymbol? type, IReadOnlyList<string> names)
    {
        return type is not null && names.Contains(type.Name, StringComparer.Ordinal);
    }

    public static string TAuditLabelRead(ISymbol symbol)
    {
        return symbol.ContainingType is null
            ? symbol.Name
            : $"{symbol.ContainingType.Name}.{symbol.Name}";
    }

    private static ISymbol? TAuditSymbolResolve(SyntaxNode node)
    {
        SemanticModel model = TAuditBinder.TAuditModelRead(node);
        ISymbol? symbol = node is ExpressionSyntax ? null : model.GetDeclaredSymbol(node);
        if (symbol is null)
        {
            SymbolInfo info = model.GetSymbolInfo(node);
            symbol = info.Symbol ?? info.CandidateSymbols.FirstOrDefault();
        }

        return symbol?.OriginalDefinition;
    }
}
