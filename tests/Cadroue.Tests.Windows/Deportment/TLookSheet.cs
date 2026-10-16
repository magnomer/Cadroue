using System.Text.RegularExpressions;
using System.Xml.Linq;

using Xunit;

namespace Cadroue.Tests.Windows;

public sealed class TLookSheet
{
    private static readonly XNamespace TLookSheetMarkup = "http://schemas.microsoft.com/winfx/2006/xaml";

    private static readonly Regex TLookSheetRow = new(
        """new\(\s*"(?<style>\w+)"\s*,[^"]*?"(?<part>\w+)"\s*,[^"]*?"(?<key>\w+)"\s*\)""",
        RegexOptions.Singleline);

    [Fact]
    public void Sheet_EveryStylePartAndKey_ExistsInMergedMarkup()
    {
        string root = TLookSheetFind();
        string veneer = Path.Combine(root, "src", "Cadroue.UIVeneer");
        List<XElement> sheets = [.. XDocument.Load(Path.Combine(veneer, "App.xaml"))
            .Descendants()
            .Where(element => element.Name.LocalName == "ResourceDictionary")
            .Select(element => (string?)element.Attribute("Source"))
            .OfType<string>()
            .Select(source => XDocument.Load(Path.Combine(veneer, source)).Root!)];
        XName keyName = TLookSheetMarkup + "Key";
        List<XElement> entries = [.. sheets.SelectMany(sheet => sheet.Elements())];
        HashSet<string> keys = [.. entries.Select(element => (string?)element.Attribute(keyName)).OfType<string>()];
        Dictionary<string, XElement> styles = entries
            .Where(element => element.Name.LocalName == "Style" && element.Attribute(keyName) is not null)
            .ToDictionary(element => (string)element.Attribute(keyName)!);
        string source = File.ReadAllText(
            Path.Combine(root, "src", "Cadroue.UIDeportment", "Kit", "Bind", "Look", "QLookSheet.cs"));
        List<Match> rows = [.. TLookSheetRow.Matches(source).Cast<Match>()];

        List<string> missing = [];
        foreach (Match row in rows)
        {
            string style = row.Groups["style"].Value;
            string part = row.Groups["part"].Value;
            string key = row.Groups["key"].Value;
            if (!styles.ContainsKey(style))
            {
                missing.Add($"style {style}");
                continue;
            }

            List<XElement> holders = TLookSheetSelect(styles, style);
            if (holders.Count == 0 || holders.Any(holder => !TLookSheetRead(holder).Contains(part)))
            {
                missing.Add($"part {part} in {style}");
            }

            if (!keys.Contains(key))
            {
                missing.Add($"key {key} for {style}");
            }
        }

        Assert.NotEmpty(rows);
        Assert.Equal(Regex.Count(source, @"\bnew\("), rows.Count);
        Assert.Empty(missing);
    }

    private static List<XElement> TLookSheetSelect(Dictionary<string, XElement> styles, string style)
    {
        if (TLookSheetRead(styles[style]).Count > 0)
        {
            return [styles[style]];
        }

        return [.. styles.Values.Where(candidate =>
            (string?)candidate.Attribute("BasedOn") == $"{{StaticResource {style}}}")];
    }

    private static HashSet<string> TLookSheetRead(XElement style)
    {
        return [.. style.Descendants()
            .Select(element => (string?)element.Attribute(TLookSheetMarkup + "Name"))
            .OfType<string>()];
    }

    private static string TLookSheetFind()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Cadroue.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the folder holding Cadroue.slnx.");
    }
}
