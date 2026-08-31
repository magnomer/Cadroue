using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cadroue.Convention.Tests;

internal sealed record TViolation(
    string TViolationPath,
    int TViolationLine,
    string TViolationName,
    string TViolationKind,
    string TViolationReason);

internal readonly record struct TSpecimen(
    string TSpecimenPath,
    int TSpecimenLine,
    string TSpecimenName,
    string TSpecimenKind);

internal static class TAuditName
{
    private static readonly CSharpParseOptions TAuditSyntaxOptions = new(
        languageVersion: LanguageVersion.Preview,
        documentationMode: DocumentationMode.None,
        kind: SourceCodeKind.Regular);

    private static readonly Regex TAuditComponentPattern = new(
        "[A-Z]+(?=[A-Z][a-z]|[0-9]|$)|[A-Z]?[a-z]+|[0-9]+",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly HashSet<string> TAuditMethodKinds =
        new(StringComparer.Ordinal) { "Method", "LocalFunction" };

    private static readonly HashSet<string> TAuditDataKinds =
        new(StringComparer.Ordinal) { "Field", "Property", "EnumMember", "RecordProperty", "XamlName" };

    public static IReadOnlyList<TViolation> TAuditRun(IEnumerable<string> sourcePaths, TAuditRegistry registry)
    {
        List<TSpecimen> candidates = [];
        foreach (string path in sourcePaths)
        {
            if (path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                TSpecimenCodeRead(path, candidates);
            }
            else if (path.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase))
            {
                TSpecimenMarkupRead(path, candidates);
            }
        }

        // The test suite either names every test method descriptively without a prefix
        // (all exempt) or commits to the T prefix on every one. A single T-prefixed test
        // method flips the whole suite into the prefix-required mode.
        bool anyTestPrefixed = candidates.Any(candidate =>
            string.Equals(candidate.TSpecimenKind, "TestMethod", StringComparison.Ordinal) &&
            string.Equals(TAuditPrefixRead(candidate.TSpecimenName.TrimStart('_')), "T", StringComparison.OrdinalIgnoreCase));

        List<TViolation> violations = [];
        foreach (TSpecimen candidate in candidates)
        {
            string? reason = TViolationResolve(
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

    private static void TSpecimenCodeRead(string path, List<TSpecimen> candidates)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(File.ReadAllText(path), TAuditSyntaxOptions, path);
        SyntaxNode root = tree.GetRoot();

        foreach (SyntaxNode node in root.DescendantNodesAndSelf())
        {
            (SyntaxToken Identifier, string Kind)? candidate = node switch
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
                    when !TAuditExternalCheck(method.Modifiers, method.ExplicitInterfaceSpecifier, method.AttributeLists) &&
                         !TAuditContractCheck(method, method.Identifier.ValueText)
                    => (method.Identifier,
                        TAuditAttributeCheck(method.AttributeLists, "Fact") || TAuditAttributeCheck(method.AttributeLists, "Theory")
                            ? "TestMethod"
                            : "Method"),
                LocalFunctionStatementSyntax local => (local.Identifier, "LocalFunction"),
                PropertyDeclarationSyntax property
                    when !TAuditExternalCheck(property.Modifiers, property.ExplicitInterfaceSpecifier, property.AttributeLists) &&
                         !TAuditContractCheck(property, property.Identifier.ValueText)
                    => (property.Identifier, "Property"),
                EventDeclarationSyntax evt
                    when !TAuditExternalCheck(evt.Modifiers, evt.ExplicitInterfaceSpecifier, evt.AttributeLists) &&
                         !TAuditContractCheck(evt, evt.Identifier.ValueText)
                    => (evt.Identifier, "Event"),
                VariableDeclaratorSyntax variable => TSpecimenVariableRead(variable),
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

            int line = tree.GetLineSpan(candidate.Value.Identifier.Span).StartLinePosition.Line + 1;
            candidates.Add(new TSpecimen(path, line, name, candidate.Value.Kind));
        }
    }

    private static (SyntaxToken Identifier, string Kind)? TSpecimenVariableRead(VariableDeclaratorSyntax variable)
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
                 attribute.Name.NamespaceName == "http://schemas.microsoft.com/winfx/2006/xaml"));

