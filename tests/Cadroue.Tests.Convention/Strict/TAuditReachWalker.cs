using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Convention.Tests;

internal static class TAuditReachWalker
{
    private static readonly Regex TAuditLogicPattern = new(
        @"(?<![A-Za-z0-9_])(L[A-Z][A-Za-z0-9_]*)", RegexOptions.Compiled);

    private static readonly Regex TAuditStaticPattern = new(
        @"x:Static\s+(?:[A-Za-z0-9_]+:)?(L[A-Z][A-Za-z0-9_]*)", RegexOptions.Compiled);

    private static readonly Regex TAuditSlotPattern = new(
        $@"\b({string.Join('|', TAuditStrictSetting.TAuditBranchingSlots)})\s*=", RegexOptions.Compiled);

    private static readonly Regex TAuditTetheringPattern = new(
        @"\{\s*(?:([A-Za-z_][\w.]*):)?([A-Za-z_][\w.]*)", RegexOptions.Compiled);

    public static IReadOnlyList<TViolation> TAuditRun(IEnumerable<string> markupPaths)
    {
        List<TViolation> violations = [];
        IReadOnlySet<string> deportment = TAuditBinderSide.TAuditDeportmentRead();
        Dictionary<string, List<string>> spaces = TAuditSpaceRead();
        foreach (string path in markupPaths)
        {
            XDocument document;
            try
            {
                document = XDocument.Load(path, LoadOptions.SetLineInfo);
            }
            catch (XmlException failure)
            {
                violations.Add(new TViolation(
                    path, failure.LineNumber, "markup", "Overreaching", "does not parse as XML"));
                continue;
            }

            SortedDictionary<int, List<string>> hooks = [];
            foreach (XElement element in document.Descendants())
            {
                TAuditElementScan(path, element, deportment, violations);
                TAuditTetheringScan(element, spaces, hooks);
            }

            violations.AddRange(hooks.Select(hook => new TViolation(
                path,
                hook.Key,
                hook.Value[0],
                "Tethering",
                $"line hooks logic into markup: {string.Join(", ", hook.Value)}")));
        }

        return violations;
    }

    private static void TAuditTetheringScan(
        XElement element, Dictionary<string, List<string>> spaces, SortedDictionary<int, List<string>> hooks)
    {
        string name = element.Name.LocalName;
        int line = ((IXmlLineInfo)element).LineNumber;
        string property = name[(name.LastIndexOf('.') + 1)..];
        if (TAuditStrictSetting.TAuditTetheringElements.Contains(name, StringComparer.Ordinal)
            || (name.Contains('.', StringComparison.Ordinal)
                && TAuditStrictSetting.TAuditTetheringSlots.Contains(property, StringComparer.Ordinal))
            || TAuditTetheringCheck(element, spaces))
        {
            TAuditTetheringAdd(hooks, line, name);
        }

        foreach (XAttribute attribute in element.Attributes().Where(attribute => !attribute.IsNamespaceDeclaration))
        {
            line = ((IXmlLineInfo)attribute).LineNumber;
            if (TAuditStrictSetting.TAuditTetheringSlots.Contains(attribute.Name.LocalName, StringComparer.Ordinal))
            {
                TAuditTetheringAdd(hooks, line, attribute.Name.LocalName);
            }

            string shell = TAuditStrictSetting.TAuditVeneerNamespace + ".";
            if (attribute.Name.LocalName == "Class" && !attribute.Value.StartsWith(shell, StringComparison.Ordinal))
            {
                TAuditTetheringAdd(hooks, line, "x:Class");
            }

            string slot = (element.Attribute("Property")?.Value ?? string.Empty).Trim('(', ')');
            slot = slot[(slot.LastIndexOf('.') + 1)..];
            if (attribute.Name.LocalName == "Property"
                && TAuditStrictSetting.TAuditTetheringSlots.Contains(slot, StringComparer.Ordinal))
            {
                TAuditTetheringAdd(hooks, line, slot);
            }

            bool literal = !attribute.Value.StartsWith('{');
            if (literal
                && (TAuditStrictSetting.TAuditTetheringLiterals.Contains(
                        attribute.Name.LocalName, StringComparer.Ordinal)
                    || (attribute.Name.LocalName == "Value"
                        && TAuditStrictSetting.TAuditTetheringLiterals.Contains(slot, StringComparer.Ordinal))))
            {
                TAuditTetheringAdd(hooks, line, attribute.Name.LocalName == "Value" ? slot : attribute.Name.LocalName);
            }

            foreach (Match match in TAuditTetheringPattern.Matches(attribute.Value))
            {
                string prefix = match.Groups[1].Value;
                string extension = prefix.Length == 0 ? match.Groups[2].Value : $"{prefix}:{match.Groups[2].Value}";
                string space = prefix.Length == 0
                    ? string.Empty
                    : element.GetNamespaceOfPrefix(prefix)?.NamespaceName ?? string.Empty;
                if (TAuditStrictSetting.TAuditTetheringExtensions.Contains(extension, StringComparer.Ordinal)
                    || space.StartsWith("clr-namespace:", StringComparison.Ordinal))
                {
                    TAuditTetheringAdd(hooks, line, extension);
                }
            }
        }
    }

