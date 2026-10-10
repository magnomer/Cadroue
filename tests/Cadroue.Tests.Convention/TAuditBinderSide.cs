using Microsoft.CodeAnalysis;

namespace Convention.Tests;

internal static class TAuditBinderSide
{
    private const string TAuditShellSide = "shell";

    private const string TAuditConductSide = "conduct";

    private const string TAuditEngineSide = "engine";

    private static readonly string[] TAuditLogicSides = [TAuditConductSide, TAuditEngineSide];

    public static bool TAuditLogicCheck(SyntaxNode node)
    {
        ISymbol? symbol = TAuditBinderSymbol.TAuditSymbolRead(node);
        return TAuditLogicCheck(symbol) || TAuditLogicCheck(TAuditBinderSymbol.TAuditTypeRead(node));
    }

    public static bool TAuditLogicCheck(ISymbol? symbol) => TAuditDepthCheck(symbol, TAuditLogicSides);

    public static bool TAuditLogicCheck(ITypeSymbol? type) => TAuditDepthCheck(type, TAuditLogicSides);

    public static bool TAuditEngineCheck(SyntaxNode node)
    {
        return TAuditDepthCheck(TAuditBinderSymbol.TAuditSymbolRead(node), [TAuditEngineSide])
               || TAuditDepthCheck(TAuditBinderSymbol.TAuditTypeRead(node), [TAuditEngineSide]);
    }

    public static bool TAuditEngineCheck(ISymbol? symbol) => TAuditDepthCheck(symbol, [TAuditEngineSide]);

    public static bool TAuditEngineCheck(ITypeSymbol? type) => TAuditDepthCheck(type, [TAuditEngineSide]);

    public static bool TAuditConductCheck(ITypeSymbol? type)
    {
        return type is INamedTypeSymbol named && TAuditSideRead(named) == TAuditConductSide
               && named.TypeArguments.All(TAuditConductCheck);
    }

    public static bool TAuditShellCheck(ITypeSymbol? type) => TAuditDepthCheck(type, [TAuditShellSide]);

    public static bool TAuditSurfaceCheck(ITypeSymbol? type)
    {
        string? source = type is INamedTypeSymbol named
            ? TAuditBinderSymbol.TAuditSourceRead(named.OriginalDefinition)
            : null;
        return source is not null && TAuditBinder.TAuditRootRead(TAuditStrictSetting.TAuditVeneerInclude)
            .Any(folder => source.StartsWith(folder + "/", StringComparison.OrdinalIgnoreCase));
    }

    private static bool TAuditDepthCheck(ISymbol? symbol, string[] sides)
    {
        return symbol switch
        {
            null => false,
            ILocalSymbol local => TAuditDepthCheck(local.Type, sides),
            IParameterSymbol parameter => TAuditDepthCheck(parameter.Type, sides),
            ITypeSymbol type => TAuditDepthCheck(type, sides),
            _ => sides.Contains(TAuditSideRead(symbol.ContainingType))
        };
    }

    private static bool TAuditDepthCheck(ITypeSymbol? type, string[] sides)
    {
        return type switch
        {
            null => false,
            IArrayTypeSymbol array => TAuditDepthCheck(array.ElementType, sides),
            INamedTypeSymbol named => sides.Contains(TAuditSideRead(named))
                                      || named.TypeArguments.Any(argument => TAuditDepthCheck(argument, sides)),
            _ => false
        };
    }

    public static IReadOnlySet<string> TAuditDeportmentRead()
    {
        HashSet<string> names = new(StringComparer.Ordinal);
        INamespaceSymbol? space = TAuditBinder.TAuditCompilation.Assembly.GlobalNamespace;
        foreach (string part in TAuditStrictSetting.TAuditDeportmentNamespace.Split('.'))
        {
            space = space?.GetNamespaceMembers()
                .FirstOrDefault(member => string.Equals(member.Name, part, StringComparison.Ordinal));
        }

        foreach (INamedTypeSymbol type in space?.GetTypeMembers() ?? [])
        {
            names.Add(type.Name);
            names.UnionWith(type.GetMembers().Select(member => member.Name));
        }

        Stack<INamespaceOrTypeSymbol> pending = new([TAuditBinder.TAuditCompilation.Assembly.GlobalNamespace]);
        while (pending.TryPop(out INamespaceOrTypeSymbol? current))
        {
            foreach (INamespaceOrTypeSymbol child in current.GetMembers().OfType<INamespaceOrTypeSymbol>())
            {
                pending.Push(child);
            }

            if (current is INamedTypeSymbol deeper && TAuditLogicSides.Contains(TAuditSideRead(deeper)))
            {
                names.Remove(deeper.Name);
                names.ExceptWith(deeper.GetMembers().Select(member => member.Name));
            }
        }

        return names;
    }

    private static string? TAuditSideRead(INamedTypeSymbol? type)
    {
        string? source = type is null ? null : TAuditBinderSymbol.TAuditSourceRead(type.OriginalDefinition);
        if (source is null)
        {
            return null;
        }

        if (TAuditBinder.TAuditRootRead(
                [.. TAuditTruthSetting.TAuditShellInclude, .. TAuditTruthSetting.TAuditCapsuleInclude])
            .Any(folder => source.StartsWith(folder + "/", StringComparison.OrdinalIgnoreCase)))
        {
            return TAuditShellSide;
        }

        return source.StartsWith(TAuditTruthSetting.TAuditConductRoot + "/", StringComparison.OrdinalIgnoreCase)
            ? TAuditConductSide
            : TAuditEngineSide;
    }
}
