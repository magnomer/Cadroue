using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Convention.Tests;

internal static class TAuditPlatformProject
{
    public static bool TAuditTrueCheck(string value) => value.Equals("true", StringComparison.OrdinalIgnoreCase);

    public static bool TAuditListCheck(XDocument document, string element, string rule) =>
        TAuditValueRead(document, element)
            .SelectMany(value => Regex.Split(value, @"[;,\s]+"))
            .Contains(rule, StringComparer.OrdinalIgnoreCase);

    public static string[] TAuditFrameworkRead(XDocument document) => TAuditValueRead(document, "TargetFramework")
        .Concat(TAuditValueRead(document, "TargetFrameworks"))
        .SelectMany(value => value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        .ToArray();

    public static IEnumerable<string> TAuditValueRead(XDocument document, string element) => document.Descendants()
        .Where(node => node.Name.LocalName.Equals(element, StringComparison.OrdinalIgnoreCase))
        .Select(node => node.Value.Trim());

    public static IEnumerable<string> TAuditIncludeRead(XDocument document, string element) => document.Descendants()
        .Where(node => node.Name.LocalName.Equals(element, StringComparison.OrdinalIgnoreCase))
        .Select(node => (string?)node.Attribute("Include") ?? "")
        .Where(include => include.Length > 0);
}
