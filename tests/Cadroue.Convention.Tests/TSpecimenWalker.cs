using System.Xml;
using System.Xml.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Convention.Tests;

internal static class TSpecimenWalker
{
    private static readonly CSharpParseOptions TAuditSyntaxOptions = new(
        languageVersion: LanguageVersion.Preview,
        documentationMode: DocumentationMode.None,
        kind: SourceCodeKind.Regular);

    internal static void TSpecimenCodeRead(string path, List<TSpecimen> candidates)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(File.ReadAllText(path), TAuditSyntaxOptions, path);
        SyntaxNode root = tree.GetRoot();

        foreach (SyntaxNode node in root.DescendantNodesAndSelf())
        {
            (SyntaxToken TSpecimenIdentifier, string TSpecimenKind)? candidate = node switch
            {
                BaseTypeDeclarationSyntax type when !TAuditGeneratedCheck(type.AttributeLists)
                    => (type.Identifier, type.Kind().ToString()),
                ParameterSyntax parameter
                    when parameter.Parent?.Parent is RecordDeclarationSyntax record &&
                         !TAuditGeneratedCheck(record.AttributeLists)
                    => (parameter.Identifier, "RecordProperty"),
                DelegateDeclarationSyntax del when !TAuditGeneratedCheck(del.AttributeLists)
                    => (del.Identifier, "Delegate"),
                MethodDeclarationSyntax method
                    when !TAuditExternalCheck(
                        method.Modifiers,
                        method.ExplicitInterfaceSpecifier,
                        method.AttributeLists) &&
                         !TAuditContractCheck(method, method.Identifier.ValueText)
                    => (method.Identifier,
                        TAuditAttributeCheck(method.AttributeLists, TAuditNameSetting.TAuditTestAttributes)
                            ? "TestMethod"
                            : "Method"),
                LocalFunctionStatementSyntax local => (local.Identifier, "LocalFunction"),
                PropertyDeclarationSyntax property
                    when !TAuditExternalCheck(
                        property.Modifiers,
                        property.ExplicitInterfaceSpecifier,
                        property.AttributeLists) &&
                         !TAuditContractCheck(property, property.Identifier.ValueText)
                    => (property.Identifier, "Property"),
                EventDeclarationSyntax evt
                    when !TAuditExternalCheck(evt.Modifiers, evt.ExplicitInterfaceSpecifier, evt.AttributeLists) &&
                         !TAuditContractCheck(evt, evt.Identifier.ValueText)
                    => (evt.Identifier, "Event"),
                TupleElementSyntax tupleElement => (tupleElement.Identifier, "TupleElement"),
                TypeParameterSyntax typeParameter => (typeParameter.Identifier, "TypeParameter"),
                AnonymousObjectMemberDeclaratorSyntax anonymousMember => TSpecimenAnonymousRead(anonymousMember),
                VariableDeclaratorSyntax variable => TSpecimenVariableRead(variable),
                EnumMemberDeclarationSyntax enumMember => (enumMember.Identifier, "EnumMember"),
                _ => null
            };

            if (candidate is null)
            {
                continue;
            }

            string name = candidate.Value.TSpecimenIdentifier.ValueText;
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            int line = tree.GetLineSpan(candidate.Value.TSpecimenIdentifier.Span).StartLinePosition.Line + 1;
            candidates.Add(new TSpecimen(path, line, name, candidate.Value.TSpecimenKind));

            if (node is MethodDeclarationSyntax commandMethod)
            {
                TSpecimenCommandRead(path, line, commandMethod, candidates);
            }
        }
    }

    private static void TSpecimenCommandRead(
        string path,
        int line,
        MethodDeclarationSyntax method,
        ICollection<TSpecimen> candidates)
    {
        AttributeSyntax? relayCommand = null;
        foreach (AttributeSyntax attribute in method.AttributeLists.SelectMany(list => list.Attributes))
        {
            string attributeName = attribute.Name.ToString();
            int separator = attributeName.LastIndexOf('.');
            if (separator >= 0)
            {
                attributeName = attributeName[(separator + 1)..];
            }

            if (attributeName.EndsWith("Attribute", StringComparison.Ordinal))
            {
                attributeName = attributeName[..^"Attribute".Length];
            }

            if (TAuditNameSetting.TAuditCommandAttributes.Contains(attributeName, StringComparer.Ordinal))
            {
                relayCommand = attribute;
                break;
            }
        }

        if (relayCommand is null)
        {
            return;
        }

        string stem = method.Identifier.ValueText;
        string asyncSuffix = TAuditNameSetting.TAuditCommandAsyncSuffix;
        if (stem.EndsWith(asyncSuffix, StringComparison.Ordinal) && stem.Length > asyncSuffix.Length)
        {
            stem = stem[..^asyncSuffix.Length];
        }

        candidates.Add(new TSpecimen(path, line, stem + TAuditNameSetting.TAuditCommandSuffix, "GeneratedCommand"));

        foreach (AttributeArgumentSyntax argument in relayCommand.ArgumentList?.Arguments ?? default)
        {
            if (argument.NameEquals?.Name.Identifier.ValueText == TAuditNameSetting.TAuditCommandCancelArgument &&
                argument.Expression.IsKind(SyntaxKind.TrueLiteralExpression))
            {
                candidates.Add(new TSpecimen(
                    path, line, stem + TAuditNameSetting.TAuditCommandCancelSuffix, "GeneratedCommand"));
            }
        }
    }

    private static (SyntaxToken TSpecimenIdentifier, string TSpecimenKind)? TSpecimenAnonymousRead(
        AnonymousObjectMemberDeclaratorSyntax member)
    {
        if (member.NameEquals is not null)
        {
            return (member.NameEquals.Name.Identifier, "AnonymousMember");
        }

        return member.Expression switch
        {
            IdentifierNameSyntax identifier => (identifier.Identifier, "AnonymousMember"),
            MemberAccessExpressionSyntax memberAccess => (memberAccess.Name.Identifier, "AnonymousMember"),
            _ => null
        };
    }

    private static (SyntaxToken TSpecimenIdentifier, string TSpecimenKind)? TSpecimenVariableRead(
        VariableDeclaratorSyntax variable)
    {
        if (variable.Parent is not VariableDeclarationSyntax declaration)
        {
            return null;
        }

        return declaration.Parent switch
        {
            EventFieldDeclarationSyntax eventField
                when !TAuditGeneratedCheck(eventField.AttributeLists) &&
                     !TAuditContractCheck(eventField, variable.Identifier.ValueText)
                => (variable.Identifier, "EventField"),
            FieldDeclarationSyntax field when !TAuditGeneratedCheck(field.AttributeLists)
                => (variable.Identifier, "Field"),
            _ => null
        };
    }

    internal static void TSpecimenMarkupRead(string path, List<TSpecimen> candidates)
    {
        using FileStream stream = File.OpenRead(path);
        using XmlReader reader = XmlReader.Create(stream, new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            IgnoreComments = false,
            IgnoreWhitespace = false
        });

        XDocument document = XDocument.Load(reader, LoadOptions.SetLineInfo | LoadOptions.PreserveWhitespace);
        if (document.Root is null)
        {
            return;
        }

        foreach (XElement element in document.Root.DescendantsAndSelf())
        {
            XAttribute? nameAttribute = element.Attributes().FirstOrDefault(attribute =>
                attribute.Name.LocalName == "Name" &&
                (string.IsNullOrEmpty(attribute.Name.NamespaceName) ||
                 attribute.Name.NamespaceName == TAuditNameSetting.TAuditXamlNamespace));

            if (nameAttribute is null || string.IsNullOrWhiteSpace(nameAttribute.Value))
            {
                continue;
            }

            int line = nameAttribute is IXmlLineInfo info && info.HasLineInfo() ? info.LineNumber : 0;
            candidates.Add(new TSpecimen(path, line, nameAttribute.Value, "XamlName"));
        }
    }

    private static readonly Dictionary<string, HashSet<string>> TAuditFrameworkContracts =
        TAuditNameSetting.TAuditFrameworkContracts.ToDictionary(
            entry => entry.Key,
            entry => new HashSet<string>(entry.Value, StringComparer.Ordinal),
            StringComparer.Ordinal);

    private static bool TAuditContractCheck(SyntaxNode node, string name)
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
                            TAuditInterfaceRead(baseType.Type),
                            out HashSet<string>? members) &&
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

    private static bool TAuditExternalCheck(
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
               TAuditAttributeCheck(attributes, TAuditNameSetting.TAuditExternalAttributes);
    }

    private static bool TAuditGeneratedCheck(SyntaxList<AttributeListSyntax> attributes) =>
        TAuditAttributeCheck(attributes, TAuditNameSetting.TAuditGeneratedAttributes);

    private static bool TAuditAttributeCheck(SyntaxList<AttributeListSyntax> attributes, string[] expectedNames)
    {
        foreach (string expectedName in expectedNames)
        {
            if (TAuditAttributeMatch(attributes, expectedName))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TAuditAttributeMatch(SyntaxList<AttributeListSyntax> attributes, string expectedName)
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
}
