using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Convention.Tests;

internal static class TAuditBinder
{
    private const string TAuditBinderSource = "src/";

    private static readonly CSharpParseOptions TAuditSyntaxOptions = new(
        languageVersion: LanguageVersion.Preview,
        documentationMode: DocumentationMode.None,
        kind: SourceCodeKind.Regular);

    private static readonly Lazy<CSharpCompilation> TAuditBinderCompilation = new(TAuditCompilationRead);

    private static readonly Lazy<IReadOnlyList<SyntaxTree>> TAuditTracked =
        new(() => TAuditTrackedRead(TAuditBinderCompilation.Value));

    private static readonly AsyncLocal<CSharpCompilation?> TAuditAssayCompilation = new();

    private static readonly AsyncLocal<IReadOnlyDictionary<string, string>?> TAuditAssayMarkup = new();

    private static readonly Dictionary<SyntaxTree, SemanticModel> TAuditModels = [];

    public static string TAuditRoot { get; } = TAuditSource.TAuditRootRead();

    public static IReadOnlyList<SyntaxTree> TAuditTrees => TAuditAssayCompilation.Value is { } assay
        ? TAuditTrackedRead(assay)
        : TAuditTracked.Value;

    public static CSharpCompilation TAuditCompilation =>
        TAuditAssayCompilation.Value ?? TAuditBinderCompilation.Value;

    public static TAuditAssayResult TAuditAssayRun<TAuditAssayResult>(
        IReadOnlyDictionary<string, string> sources, Func<TAuditAssayResult> walk)
    {
        ILookup<bool, KeyValuePair<string, string>> parts = sources.ToLookup(source =>
            string.Equals(Path.GetExtension(source.Key), ".xaml", StringComparison.OrdinalIgnoreCase));
        List<SyntaxTree> trees = parts[false]
            .Select(source => CSharpSyntaxTree.ParseText(
                source.Value, TAuditSyntaxOptions, Path.GetFullPath(Path.Combine(TAuditRoot, source.Key))))
            .ToList();
        TAuditAssayCompilation.Value = TAuditCompilationCreate(trees, OutputKind.DynamicallyLinkedLibrary);
        TAuditAssayMarkup.Value = parts[true].ToDictionary(
            source => Path.GetFullPath(Path.Combine(TAuditRoot, source.Key)), source => source.Value,
            StringComparer.OrdinalIgnoreCase);
        try
        {
            return walk();
        }
        finally
        {
            TAuditAssayCompilation.Value = null;
            TAuditAssayMarkup.Value = null;
        }
    }

    public static string TAuditMarkupRead(string path)
    {
        return TAuditAssayMarkup.Value is { } markups
            ? markups[Path.GetFullPath(path)]
            : File.ReadAllText(path);
    }

    public static SemanticModel TAuditModelRead(SyntaxTree tree)
    {
        lock (TAuditModels)
        {
            if (!TAuditModels.TryGetValue(tree, out SemanticModel? model))
            {
                model = TAuditCompilation.GetSemanticModel(tree, true);
                TAuditModels[tree] = model;
            }

            return model;
        }
    }

    public static SemanticModel TAuditModelRead(SyntaxNode node) => TAuditModelRead(node.SyntaxTree);

    public static IReadOnlyList<SyntaxNode> TAuditWalkRead(IReadOnlyList<string> sourcePaths)
    {
        HashSet<string> chosen = new(sourcePaths.Select(Path.GetFullPath), StringComparer.OrdinalIgnoreCase);
        return TAuditTrees
            .Where(tree => chosen.Contains(Path.GetFullPath(tree.FilePath)))
            .Select(tree => tree.GetRoot())
            .ToList();
    }

    public static void TAuditCoverCheck(string audit, IReadOnlyList<string> sourcePaths)
    {
        HashSet<string> walked = new(
            TAuditTrees.Select(tree => Path.GetFullPath(tree.FilePath)), StringComparer.OrdinalIgnoreCase);
        List<string> missed = sourcePaths
            .Where(path => !walked.Contains(Path.GetFullPath(path)))
            .Select(path => $"  {TAuditRelativeRead(path)}")
            .ToList();
        Assert.True(missed.Count == 0, TAuditConvention.TAuditReportFormat(
            audit, $"{missed.Count} tracked source(s) never reach the walkers.\n{string.Join('\n', missed)}"));
    }

    public static bool TAuditWalkCheck(SyntaxNode root)
    {
        string relative = TAuditRelativeRead(root.SyntaxTree.FilePath);
        return TAuditRootRead(TAuditTruthSetting.TAuditTruthInclude)
            .Any(folder => relative.StartsWith(folder + "/", StringComparison.OrdinalIgnoreCase));
    }

    public static IReadOnlyList<string> TAuditRootRead(IEnumerable<string> patterns)
    {
        return patterns
            .Select(pattern => pattern[..pattern.IndexOf('*')].TrimEnd('/'))
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    public static string TAuditRelativeRead(string path) =>
        Path.GetRelativePath(TAuditRoot, path).Replace('\\', '/');

    public static string? TAuditRingRead(string relative, IEnumerable<string> rings)
    {
        foreach (string ring in rings)
        {
            if (relative.StartsWith(TAuditBinderSource + ring + "/", StringComparison.OrdinalIgnoreCase))
            {
                return ring;
            }
        }

        return null;
    }

    private static IReadOnlyList<SyntaxTree> TAuditTrackedRead(CSharpCompilation compilation)
    {
        string prefix = Path.Combine(TAuditRoot, TAuditBinderSource.TrimEnd('/')) + Path.DirectorySeparatorChar;
        return compilation.SyntaxTrees
            .Where(tree => !TAuditRelativeRead(tree.FilePath).Split('/').Contains("obj", StringComparer.Ordinal))
            .Where(tree => Path.GetFullPath(tree.FilePath).StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private static CSharpCompilation TAuditCompilationRead()
    {
        TAuditScope scope = new(
            [],
            [TAuditBinderSource + "*.cs"],
            TAuditNameSetting.TAuditExcludedSegments,
            TAuditNameSetting.TAuditExcludedSuffixes,
            TAuditNameSetting.TAuditExcludedPrefixes,
            []);
        IReadOnlyList<string> sources = TAuditSource.TAuditFileRead(TAuditRoot, scope);
        Assert.True(sources.Count > 0, TAuditConvention.TAuditReportFormat(
            "AUDITBINDER", "No tracked source file was enumerated; every bound audit would pass vacuously."));

        List<SyntaxTree> trees = sources
            .Concat(TAuditReference.TAuditGeneratedRead())
            .AsParallel()
            .AsOrdered()
            .Select(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path), TAuditSyntaxOptions, path))
            .ToList();
        return TAuditCompilationCreate(trees, OutputKind.ConsoleApplication);
    }

    private static CSharpCompilation TAuditCompilationCreate(IReadOnlyList<SyntaxTree> trees, OutputKind kind)
    {
        CSharpCompilation compilation = CSharpCompilation.Create(
            "AuditBinder",
            trees,
            TAuditReference.TAuditReferenceRead(),
            new CSharpCompilationOptions(
                kind,
                allowUnsafe: true,
                nullableContextOptions: NullableContextOptions.Enable));
        List<string> errors = compilation.GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Select(diagnostic => $"  {diagnostic}")
            .ToList();
        Assert.True(errors.Count == 0, TAuditConvention.TAuditReportFormat(
            "AUDITBINDER",
            $"{errors.Count} compile error(s) in the bound sources; an unbound name would slip past every walker.\n"
            + string.Join('\n', errors.Take(40))));
        return compilation;
    }
}
