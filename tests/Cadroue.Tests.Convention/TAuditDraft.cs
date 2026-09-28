using System.Text.RegularExpressions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Xunit;

namespace Convention.Tests;

public sealed class TAuditDraft
{
    private static readonly CSharpParseOptions TAuditSyntaxOptions = new(
        languageVersion: LanguageVersion.Preview,
        documentationMode: DocumentationMode.None,
        kind: SourceCodeKind.Regular);

    [Fact]
    public void AuditDraft_Portrait_NamesEveryProperty()
    {
        TAuditDraftCheck(
            "portrait", TAuditDraftSetting.TAuditDraftPortrait, TAuditDraftSetting.TAuditPortraitWaiver);
    }

    [Fact]
    public void AuditDraft_Markup_NamesEveryProperty()
    {
        TAuditDraftCheck("markup", TAuditDraftSetting.TAuditDraftMarkup, TAuditDraftSetting.TAuditMarkupWaiver);
    }

    [Fact]
    public void AuditDraft_Exemplar_NamesEveryProperty()
    {
        TAuditDraftCheck(
            "exemplar", TAuditDraftSetting.TAuditDraftExemplar, TAuditDraftSetting.TAuditExemplarWaiver);
    }

    [Fact]
    public void AuditDraft_Setting_MatchesTheRecords()
    {
        string repoRoot = TAuditSource.TAuditRootRead();
        Dictionary<string, IReadOnlyList<string>> records = TAuditDraftRead(repoRoot);
        HashSet<string> properties = new(records.Values.SelectMany(names => names), StringComparer.Ordinal);

        List<string> hits = [];
        foreach (string type in TAuditDraftSetting.TAuditDraftTypes)
        {
            if (!records.ContainsKey(type))
            {
                hits.Add($"  {type} is no record under {string.Join(' ', TAuditDraftSetting.TAuditDraftInclude)}");
            }
        }

        foreach (string name in TAuditDraftSetting.TAuditDraftWaiver)
        {
            if (!properties.Contains(name))
            {
                hits.Add($"  {name} is waived but is no property of a draft record");
            }
        }

        Assert.True(hits.Count == 0, TAuditConvention.TAuditReportFormat(
            "AUDITDRAFT",
            $"{hits.Count} setting line(s) name nothing in the draft records.\n{string.Join('\n', hits)}"));
    }

    private static void TAuditDraftCheck(string side, IReadOnlyList<string> include, IReadOnlyList<string> waiver)
    {
        if (TAuditDraftSetting.TAuditDraftTypes.Length == 0)
        {
            return;
        }

        string repoRoot = TAuditSource.TAuditRootRead();
        Dictionary<string, IReadOnlyList<string>> records = TAuditDraftRead(repoRoot);
        string text = TAuditTextRead(repoRoot, include);
        HashSet<string> shared = new(TAuditDraftSetting.TAuditDraftWaiver, StringComparer.Ordinal);
        HashSet<string> waived = new(waiver, StringComparer.Ordinal);
        HashSet<string> properties = new(StringComparer.Ordinal);

        List<string> missing = [];
        List<string> stale = [];
        foreach ((string type, IReadOnlyList<string> names) in records)
        {
            foreach (string name in names)
            {
                properties.Add(name);
                if (shared.Contains(name))
                {
                    continue;
                }

                bool named = TAuditNameCheck(text, name);
                if (waived.Contains(name))
                {
                    if (named)
                    {
                        stale.Add($"  {name} is waived for the {side} but the {side} names it");
                    }
                }
                else if (!named)
                {
                    missing.Add($"  {type}.{name}");
                }
            }
        }

        foreach (string name in waiver)
        {
            if (!properties.Contains(name))
            {
                stale.Add($"  {name} is waived for the {side} but is no property of a draft record");
            }
        }

        string files = string.Join(' ', include);
        Assert.True(missing.Count == 0 && stale.Count == 0, TAuditConvention.TAuditReportFormat(
            "AUDITDRAFT",
            $"{missing.Count} draft propert(y/ies) unnamed in the {side} ({files}), "
            + $"{stale.Count} stale waiver line(s).\n"
            + string.Join('\n', missing.Concat(stale))));
    }

    private static Dictionary<string, IReadOnlyList<string>> TAuditDraftRead(string repoRoot)
    {
        HashSet<string> types = new(TAuditDraftSetting.TAuditDraftTypes, StringComparer.Ordinal);
        Dictionary<string, IReadOnlyList<string>> records = new(StringComparer.Ordinal);
        TAuditScope scope = new([], TAuditDraftSetting.TAuditDraftInclude, [], [], [], []);
        foreach (string path in TAuditSource.TAuditFileRead(repoRoot, scope))
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(File.ReadAllText(path), TAuditSyntaxOptions, path);
            IEnumerable<RecordDeclarationSyntax> declared =
                tree.GetRoot().DescendantNodes().OfType<RecordDeclarationSyntax>();
            foreach (RecordDeclarationSyntax record in declared)
            {
                string type = record.Identifier.ValueText;
                if (!types.Contains(type) || record.ParameterList is null)
                {
                    continue;
                }

                records[type] = record.ParameterList.Parameters
                    .Select(parameter => parameter.Identifier.ValueText)
                    .ToList();
            }
        }

        return records;
    }

    private static string TAuditTextRead(string repoRoot, IReadOnlyList<string> include)
    {
        TAuditScope scope = new([], include, [], [], [], []);
        IReadOnlyList<string> files = TAuditSource.TAuditFileRead(repoRoot, scope);
        Assert.True(files.Count > 0, TAuditConvention.TAuditReportFormat(
            "AUDITDRAFT", $"No tracked file matches {string.Join(' ', include)}."));
        return string.Join('\n', files.Select(File.ReadAllText));
    }

    private static bool TAuditNameCheck(string text, string name)
    {
        return Regex.IsMatch(text, @"\b" + Regex.Escape(name) + @"\b", RegexOptions.CultureInvariant);
    }
}
