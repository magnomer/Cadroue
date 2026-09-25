using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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

    private static readonly Lazy<IReadOnlyList<SyntaxTree>> TAuditTracked = new(TAuditTrackedRead);

    private static readonly Dictionary<SyntaxTree, SemanticModel> TAuditModels = [];

    private static readonly Dictionary<SyntaxNode, ISymbol?> TAuditSymbols = [];

    public static string TAuditRoot { get; } = TAuditSource.TAuditRootRead();

    public static IReadOnlyList<SyntaxTree> TAuditTrees => TAuditTracked.Value;

    public static CSharpCompilation TAuditCompilation => TAuditBinderCompilation.Value;

    public static SemanticModel TAuditModelRead(SyntaxTree tree)
    {
        lock (TAuditModels)
        {
            if (!TAuditModels.TryGetValue(tree, out SemanticModel? model))
            {
                model = TAuditBinderCompilation.Value.GetSemanticModel(tree, true);
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

    public static bool TAuditHeaderCheck(SimpleNameSyntax name)
    {
        SyntaxNode? parent = name.Parent;
        if (parent is QualifiedNameSyntax qualified)
        {
            parent = qualified.Parent;
        }

        return parent is UsingDirectiveSyntax or NamespaceDeclarationSyntax or FileScopedNamespaceDeclarationSyntax;
    }

    public static bool TAuditDataCheck(INamedTypeSymbol type) =>
        type.TypeKind is TypeKind.Enum or TypeKind.Struct or TypeKind.Delegate || type.IsRecord;

    public static INamedTypeSymbol? TAuditTypeRead(ISymbol symbol) => symbol switch
    {
        IAliasSymbol alias => TAuditTypeRead(alias.Target),
        INamedTypeSymbol named => named.IsTupleType || named.IsAnonymousType ? null : named.OriginalDefinition,
        IArrayTypeSymbol array => TAuditTypeRead(array.ElementType),
        INamespaceSymbol or ITypeParameterSymbol or ILocalSymbol or IParameterSymbol => null,
        IRangeVariableSymbol or IDiscardSymbol or ILabelSymbol or IPreprocessingSymbol => null,
        _ => symbol.ContainingType?.OriginalDefinition,
    };

    public static ISymbol? TAuditSymbolRead(SemanticModel model, SimpleNameSyntax name)
    {
        SymbolInfo info = model.GetSymbolInfo(name);
        return info.Symbol ?? info.CandidateSymbols.FirstOrDefault();
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

    public static string? TAuditSourceRead(INamedTypeSymbol type)
    {
        Location? source = type.Locations.FirstOrDefault(location => location.IsInSource);
        return source?.SourceTree is null ? null : TAuditRelativeRead(source.SourceTree.FilePath);
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
            _ => TAuditSideCheck(symbol.ContainingType, false)
        };
    }

    public static bool TAuditLogicCheck(ITypeSymbol? type)
    {
        return type switch
        {
            null => false,
            IArrayTypeSymbol array => TAuditLogicCheck(array.ElementType),
            INamedTypeSymbol named => TAuditSideCheck(named, false) || named.TypeArguments.Any(TAuditLogicCheck),
            _ => false
        };
    }

    public static bool TAuditShellCheck(ITypeSymbol? type)
    {
        return type switch
        {
            null => false,
            IArrayTypeSymbol array => TAuditShellCheck(array.ElementType),
            INamedTypeSymbol named => TAuditSideCheck(named, true) || named.TypeArguments.Any(TAuditShellCheck),
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

    public static IReadOnlySet<string> TAuditDeportmentRead()
    {
        HashSet<string> names = new(StringComparer.Ordinal);
        INamespaceSymbol? space = TAuditBinderCompilation.Value.Assembly.GlobalNamespace;
        foreach (string part in TAuditStrictSetting.TAuditDeportmentNamespace.Split('.'))
        {
            space = space?.GetNamespaceMembers()
                .FirstOrDefault(member => string.Equals(member.Name, part, StringComparison.Ordinal));
        }

        foreach (INamedTypeSymbol type in space?.GetTypeMembers() ?? [])
        {
            names.Add(type.Name);
            names.UnionWith(type.GetMembers().Select(member => member.Name));
        }

        return names;
    }

    private static bool TAuditSideCheck(INamedTypeSymbol? type, bool shell)
    {
        string? source = type is null ? null : TAuditSourceRead(type.OriginalDefinition);
        if (source is null)
        {
            return false;
        }

        bool inside = TAuditRootRead(TAuditTruthSetting.TAuditShellInclude)
            .Any(folder => source.StartsWith(folder + "/", StringComparison.OrdinalIgnoreCase));
        return inside == shell;
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

    private static IReadOnlyList<SyntaxTree> TAuditTrackedRead()
    {
        string prefix = Path.Combine(TAuditRoot, TAuditBinderSource.TrimEnd('/')) + Path.DirectorySeparatorChar;
        return TAuditBinderCompilation.Value.SyntaxTrees
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
            .Concat(TAuditGeneratedRead())
            .AsParallel()
            .AsOrdered()
            .Select(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path), TAuditSyntaxOptions, path))
            .ToList();
        return CSharpCompilation.Create(
            "AuditBinder",
            trees,
            TAuditReferenceRead(),
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                allowUnsafe: true,
                nullableContextOptions: NullableContextOptions.Enable));
    }

    private static List<string> TAuditGeneratedRead()
    {
        List<string> files = [];
        foreach (string root in TAuditRootRead(TAuditTruthSetting.TAuditShellInclude))
        {
            string folder = Path.Combine(
                TAuditRoot,
                root.Replace('/', Path.DirectorySeparatorChar),
                "obj",
                TAuditTruthSetting.TAuditConfiguration);
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

            files.AddRange(Directory.EnumerateFiles(target, "*.cs", SearchOption.AllDirectories)
                .Where(path => !Path.GetFileName(path).Contains("_wpftmp", StringComparison.Ordinal))
                .Where(path => !path.EndsWith(".g.i.cs", StringComparison.OrdinalIgnoreCase)));
        }

        return files;
    }

    private static List<MetadataReference> TAuditReferenceRead()
    {
        Dictionary<string, string> chosen = new(StringComparer.OrdinalIgnoreCase);
        string runtime = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        string shared = Path.GetDirectoryName(Path.GetDirectoryName(runtime)!)!;
        foreach (string pack in TAuditTruthSetting.TAuditFrameworkPacks)
        {
            string folder = Path.Combine(shared, pack, Path.GetFileName(runtime));
            Assert.True(Directory.Exists(folder), TAuditConvention.TAuditReportFormat(
                "AUDITBINDER",
                $"The shared framework '{pack}' is not installed beside the test runtime at {folder}."));
            foreach (string path in Directory.EnumerateFiles(folder, "*.dll"))
            {
                chosen.TryAdd(Path.GetFileNameWithoutExtension(path), path);
            }
        }

        string output = Path.Combine(
            TAuditRoot,
            TAuditTruthSetting.TAuditReferenceRoot.Replace('/', Path.DirectorySeparatorChar),
            "bin",
            TAuditTruthSetting.TAuditConfiguration);
        string? built = Directory.Exists(output)
            ? Directory.EnumerateDirectories(output).OrderByDescending(Directory.GetLastWriteTimeUtc).FirstOrDefault()
            : null;
        Assert.True(built is not null, TAuditConvention.TAuditReportFormat(
            "AUDITBINDER", $"No build output under {output}; build the solution before the audit."));
        string project = TAuditNameSetting.TAuditProject;
        foreach (string path in Directory.EnumerateFiles(built!, "*.dll"))
        {
            string name = Path.GetFileNameWithoutExtension(path);
            if (name.Equals(project, StringComparison.OrdinalIgnoreCase)
                || name.StartsWith(project + ".", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            chosen[name] = path;
        }

        return chosen.Values.Select(path => (MetadataReference)MetadataReference.CreateFromFile(path)).ToList();
    }
}
