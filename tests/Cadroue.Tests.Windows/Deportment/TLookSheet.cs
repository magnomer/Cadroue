using System.Runtime.ExceptionServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Threading;
using System.Xml.Linq;

using Xunit;

namespace Cadroue.Tests.Windows;

public sealed class TLookSheet
{
    private static readonly XNamespace TLookSheetMarkup = "http://schemas.microsoft.com/winfx/2006/xaml";

    private static readonly Regex TLookSheetRow = new(
        """new\(\s*"(?<style>\w+)"\s*,[^"]*?"(?<part>\w+)"\s*,[^"]*?"(?<key>\w+)"\s*\)""",
        RegexOptions.Singleline);

    // The window carries no Loaded handler of its own; the reach set on it is what lets Loaded find the button.
    [Fact]
    public void Look_SurfaceWithoutHandler_ReachesChild()
    {
        double faded = 1;
        TLookSheetRun(() =>
        {
            TInterface.TLookStateAttach();
            Window window = new() { Width = 120, Height = 80, ShowInTaskbar = false, WindowStyle = WindowStyle.None };
            window.Resources["PButtonFade"] = 0.35;
            window.Resources["PButtonFaded"] = (Style)XamlReader.Parse(
                "<Style xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" "
                + "xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" TargetType=\"Button\">"
                + "<Setter Property=\"Template\"><Setter.Value><ControlTemplate TargetType=\"Button\">"
                + "<Border x:Name=\"PButtonFrame\"><Border x:Name=\"PButtonPlate\" /></Border>"
                + "</ControlTemplate></Setter.Value></Setter></Style>");
            TInterface.TLookStyleAttach(window);
            Button button = new() { IsEnabled = false, Style = (Style)window.Resources["PButtonFaded"] };
            window.Content = button;
            window.Show();
            window.Dispatcher.Invoke(() => { }, DispatcherPriority.SystemIdle);
            faded = ((FrameworkElement)button.Template.FindName("PButtonFrame", button)).Opacity;
            window.Close();
        });

        Assert.Equal(0.35, faded, 3);
    }

    private static void TLookSheetRun(Action body)
    {
        ExceptionDispatchInfo? failure = null;
        Thread thread = new(() =>
        {
            try
            {
                body();
            }
            catch (Exception exception)
            {
                failure = ExceptionDispatchInfo.Capture(exception);
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        failure?.Throw();
    }

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
