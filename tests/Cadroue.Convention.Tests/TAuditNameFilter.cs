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

    internal static bool TAuditContractCheck(SyntaxNode node, string name)
    {
        for (SyntaxNode? current = node.Parent; current is not null; current = current.Parent)
        {
            if (current is not TypeDeclarationSyntax type)
            {
                continue;
            }

            bool nameIsContract = TAuditFrameworkContracts.Values.Any(members => members.Contains(name));

            if (type.BaseList is not null)
            {
                foreach (BaseTypeSyntax baseType in type.BaseList.Types)
                {
                    if (TAuditFrameworkContracts.TryGetValue(
                            TAuditInterfaceRead(baseType.Type), out HashSet<string>? members) &&
                        members.Contains(name))
                    {
                        return true;
                    }
                }
            }

            return nameIsContract && type.Modifiers.Any(token => token.IsKind(SyntaxKind.PartialKeyword));
        }

        return false;
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
