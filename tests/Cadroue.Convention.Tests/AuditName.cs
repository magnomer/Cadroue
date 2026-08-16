using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cadroue.Convention.Tests;

internal sealed record NameViolation(string Path, int Line, string Name, string Kind, string Reason);

internal static class AuditName
{
    private static readonly CSharpParseOptions ParseOptions = new(
        languageVersion: LanguageVersion.Preview,
        documentationMode: DocumentationMode.None,
        kind: SourceCodeKind.Regular);

    private static readonly Regex ComponentPattern = new(
        "[A-Z]+(?=[A-Z][a-z]|[0-9]|$)|[A-Z]?[a-z]+|[0-9]+",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly HashSet<string> MethodKinds =
        new(StringComparer.Ordinal) { "Method", "LocalFunction" };

    private static readonly HashSet<string> VerbForbiddenKinds =
        new(StringComparer.Ordinal) { "Field", "Property", "EnumMember", "RecordProperty", "XamlName" };

    public static IReadOnlyList<NameViolation> Audit(IEnumerable<string> sourcePaths, AuditRegistry registry)
    {
        List<NameViolation> violations = [];
        foreach (string path in sourcePaths)
        {
            if (path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                AuditCSharp(path, registry, violations);
            }
            else if (path.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase))
            {
                AuditXaml(path, registry, violations);
            }
        }

        return violations;
    }

    private static void AuditCSharp(string path, AuditRegistry registry, List<NameViolation> violations)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(File.ReadAllText(path), ParseOptions, path);
        SyntaxNode root = tree.GetRoot();

        foreach (SyntaxNode node in root.DescendantNodesAndSelf())
        {
            (SyntaxToken Identifier, string Kind)? candidate = node switch
            {
                BaseTypeDeclarationSyntax type when !HasGeneratedAttribute(type.AttributeLists)
                    => (type.Identifier, type.Kind().ToString()),
                ParameterSyntax parameter
                    when parameter.Parent?.Parent is RecordDeclarationSyntax record &&
                         !HasGeneratedAttribute(record.AttributeLists)
                    => (parameter.Identifier, "RecordProperty"),
                DelegateDeclarationSyntax del when !HasGeneratedAttribute(del.AttributeLists)
                    => (del.Identifier, "Delegate"),
                MethodDeclarationSyntax method
                    when !IsExternal(method.Modifiers, method.ExplicitInterfaceSpecifier, method.AttributeLists)
                    => (method.Identifier, "Method"),
                LocalFunctionStatementSyntax local => (local.Identifier, "LocalFunction"),
                PropertyDeclarationSyntax property
                    when !IsExternal(property.Modifiers, property.ExplicitInterfaceSpecifier, property.AttributeLists)
                    => (property.Identifier, "Property"),
                EventDeclarationSyntax evt
                    when !IsExternal(evt.Modifiers, evt.ExplicitInterfaceSpecifier, evt.AttributeLists)
                    => (evt.Identifier, "Event"),
                VariableDeclaratorSyntax variable => VariableCandidate(variable),
                EnumMemberDeclarationSyntax enumMember => (enumMember.Identifier, "EnumMember"),
                _ => null
            };

            if (candidate is null)
            {
                continue;
            }

            string name = candidate.Value.Identifier.ValueText;
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            string? reason = Analyze(name, candidate.Value.Kind, registry);
            if (reason is not null)
            {
                int line = tree.GetLineSpan(candidate.Value.Identifier.Span).StartLinePosition.Line + 1;
                violations.Add(new NameViolation(path, line, name, candidate.Value.Kind, reason));
            }
        }
    }

    private static (SyntaxToken Identifier, string Kind)? VariableCandidate(VariableDeclaratorSyntax variable)
    {
        if (variable.Parent is not VariableDeclarationSyntax declaration)
        {
            return null;
        }

        return declaration.Parent switch
        {
            EventFieldDeclarationSyntax eventField when !HasGeneratedAttribute(eventField.AttributeLists)
                => (variable.Identifier, "EventField"),
            FieldDeclarationSyntax field when !HasGeneratedAttribute(field.AttributeLists)
                => (variable.Identifier, "Field"),
            _ => null
        };
    }

    private static void AuditXaml(string path, AuditRegistry registry, List<NameViolation> violations)
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
                 attribute.Name.NamespaceName == "http://schemas.microsoft.com/winfx/2006/xaml"));

            if (nameAttribute is null || string.IsNullOrWhiteSpace(nameAttribute.Value))
            {
                continue;
            }

            string? reason = Analyze(nameAttribute.Value, "XamlName", registry);
            if (reason is not null)
            {
                int line = nameAttribute is IXmlLineInfo info && info.HasLineInfo() ? info.LineNumber : 0;
                violations.Add(new NameViolation(path, line, nameAttribute.Value, "XamlName", reason));
            }
        }
    }

    private static string? Analyze(string name, string kind, AuditRegistry registry)
    {
        string working = name.TrimStart('_');
        string? prefix = ReadPrefix(working);
        if (prefix is null)
        {
            return null;
        }

        string remainder = working[prefix.Length..];
        if (string.IsNullOrWhiteSpace(remainder))
        {
            return null;
        }

        List<string> components = [];
        foreach (string segment in remainder.Split('_'))
        {
            if (string.IsNullOrWhiteSpace(segment))
            {
                return null;
            }

            MatchCollection matches = ComponentPattern.Matches(segment);
            string rebuilt = string.Concat(matches.Cast<Match>().Select(match => match.Value));
            if (!string.Equals(rebuilt, segment, StringComparison.Ordinal))
            {
                return null;
            }

            components.AddRange(matches.Cast<Match>().Select(match => match.Value));
        }

        if (components.Count == 0)
        {
            return null;
        }

        bool isXInstance = working.Contains('_') && registry.XInstances.Contains(working);
        bool exempt = registry.Exempt.Contains(working);

        if (exempt)
        {
            return null;
        }

        string baseName = components[0];
        if (!registry.Bases.Contains(baseName))
        {
            return $"unregistered base `{baseName}`";
        }

        if (!isXInstance)
        {
            string last = components[^1];
            bool lastIsVerb = registry.Verbs.Contains(last);
            if (MethodKinds.Contains(kind) && !lastIsVerb)
            {
                return $"method does not end in a registered verb (`{last}`)";
            }

            if (VerbForbiddenKinds.Contains(kind) && lastIsVerb)
            {
                return $"data or type name ends in a registered verb (`{last}`)";
            }

            if (components.Count > 3)
            {
                return $"{components.Count} components after the prefix (limit is three)";
            }
        }

        return null;
    }

    private static bool IsExternal(
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

        return HasGeneratedAttribute(attributes) ||
               HasAttribute(attributes, "DllImport") ||
               HasAttribute(attributes, "LibraryImport");
    }

    private static bool HasGeneratedAttribute(SyntaxList<AttributeListSyntax> attributes) =>
        HasAttribute(attributes, "GeneratedCode") || HasAttribute(attributes, "CompilerGenerated");

    private static bool HasAttribute(SyntaxList<AttributeListSyntax> attributes, string expectedName)
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

    private static string? ReadPrefix(string name)
    {
        foreach (string prefix in new[] { "PS", "LS", "ps", "ls", "P", "L", "p", "l" })
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