    private static void TAuditTetheringAdd(SortedDictionary<int, List<string>> hooks, int line, string marker)
    {
        if (!hooks.TryGetValue(line, out List<string>? markers))
        {
            markers = [];
            hooks[line] = markers;
        }

        markers.Add(marker);
    }

    private static bool TAuditTetheringCheck(XElement element, Dictionary<string, List<string>> spaces)
    {
        const string prefix = "clr-namespace:";
        string name = element.Name.LocalName;
        string uri = element.Name.NamespaceName;
        if (name.Contains('.', StringComparison.Ordinal))
        {
            return false;
        }

        IEnumerable<string> candidates = uri.StartsWith(prefix, StringComparison.Ordinal)
            ? [uri[prefix.Length..].Split(';')[0]]
            : spaces.GetValueOrDefault(uri) ?? [];
        foreach (string space in candidates)
        {
            for (INamedTypeSymbol? type = TAuditBinder.TAuditCompilation.GetTypeByMetadataName($"{space}.{name}");
                 type is not null;
                 type = type.BaseType)
            {
                if (type.Interfaces.Append(type).Any(shape =>
                        TAuditStrictSetting.TAuditTetheringTypes.Contains(
                            shape.ToDisplayString(), StringComparer.Ordinal)))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static Dictionary<string, List<string>> TAuditSpaceRead()
    {
        CSharpCompilation compilation = TAuditBinder.TAuditCompilation;
        Dictionary<string, List<string>> spaces = new(StringComparer.Ordinal);
        IEnumerable<AttributeData> attributes = compilation.References
            .Select(compilation.GetAssemblyOrModuleSymbol)
            .OfType<IAssemblySymbol>()
            .Append(compilation.Assembly)
            .SelectMany(assembly => assembly.GetAttributes());
        foreach (AttributeData attribute in attributes)
        {
            if (attribute.AttributeClass?.Name is not "XmlnsDefinitionAttribute"
                || attribute.ConstructorArguments is not [{ Value: string uri }, { Value: string space }, ..])
            {
                continue;
            }

            if (!spaces.TryGetValue(uri, out List<string>? list))
            {
                list = [];
                spaces[uri] = list;
            }

            list.Add(space);
        }

        return spaces;
    }

    public static IReadOnlyList<string> TAuditControlRead(IEnumerable<string> markupPaths)
    {
        List<string> names = [];
        foreach (string path in markupPaths)
        {
            XDocument document;
            try
            {
                document = XDocument.Load(path);
            }
            catch (XmlException)
            {
                continue;
            }

            names.AddRange(document.Descendants()
                .SelectMany(element => element.Attributes())
                .Where(attribute => string.Equals(attribute.Name.LocalName, "Name", StringComparison.Ordinal))
                .Select(attribute => attribute.Value));
        }

        return names;
    }

    private static void TAuditElementScan(
        string path,
        XElement element,
        IReadOnlySet<string> deportment,
        List<TViolation> violations)
    {
        TAuditBranchingScan(path, element, violations);
        foreach (XAttribute attribute in element.Attributes())
        {
            int line = ((IXmlLineInfo)attribute).LineNumber;
            if (attribute.IsNamespaceDeclaration)
            {
                TAuditNamespaceScan(path, line, attribute.Value, violations);
                continue;
            }

            if (string.Equals(attribute.Name.LocalName, "Class", StringComparison.Ordinal))
            {
                continue;
            }

            TAuditValueScan(path, line, attribute.Name.LocalName, attribute.Value, deportment, violations);
        }

        foreach (XText text in element.Nodes().OfType<XText>())
        {
            TAuditValueScan(path, ((IXmlLineInfo)text).LineNumber, "text", text.Value, deportment, violations);
        }
    }

    private static void TAuditBranchingScan(string path, XElement element, List<TViolation> violations)
    {
        string name = element.Name.LocalName;
        string property = name[(name.LastIndexOf('.') + 1)..];
        if (TAuditStrictSetting.TAuditBranchingElements.Contains(name, StringComparer.Ordinal))
        {
            violations.Add(new TViolation(
                path, ((IXmlLineInfo)element).LineNumber, name, "Branching", "markup branches on a condition"));
        }
        else if (name.Contains('.', StringComparison.Ordinal)
                 && TAuditStrictSetting.TAuditBranchingSlots.Contains(property, StringComparer.Ordinal))
        {
            violations.Add(new TViolation(
                path, ((IXmlLineInfo)element).LineNumber, name, "Branching", $"property element {property} computes"));
        }

        foreach (XAttribute attribute in element.Attributes().Where(attribute => !attribute.IsNamespaceDeclaration))
        {
            int line = ((IXmlLineInfo)attribute).LineNumber;
            string slot = attribute.Name.LocalName;
            if (TAuditStrictSetting.TAuditBranchingSlots.Contains(slot, StringComparer.Ordinal))
            {
                violations.Add(new TViolation(path, line, slot, "Branching", $"binding {slot} computes in markup"));
            }

            foreach (Match match in TAuditSlotPattern.Matches(attribute.Value))
            {
                string found = match.Groups[1].Value;
                violations.Add(new TViolation(path, line, found, "Branching", $"binding {found} computes in markup"));
            }
        }
    }

    private static void TAuditNamespaceScan(string path, int line, string value, List<TViolation> violations)
    {
        const string prefix = "clr-namespace:";
        if (!value.StartsWith(prefix, StringComparison.Ordinal))
        {
            return;
        }

        string mapped = value[prefix.Length..].Split(';')[0];
        if (TAuditStrictSetting.TAuditOverreachingNamespaces.Any(space =>
                mapped.Equals(space, StringComparison.Ordinal)
                || mapped.StartsWith(space + ".", StringComparison.Ordinal)))
        {
            violations.Add(new TViolation(path, line, mapped, "Overreaching", "maps a logic namespace"));
        }
    }

    private static void TAuditValueScan(
        string path,
        int line,
        string slot,
        string value,
        IReadOnlySet<string> deportment,
        List<TViolation> violations)
    {
        HashSet<string> named = new(StringComparer.Ordinal);
        foreach (Match match in TAuditStaticPattern.Matches(value))
        {
            string name = match.Groups[1].Value;
            named.Add(name);
            if (!deportment.Contains(name))
            {
                violations.Add(new TViolation(path, line, name, "Overreaching", "reads a logic constant"));
            }
        }

        foreach (Match match in TAuditLogicPattern.Matches(value))
        {
            string name = match.Groups[1].Value;
            if (named.Add(name) && !deportment.Contains(name))
            {
                violations.Add(new TViolation(path, line, name, "Overreaching", $"names logic in {slot}"));
            }
        }
    }
}
