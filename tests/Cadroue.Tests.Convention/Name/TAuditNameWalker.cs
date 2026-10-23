using System.Xml;
using System.Xml.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Convention.Tests;

internal static class TAuditNameWalker
{
    private static readonly CSharpParseOptions TAuditSyntaxOptions = new(
        languageVersion: LanguageVersion.Preview,
        documentationMode: DocumentationMode.None,
        kind: SourceCodeKind.Regular);

    public static IReadOnlyList<TViolation> TAuditRun(IEnumerable<string> sourcePaths, TAuditRegistry registry)
    {
        List<TSpecimen> candidates = TAuditSpecimenRead(sourcePaths);

        bool anyTestPrefixed = candidates.Any(candidate =>
            string.Equals(candidate.TSpecimenKind, "TestMethod", StringComparison.Ordinal) &&
            string.Equals(
                TAuditNameFilter.TAuditPrefixRead(candidate.TSpecimenName.TrimStart('_')),
                TAuditNameSetting.TAuditTestPrefix,
                StringComparison.OrdinalIgnoreCase));

        List<TViolation> violations = [];
        foreach (TSpecimen candidate in candidates)
        {
            if (registry.TAuditExemptValidate(candidate.TSpecimenName, candidate.TSpecimenPath))
            {
                continue;
            }

            string? reason = TAuditViolation.TViolationResolve(
                candidate.TSpecimenName, candidate.TSpecimenKind, registry, anyTestPrefixed);
            if (reason is not null)
            {
                violations.Add(new TViolation(
                    candidate.TSpecimenPath,
                    candidate.TSpecimenLine,
                    candidate.TSpecimenName,
                    candidate.TSpecimenKind,
                    reason));
            }
        }

        return violations;
    }

    public static List<TSpecimen> TAuditSpecimenRead(IEnumerable<string> sourcePaths)
    {
        List<TSpecimen> candidates = [];
        List<SyntaxTree> trees = sourcePaths
            .Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            .Select(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path), TAuditSyntaxOptions, path))
            .ToList();
        TAuditNameFilter.TAuditTypeScan(trees);
        foreach (SyntaxTree tree in trees)
        {
            TSpecimenCodeRead(tree, candidates);
        }

        foreach (string path in sourcePaths.Where(path => path.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase)))
        {
            TSpecimenMarkupRead(path, candidates);
        }

        return candidates;
    }

    private static void TSpecimenCodeRead(SyntaxTree tree, List<TSpecimen> candidates)
    {
        string path = tree.FilePath;
        SyntaxNode root = tree.GetRoot();

        foreach (SyntaxNode node in root.DescendantNodesAndSelf())
        {
            (SyntaxToken TSpecimenIdentifier, string TSpecimenKind)? candidate = node switch
            {
                BaseTypeDeclarationSyntax type when !TAuditNameFilter.TAuditGeneratedCheck(type.AttributeLists)
                    => (type.Identifier, type.Kind().ToString()),
                ParameterSyntax parameter
                    when parameter.Parent?.Parent is RecordDeclarationSyntax record &&
                         !TAuditNameFilter.TAuditGeneratedCheck(record.AttributeLists)
                    => (parameter.Identifier, "RecordProperty"),
                DelegateDeclarationSyntax del when !TAuditNameFilter.TAuditGeneratedCheck(del.AttributeLists)
                    => (del.Identifier, "Delegate"),
                MethodDeclarationSyntax method
                    when !TAuditNameFilter.TAuditExternalCheck(
                             method.Modifiers, method.ExplicitInterfaceSpecifier, method.AttributeLists) &&
                         !TAuditNameFilter.TAuditContractCheck(method, method.Identifier.ValueText)
                    => (method.Identifier,
                        TAuditNameFilter.TAuditAttributesCheck(
                            method.AttributeLists, TAuditNameSetting.TAuditTestAttributes)
                            ? "TestMethod"
                            : "Method"),
                LocalFunctionStatementSyntax local => (local.Identifier, "LocalFunction"),
                PropertyDeclarationSyntax property
                    when !TAuditNameFilter.TAuditExternalCheck(
                             property.Modifiers, property.ExplicitInterfaceSpecifier, property.AttributeLists) &&
                         !TAuditNameFilter.TAuditContractCheck(property, property.Identifier.ValueText)
                    => (property.Identifier, "Property"),
                EventDeclarationSyntax evt
                    when !TAuditNameFilter.TAuditExternalCheck(
                             evt.Modifiers, evt.ExplicitInterfaceSpecifier, evt.AttributeLists) &&
                         !TAuditNameFilter.TAuditContractCheck(evt, evt.Identifier.ValueText)
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
        string asyncSuffix = TAuditNameSetting.TAuditAsyncSuffix;
        if (stem.EndsWith(asyncSuffix, StringComparison.Ordinal) && stem.Length > asyncSuffix.Length)
        {
            stem = stem[..^asyncSuffix.Length];
        }

        candidates.Add(new TSpecimen(path, line, stem + TAuditNameSetting.TAuditCommandSuffix, "GeneratedCommand"));

        foreach (AttributeArgumentSyntax argument in relayCommand.ArgumentList?.Arguments ?? default)
        {
            if (argument.NameEquals?.Name.Identifier.ValueText == TAuditNameSetting.TAuditCancelArgument &&
                argument.Expression.IsKind(SyntaxKind.TrueLiteralExpression))
            {
                candidates.Add(new TSpecimen(
                    path, line, stem + TAuditNameSetting.TAuditCancelSuffix, "GeneratedCommand"));
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
                when !TAuditNameFilter.TAuditGeneratedCheck(eventField.AttributeLists) &&
                     !TAuditNameFilter.TAuditContractCheck(eventField, variable.Identifier.ValueText)
                => (variable.Identifier, "EventField"),
            FieldDeclarationSyntax field when !TAuditNameFilter.TAuditGeneratedCheck(field.AttributeLists)
                => (variable.Identifier, "Field"),
            _ => null
        };
    }

    private static void TSpecimenMarkupRead(string path, List<TSpecimen> candidates)
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
}
