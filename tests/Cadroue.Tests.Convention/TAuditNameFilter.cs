using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Convention.Tests;

internal static class TAuditNameFilter
{
    private static readonly Dictionary<string, HashSet<string>> TAuditFrameworkContracts =
        TAuditNameSetting.TAuditFrameworkContracts.ToDictionary(
            entry => entry.Key,
            entry => new HashSet<string>(entry.Value, StringComparer.Ordinal),
            StringComparer.Ordinal);

    private static Dictionary<string, List<TypeDeclarationSyntax>> TAuditTypeParts = new(StringComparer.Ordinal);

    private static Dictionary<string, List<string>> TAuditTypeKeys = new(StringComparer.Ordinal);

    internal static void TAuditTypeScan(IEnumerable<SyntaxTree> trees)
    {
        Dictionary<string, List<TypeDeclarationSyntax>> parts = new(StringComparer.Ordinal);
        Dictionary<string, List<string>> keys = new(StringComparer.Ordinal);
        foreach (TypeDeclarationSyntax type in trees.SelectMany(tree =>
                     tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>()))
        {
            string key = TAuditKeyRead(type);
            if (!parts.TryGetValue(key, out List<TypeDeclarationSyntax>? known))
            {
                known = [];
                parts[key] = known;
                string simple = type.Identifier.ValueText;
                if (!keys.TryGetValue(simple, out List<string>? named))
                {
                    named = [];
                    keys[simple] = named;
                }

                named.Add(key);
            }

            known.Add(type);
        }

        TAuditTypeParts = parts;
        TAuditTypeKeys = keys;
    }

    internal static bool TAuditContractCheck(SyntaxNode node, string name)
    {
        TypeDeclarationSyntax? type = node.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();
        return type is not null
            && TAuditContractFind(TAuditKeyRead(type), name, new HashSet<string>(StringComparer.Ordinal));
    }

    private static bool TAuditContractFind(string key, string name, HashSet<string> visited)
    {
        if (!visited.Add(key) || !TAuditTypeParts.TryGetValue(key, out List<TypeDeclarationSyntax>? parts))
        {
            return false;
        }

        foreach (BaseTypeSyntax baseType in parts.SelectMany(part => part.BaseList?.Types ?? []))
        {
            string simple = TAuditInterfaceRead(baseType.Type);
            if (TAuditFrameworkContracts.TryGetValue(simple, out HashSet<string>? members) && members.Contains(name))
            {
                return true;
            }

            if (TAuditTypeKeys.TryGetValue(simple, out List<string>? keys)
                && keys.Any(baseKey => TAuditContractFind(baseKey, name, visited)))
            {
                return true;
            }
        }

        return false;
    }

    private static string TAuditKeyRead(TypeDeclarationSyntax type)
    {
        List<string> segments = [];
        for (SyntaxNode? current = type; current is not null; current = current.Parent)
        {
            if (current is TypeDeclarationSyntax declaration)
            {
                int arity = declaration.TypeParameterList?.Parameters.Count ?? 0;
                segments.Add($"{declaration.Identifier.ValueText}`{arity}");
            }
            else if (current is BaseNamespaceDeclarationSyntax space)
            {
                segments.Add(space.Name.ToString());
            }
        }

        segments.Reverse();
        return string.Join(".", segments);
    }

    private static string TAuditInterfaceRead(TypeSyntax type) => type switch
    {
        GenericNameSyntax generic => generic.Identifier.ValueText,
        QualifiedNameSyntax qualified => TAuditInterfaceRead(qualified.Right),
        AliasQualifiedNameSyntax alias => TAuditInterfaceRead(alias.Name),
        IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
        _ => string.Empty
    };

    internal static bool TAuditExternalCheck(
        SyntaxTokenList modifiers,
        ExplicitInterfaceSpecifierSyntax? explicitInterface,
        SyntaxList<AttributeListSyntax> attributes)
    {
        if (explicitInterface is not null ||
            modifiers.Any(token => token.IsKind(SyntaxKind.OverrideKeyword)) ||
            modifiers.Any(token => token.IsKind(SyntaxKind.ExternKeyword)))
        {
            return true;
        }

        return TAuditGeneratedCheck(attributes) ||
               TAuditAttributesCheck(attributes, TAuditNameSetting.TAuditExternalAttributes);
    }

    internal static bool TAuditGeneratedCheck(SyntaxList<AttributeListSyntax> attributes) =>
        TAuditAttributesCheck(attributes, TAuditNameSetting.TAuditGeneratedAttributes);

    internal static bool TAuditAttributesCheck(SyntaxList<AttributeListSyntax> attributes, string[] expectedNames)
    {
        foreach (string expectedName in expectedNames)
        {
            if (TAuditAttributeCheck(attributes, expectedName))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TAuditAttributeCheck(SyntaxList<AttributeListSyntax> attributes, string expectedName)
    {
        foreach (AttributeSyntax attribute in attributes.SelectMany(list => list.Attributes))
        {
            string name = attribute.Name.ToString();
            int separator = name.LastIndexOf('.');
            if (separator >= 0)
            {
                name = name[(separator + 1)..];
            }

            if (name.EndsWith("Attribute", StringComparison.Ordinal))
            {
                name = name[..^"Attribute".Length];
            }

            if (string.Equals(name, expectedName, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    internal static string? TAuditPrefixRead(string name)
    {
        foreach (string prefix in TAuditNameSetting.TAuditPrefixes)
        {
            if (!name.StartsWith(prefix, StringComparison.Ordinal) || name.Length == prefix.Length)
            {
                continue;
            }

            char next = name[prefix.Length];
            if (char.IsUpper(next) || char.IsDigit(next) || next == '_')
            {
                return prefix;
            }
        }

        return null;
    }
}
