using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace Convention.Tests;

internal static class TAuditReachWalker
{
    private static readonly Regex TAuditLogicPattern = new(
        @"(?<![A-Za-z0-9_])(L[A-Z][A-Za-z0-9_]*)", RegexOptions.Compiled);

    private static readonly Regex TAuditStaticPattern = new(
        @"x:Static\s+(?:[A-Za-z0-9_]+:)?(L[A-Z][A-Za-z0-9_]*)", RegexOptions.Compiled);

    public static IReadOnlyList<TViolation> TAuditRun(IEnumerable<string> markupPaths)
    {
        List<TViolation> violations = [];
        IReadOnlySet<string> deportment = TAuditBinder.TAuditDeportmentRead();
        foreach (string path in markupPaths)
        {
            XDocument document;
            try
            {
                document = XDocument.Load(path, LoadOptions.SetLineInfo);
            }
            catch (XmlException failure)
            {
                violations.Add(new TViolation(path, failure.LineNumber, "markup", "Reach", "does not parse as XML"));
                continue;
            }

            foreach (XElement element in document.Descendants())
            {
                TAuditElementScan(path, element, deportment, violations);
            }
        }

        return violations;
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

    private static void TAuditNamespaceScan(string path, int line, string value, List<TViolation> violations)
    {
        const string prefix = "clr-namespace:";
        if (!value.StartsWith(prefix, StringComparison.Ordinal))
        {
            return;
        }

        string mapped = value[prefix.Length..].Split(';')[0];
        if (TAuditStrictSetting.TAuditReachNamespaces.Any(space =>
                mapped.Equals(space, StringComparison.Ordinal)
                || mapped.StartsWith(space + ".", StringComparison.Ordinal)))
        {
            violations.Add(new TViolation(path, line, mapped, "Reach", "maps a logic namespace"));
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
                violations.Add(new TViolation(path, line, name, "Reach", "reads a logic constant"));
            }
        }

        foreach (Match match in TAuditLogicPattern.Matches(value))
        {
            string name = match.Groups[1].Value;
            if (named.Add(name) && !deportment.Contains(name))
            {
                violations.Add(new TViolation(path, line, name, "Reach", $"names logic in {slot}"));
            }
        }
    }
}
