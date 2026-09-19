using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Convention.Tests;

internal static class TAuditSemantic
{
    private static readonly CSharpParseOptions TAuditSyntaxOptions = new(
        languageVersion: LanguageVersion.Preview,
        documentationMode: DocumentationMode.None,
        kind: SourceCodeKind.Regular);

    private static readonly object TAuditGate = new();

    private static Compilation? TAuditCompilation;

    private static Dictionary<SyntaxTree, SemanticModel> TAuditModels = [];

    private static IReadOnlyList<SyntaxNode> TAuditRoots = [];

    private static IReadOnlyList<string> TAuditSources = [];

    private static readonly Dictionary<SyntaxNode, ISymbol?> TAuditSymbols = [];

    public static IReadOnlyList<SyntaxNode> TAuditModelCreate(IReadOnlyList<string> sourcePaths)
    {
        lock (TAuditGate)
        {
            if (TAuditCompilation is not null && TAuditSources.SequenceEqual(sourcePaths, StringComparer.Ordinal))
            {
                return TAuditRoots;
            }

            string repoRoot = TAuditSource.TAuditRootRead();
            List<SyntaxTree> walked = sourcePaths
                .Select(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path), TAuditSyntaxOptions, path))
                .ToList();
            List<SyntaxTree> generated = TAuditGeneratedRead(repoRoot)
                .Select(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path), TAuditSyntaxOptions, path))
                .ToList();
            List<MetadataReference> references = TAuditReferenceRead(repoRoot);

            TAuditCompilation = CSharpCompilation.Create(
                TAuditTruthSetting.TAuditShellAssembly,
                walked.Concat(generated),
                references,
                new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    allowUnsafe: true,
                    nullableContextOptions: NullableContextOptions.Enable));
            TAuditModels = walked.ToDictionary(tree => tree, tree => TAuditCompilation.GetSemanticModel(tree, true));
            TAuditRoots = walked.Select(tree => tree.GetRoot()).ToList();
            TAuditSources = sourcePaths.ToList();
            lock (TAuditSymbols)
            {
                TAuditSymbols.Clear();
            }

            return TAuditRoots;
        }
    }

    private static List<string> TAuditGeneratedRead(string repoRoot)
    {
        List<string> files = [];
        foreach (string root in TAuditRootRead(TAuditTruthSetting.TAuditShellInclude))
        {
            string folder = Path.Combine(
                repoRoot, root.Replace('/', Path.DirectorySeparatorChar), "obj", TAuditTruthSetting.TAuditConfiguration);
            if (!Directory.Exists(folder))
            {
                continue;
            }

            string? target = Directory.EnumerateDirectories(folder)
                .OrderByDescending(Directory.GetLastWriteTimeUtc)
                .FirstOrDefault();
            if (target is null)
            {
                continue;
            }

            files.AddRange(Directory.EnumerateFiles(target, "*.cs", SearchOption.TopDirectoryOnly)
                .Where(path => !Path.GetFileName(path).Contains("_wpftmp", StringComparison.Ordinal))
                .Where(path => !path.EndsWith(".g.i.cs", StringComparison.OrdinalIgnoreCase)));
        }

        return files;
    }

    private static List<MetadataReference> TAuditReferenceRead(string repoRoot)
    {
        Dictionary<string, string> chosen = new(StringComparer.OrdinalIgnoreCase);
        string runtime = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        string shared = Path.GetDirectoryName(Path.GetDirectoryName(runtime)!)!;
        foreach (string pack in TAuditTruthSetting.TAuditFrameworkPacks)
        {
            string folder = Path.Combine(shared, pack, Path.GetFileName(runtime));
            Assert.True(Directory.Exists(folder), TAuditConvention.TAuditReportFormat(
                "AUDITSEMANTIC", $"The shared framework '{pack}' is not installed beside the test runtime at {folder}."));
            foreach (string path in Directory.EnumerateFiles(folder, "*.dll"))
            {
                chosen.TryAdd(Path.GetFileNameWithoutExtension(path), path);
            }
        }

        string output = Path.Combine(
            repoRoot,
            TAuditTruthSetting.TAuditReferenceRoot.Replace('/', Path.DirectorySeparatorChar),
            "bin",
            TAuditTruthSetting.TAuditConfiguration);
        string? built = Directory.Exists(output)
            ? Directory.EnumerateDirectories(output).OrderByDescending(Directory.GetLastWriteTimeUtc).FirstOrDefault()
            : null;
        Assert.True(built is not null, TAuditConvention.TAuditReportFormat(
            "AUDITSEMANTIC", $"No build output under {output}; build the solution before the audit."));
        foreach (string path in Directory.EnumerateFiles(built!, "*.dll"))
        {
            string name = Path.GetFileNameWithoutExtension(path);
            if (TAuditTruthSetting.TAuditReferenceSkip.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            chosen[name] = path;
        }

        foreach (string assembly in TAuditTruthSetting.TAuditLogicAssemblies)
        {
            Assert.True(chosen.ContainsKey(assembly), TAuditConvention.TAuditReportFormat(
                "AUDITSEMANTIC", $"{assembly}.dll is not in the build output; build the solution before the audit."));
        }

        return chosen.Values.Select(path => (MetadataReference)MetadataReference.CreateFromFile(path)).ToList();
    }

    public static IReadOnlyList<string> TAuditRootRead(IEnumerable<string> patterns)
    {
        return patterns
            .Select(pattern => pattern[..pattern.IndexOf('*')].TrimEnd('/'))
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    public static bool TAuditWalkCheck(SyntaxNode root)
    {
        string repoRoot = TAuditSource.TAuditRootRead();
        string relative = Path.GetRelativePath(repoRoot, root.SyntaxTree.FilePath).Replace('\\', '/');
        return TAuditRootRead(TAuditTruthSetting.TAuditTruthInclude)
            .Any(folder => relative.StartsWith(folder + "/", StringComparison.OrdinalIgnoreCase));
    }

    public static SemanticModel TAuditModelRead(SyntaxNode node)
    {
        return TAuditModels[node.SyntaxTree];
    }

    public static ISymbol? TAuditSymbolRead(SyntaxNode node)
    {
        if (node is ArgumentSyntax argument)
        {
            node = argument.Expression;
        }

        lock (TAuditSymbols)
        {
            if (TAuditSymbols.TryGetValue(node, out ISymbol? known))
            {
                return known;
            }
        }

        ISymbol? resolved = TAuditSymbolResolve(node);
        lock (TAuditSymbols)
        {
            TAuditSymbols[node] = resolved;
        }

        return resolved;
    }

    private static ISymbol? TAuditSymbolResolve(SyntaxNode node)
    {
        SemanticModel model = TAuditModelRead(node);
        ISymbol? symbol = node is ExpressionSyntax ? null : model.GetDeclaredSymbol(node);
        if (symbol is null)
        {
            SymbolInfo info = model.GetSymbolInfo(node);
            symbol = info.Symbol ?? info.CandidateSymbols.FirstOrDefault();
        }

        return symbol?.OriginalDefinition;
    }

    public static ITypeSymbol? TAuditTypeRead(SyntaxNode node)
    {
        SemanticModel model = TAuditModelRead(node);
        if (node is ArgumentSyntax argument)
        {
            node = argument.Expression;
        }

        if (node is ExpressionSyntax expression)
        {
            ITypeSymbol? type = model.GetTypeInfo(expression).Type;
            if (type is not null && type.TypeKind != TypeKind.Error)
            {
                return type;
            }
        }

        return TAuditSymbolRead(node) switch
        {
            ILocalSymbol local => local.Type,
            IParameterSymbol parameter => parameter.Type,
            IFieldSymbol field => field.Type,
            IPropertySymbol property => property.Type,
            IMethodSymbol method => method.ReturnType,
            ITypeSymbol type => type,
            _ => null
        };
    }

    public static bool TAuditLogicCheck(SyntaxNode node)
    {
        ISymbol? symbol = TAuditSymbolRead(node);
        return TAuditLogicCheck(symbol) || TAuditLogicCheck(TAuditTypeRead(node));
    }

    public static bool TAuditLogicCheck(ISymbol? symbol)
    {
        return symbol switch
        {
            null => false,
            ILocalSymbol local => TAuditLogicCheck(local.Type),
            IParameterSymbol parameter => TAuditLogicCheck(parameter.Type),
            ITypeSymbol type => TAuditLogicCheck(type),
            _ => TAuditAssemblyCheck(symbol.ContainingAssembly)
        };
    }

    public static bool TAuditLogicCheck(ITypeSymbol? type)
    {
        return type switch
        {
            null => false,
            IArrayTypeSymbol array => TAuditLogicCheck(array.ElementType),
            INamedTypeSymbol named => TAuditAssemblyCheck(named.ContainingAssembly)
                                      || named.TypeArguments.Any(TAuditLogicCheck),
            _ => TAuditAssemblyCheck(type.ContainingAssembly)
        };
    }

    public static bool TAuditShellCheck(ITypeSymbol? type)
    {
        return type switch
        {
            null => false,
            IArrayTypeSymbol array => TAuditShellCheck(array.ElementType),
            INamedTypeSymbol named => SymbolEqualityComparer.Default.Equals(
                                          named.ContainingAssembly, TAuditCompilation?.Assembly)
                                      || named.TypeArguments.Any(TAuditShellCheck),
            _ => false
        };
    }

    public static bool TAuditControlCheck(ITypeSymbol? type)
    {
        for (ITypeSymbol? current = type; current is not null; current = current.BaseType)
        {
            if (TAuditTruthSetting.TAuditControlBases.Contains(current.ToDisplayString(), StringComparer.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    public static bool TAuditNamedCheck(ITypeSymbol? type, IReadOnlyList<string> names)
    {
        return type is not null && names.Contains(type.Name, StringComparer.Ordinal);
    }

    public static string TAuditLabelRead(ISymbol symbol)
    {
        return symbol.ContainingType is null
            ? symbol.Name
            : $"{symbol.ContainingType.Name}.{symbol.Name}";
    }

    private static bool TAuditAssemblyCheck(IAssemblySymbol? assembly)
    {
        return assembly is not null
               && TAuditTruthSetting.TAuditLogicAssemblies.Contains(assembly.Name, StringComparer.Ordinal);
    }
}