            if (nameAttribute is null || string.IsNullOrWhiteSpace(nameAttribute.Value))
            {
                continue;
            }

            int line = nameAttribute is IXmlLineInfo info && info.HasLineInfo() ? info.LineNumber : 0;
            candidates.Add(new TSpecimen(path, line, nameAttribute.Value, "XamlName"));
        }
    }

    private static string? TViolationResolve(string name, string kind, TAuditRegistry registry, bool anyTestPrefixed)
    {
        if (string.Equals(kind, "TestMethod", StringComparison.Ordinal))
        {
            // A test method carries a free-form scenario description, never an object base or
            // verb. While no test method uses the T prefix the whole suite is descriptive and
            // exempt; once any test method adopts T, every test method must carry it.
            if (!anyTestPrefixed)
            {
                return null;
            }

            string? testPrefix = TAuditPrefixRead(name.TrimStart('_'));
            if (testPrefix is null)
            {
                return "test method carries no prefix while other test methods use the T prefix";
            }

            if (!string.Equals(testPrefix, "T", StringComparison.OrdinalIgnoreCase))
            {
                return $"test method must use the T prefix, not `{testPrefix}`";
            }

            return null;
        }

        string working = name.TrimStart('_');
        string? prefix = TAuditPrefixRead(working);
        if (prefix is null)
        {
            // A codebase-owned name that survived the external/generated/framework-contract
            // filters but carries no prefix is a violation, not something to skip.
            return "missing required prefix";
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

            MatchCollection matches = TAuditComponentPattern.Matches(segment);
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

        string baseName = components[0];
        if (!registry.TAuditBases.Contains(baseName))
        {
            return $"unregistered base `{baseName}`";
        }

        string last = components[^1];
        bool lastIsVerb = registry.TAuditVerbs.Contains(last);
        if (TAuditMethodKinds.Contains(kind) && !lastIsVerb)
        {
            return $"method does not end in a registered verb (`{last}`)";
        }

        if (TAuditDataKinds.Contains(kind) && lastIsVerb)
        {
            return $"data or type name ends in a registered verb (`{last}`)";
        }

        if (components.Count > 3)
        {
            return $"{components.Count} components after the prefix (limit is three)";
        }

        return null;
    }

    private static readonly Dictionary<string, HashSet<string>> TAuditFrameworkContracts =
        new(StringComparer.Ordinal)
        {
            ["IDisposable"] = new(StringComparer.Ordinal) { "Dispose" },
            ["IAsyncDisposable"] = new(StringComparer.Ordinal) { "DisposeAsync" },
            ["INotifyPropertyChanged"] = new(StringComparer.Ordinal) { "PropertyChanged" },
            ["INotifyPropertyChanging"] = new(StringComparer.Ordinal) { "PropertyChanging" },
            ["IProgress"] = new(StringComparer.Ordinal) { "Report" },
            ["IValueConverter"] = new(StringComparer.Ordinal) { "Convert", "ConvertBack" },
            ["IMultiValueConverter"] = new(StringComparer.Ordinal) { "Convert", "ConvertBack" }
        };

    // A member whose name is a contract member of a framework interface the nearest
    // enclosing type declares is externally fixed by that interface — the same reason
    // an explicit interface implementation is exempt, applied to the implicit form.
    // For a partial type the interface may be declared in another fragment this file
    // cannot see, so a contract-member name is accepted on the name alone.
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
                    if (TAuditFrameworkContracts.TryGetValue(TAuditInterfaceRead(baseType.Type), out HashSet<string>? members) &&
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
               TAuditAttributeCheck(attributes, "DllImport") ||
               TAuditAttributeCheck(attributes, "LibraryImport");
    }

    private static bool TAuditGeneratedCheck(SyntaxList<AttributeListSyntax> attributes) =>
        TAuditAttributeCheck(attributes, "GeneratedCode") || TAuditAttributeCheck(attributes, "CompilerGenerated");

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

    private static string? TAuditPrefixRead(string name)
    {
        foreach (string prefix in new[] { "PS", "LS", "ps", "ls", "P", "L", "T", "p", "l", "t" })
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
