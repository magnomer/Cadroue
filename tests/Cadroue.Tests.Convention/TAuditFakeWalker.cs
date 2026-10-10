using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Convention.Tests;

internal static class TAuditFakeWalker
{
    private const string TAuditTypeMark = "T:";

    private static readonly CSharpParseOptions TAuditSyntaxOptions = new(
        languageVersion: LanguageVersion.Preview,
        documentationMode: DocumentationMode.None,
        kind: SourceCodeKind.Regular);

    public static IReadOnlyList<TViolation> TAuditRun(
        IReadOnlyList<string> testPaths, IReadOnlySet<string> markup, IReadOnlySet<string> elements)
    {
        Dictionary<string, TAuditFakeMember> members = new(StringComparer.Ordinal);
        foreach (SyntaxTree tree in TAuditBinder.TAuditTrees)
        {
            TAuditFakeCandidate.TAuditMemberScan(TAuditBinder.TAuditModelRead(tree), members);
        }

        TAuditFakeCandidate.TAuditLineageAdd(members);

        HashSet<string> names = new(members.Values.Select(member => member.TAuditMemberName), StringComparer.Ordinal);
        HashSet<string> serialized = new(StringComparer.Ordinal);
        foreach (SyntaxTree tree in TAuditBinder.TAuditCompilation.SyntaxTrees)
        {
            SemanticModel model = TAuditBinder.TAuditModelRead(tree);
            TAuditFakeUse.TAuditUseScan(model, members, names, false);
            TAuditFakeSerial.TAuditPersistScan(model, serialized);
        }

        CSharpCompilation tests = TAuditTestCreate(testPaths);
        foreach (SyntaxTree tree in tests.SyntaxTrees)
        {
            TAuditFakeUse.TAuditUseScan(tests.GetSemanticModel(tree, true), members, names, true);
        }

        TAuditLiveApply(members, markup, elements, serialized);
        return members.Values
            .Where(member => !member.TAuditMemberLive)
            .Where(member => !member.TAuditMemberKey.StartsWith(TAuditTypeMark, StringComparison.Ordinal))
            .Select(member => TAuditRowCreate(member, members))
            .OrderBy(row => row.TViolationPath, StringComparer.Ordinal)
            .ThenBy(row => row.TViolationLine)
            .ToList();
    }

    private static CSharpCompilation TAuditTestCreate(IReadOnlyList<string> testPaths)
    {
        CSharpCompilation source = TAuditBinder.TAuditCompilation;
        List<SyntaxTree> trees = testPaths
            .AsParallel()
            .AsOrdered()
            .Select(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path), TAuditSyntaxOptions, path))
            .ToList();
        trees.Add(CSharpSyntaxTree.ParseText(
            string.Concat(TAuditFakeSetting.TAuditFakeUsing.Select(space => $"global using {space};\n")),
            TAuditSyntaxOptions));
        return CSharpCompilation.Create(
            "AuditFake",
            trees,
            source.References.Append(source.ToMetadataReference()),
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                allowUnsafe: true,
                nullableContextOptions: NullableContextOptions.Enable));
    }

    private static void TAuditLiveApply(
        Dictionary<string, TAuditFakeMember> members,
        IReadOnlySet<string> markup,
        IReadOnlySet<string> elements,
        HashSet<string> serialized)
    {
        Dictionary<string, List<TAuditFakeMember>> dependents = new(StringComparer.Ordinal);
        Queue<TAuditFakeMember> queue = new();
        foreach (TAuditFakeMember member in members.Values)
        {
            foreach (string reader in member.TAuditMemberReaders.Where(members.ContainsKey))
            {
                if (!dependents.TryGetValue(reader, out List<TAuditFakeMember>? list))
                {
                    list = [];
                    dependents.Add(reader, list);
                }

                list.Add(member);
            }

            bool typed = member.TAuditMemberKey.StartsWith(TAuditTypeMark, StringComparison.Ordinal);
            if ((typed ? elements : markup).Contains(member.TAuditMemberName)
                || serialized.Contains(member.TAuditMemberKey)
                || member.TAuditMemberReaders.Any(reader => !members.ContainsKey(reader)))
            {
                member.TAuditMemberLive = true;
                queue.Enqueue(member);
            }
        }

        while (queue.TryDequeue(out TAuditFakeMember? live))
        {
            foreach (TAuditFakeMember member in dependents.GetValueOrDefault(live.TAuditMemberKey) ?? [])
            {
                if (!member.TAuditMemberLive)
                {
                    member.TAuditMemberLive = true;
                    queue.Enqueue(member);
                }
            }
        }
    }

    private static TViolation TAuditRowCreate(TAuditFakeMember member, Dictionary<string, TAuditFakeMember> members)
    {
        List<string> readers = member.TAuditMemberReaders
            .Select(reader => members[reader].TAuditMemberName)
            .Order(StringComparer.Ordinal)
            .ToList();
        List<string> testers = member.TAuditMemberTesters.Order(StringComparer.Ordinal).ToList();
        string kind = testers.Count > 0 ? "Tested" : "Orphan";
        List<string> reasons = [];
        if (testers.Count > 0)
        {
            reasons.Add("by tests " + TAuditListFormat(testers));
        }

        if (readers.Count > 0)
        {
            reasons.Add("by fake members " + TAuditListFormat(readers));
        }

        string reason = reasons.Count == 0 ? "read by nothing" : "read only " + string.Join(" and ", reasons);
        return new TViolation(member.TAuditMemberPath, member.TAuditMemberLine, member.TAuditMemberName, kind, reason);
    }

    private static string TAuditListFormat(List<string> names)
    {
        string shown = string.Join(", ", names.Take(3));
        return names.Count > 3 ? $"{shown} and {names.Count - 3} more" : shown;
    }
}
