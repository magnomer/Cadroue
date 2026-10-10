using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

internal sealed class LAuditBinder
{
    private static readonly CSharpParseOptions LAuditSyntaxOptions = new(
        languageVersion: LanguageVersion.Preview,
        documentationMode: DocumentationMode.None,
        kind: SourceCodeKind.Regular);

    private LAuditBinder(string root, CSharpCompilation compilation, IReadOnlyList<SyntaxTree> tracked)
    {
        LAuditRoot = root;
        LAuditCompilation = compilation;
        LAuditTrees = tracked;
    }

    public string LAuditRoot { get; }

    public CSharpCompilation LAuditCompilation { get; }

    public IReadOnlyList<SyntaxTree> LAuditTrees { get; }

    public string LAuditRelativeRead(string path) => Path.GetRelativePath(LAuditRoot, path).Replace('\\', '/');

    public static LAuditBinder LAuditBinderRead(string root, string configPath)
    {
        JsonElement config = JsonDocument.Parse(File.ReadAllText(configPath)).RootElement;
        string source = config.GetProperty("source").GetString()!;
        string configuration = config.GetProperty("configuration").GetString()!;
        string reference = config.GetProperty("reference").GetString()!;
        string owned = config.GetProperty("project").GetString()!;
        string[] packs = LAuditListRead(config, "packs");
        string[] segments = LAuditListRead(config, "excludeSegments");
        string[] suffixes = LAuditListRead(config, "excludeSuffixes");
        string[] prefixes = LAuditListRead(config, "excludePrefixes");

        List<string> sources = LAuditFileRead(root, [source + "*.cs"], [], segments, suffixes, prefixes);
        if (sources.Count == 0)
        {
            throw new InvalidOperationException("No tracked source file was enumerated, so the binder would compile nothing.");
        }

        List<string> projects = LAuditFileRead(root, [source + "*.csproj"], [], [], [], []);
        if (projects.Count == 0)
        {
            throw new InvalidOperationException("No tracked project file was enumerated, so the binder would compile nothing.");
        }

        List<SyntaxTree> trees = sources
            .Concat(LAuditGeneratedRead(root, projects, configuration))
            .AsParallel()
            .AsOrdered()
            .Select(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path), LAuditSyntaxOptions, path))
            .ToList();
        CSharpCompilation compilation = CSharpCompilation.Create(
            "LAuditBinder",
            trees,
            LAuditReferenceRead(root, packs, reference, configuration, owned),
            new CSharpCompilationOptions(
                OutputKind.ConsoleApplication,
                allowUnsafe: true,
                nullableContextOptions: NullableContextOptions.Enable));
        List<string> errors = compilation.GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Select(diagnostic => $"  {diagnostic}")
            .ToList();
        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                $"{errors.Count} compile error(s) in the bound sources, so an unbound name would slip past the audit.\n"
                + string.Join('\n', errors.Take(40)));
        }

        string prefix = Path.GetFullPath(Path.Combine(root, source.TrimEnd('/'))) + Path.DirectorySeparatorChar;
        List<SyntaxTree> tracked = compilation.SyntaxTrees
            .Where(tree => !Path.GetRelativePath(root, tree.FilePath).Replace('\\', '/').Split('/')
                .Contains("obj", StringComparer.Ordinal))
            .Where(tree => Path.GetFullPath(tree.FilePath).StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .ToList();
        return new LAuditBinder(root, compilation, tracked);
    }

    public static List<string> LAuditFileRead(
        string root, IEnumerable<string> patterns, string[] roots, string[] segments, string[] suffixes, string[] prefixes)
    {
        ProcessStartInfo info = new("git")
        {
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (string argument in new[]
                 {
                     "-c", "core.quotePath=false", "ls-files", "--cached", "--others", "--exclude-standard", "--"
                 })
        {
            info.ArgumentList.Add(argument);
        }

        foreach (string pattern in patterns)
        {
            info.ArgumentList.Add(":(icase)" + pattern);
        }

        using Process process = Process.Start(info)
            ?? throw new InvalidOperationException("Git could not be started to enumerate source files.");
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Git failed to enumerate source files.\n{error}");
        }

        List<string> files = [];
        foreach (string line in output.Split('\n'))
        {
            string relative = line.Trim();
            if (relative.Length == 0 || LAuditExcludedCheck(relative, roots, segments, suffixes, prefixes))
            {
                continue;
            }

            string full = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(full))
            {
                files.Add(full);
            }
        }

        files.Sort(StringComparer.OrdinalIgnoreCase);
        return files;
    }

    private static bool LAuditExcludedCheck(string relative, string[] roots, string[] segments, string[] suffixes, string[] prefixes)
    {
        string normalized = relative.Replace('\\', '/');
        if (roots.Length > 0 && !roots.Any(root =>
                normalized.StartsWith(root.Trim('/') + "/", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        string[] parts = normalized.Split('/');
        if (parts.Any(part => segments.Contains(part, StringComparer.OrdinalIgnoreCase)))
        {
            return true;
        }

        string name = parts[^1];
        return suffixes.Any(suffix => name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            || prefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal));
    }

    private static string LAuditTargetRead(string project, string output, string configuration)
    {
        string framework = XDocument.Load(project).Descendants()
            .Where(node => node.Name.LocalName == "TargetFramework")
            .Select(node => node.Value.Trim())
            .FirstOrDefault() ?? string.Empty;
        return Path.Combine(Path.GetDirectoryName(project)!, output, configuration, framework);
    }

    private static List<string> LAuditGeneratedRead(string root, IEnumerable<string> projects, string configuration)
    {
        List<string> files = [];
        foreach (string project in projects)
        {
            string folder = LAuditTargetRead(project, "obj", configuration);
            bool markup = Directory.EnumerateFiles(Path.GetDirectoryName(project)!, "*.xaml", SearchOption.AllDirectories)
                .Any(path => !Path.GetRelativePath(root, path).Replace('\\', '/').Split('/')
                    .Contains("obj", StringComparer.Ordinal));
            if (markup && !Directory.Exists(folder))
            {
                throw new InvalidOperationException(
                    $"No generated markup code under {folder}; build the solution before the audit.");
            }

            if (!Directory.Exists(folder))
            {
                continue;
            }

            files.AddRange(Directory.EnumerateFiles(folder, "*.g.cs", SearchOption.AllDirectories)
                .Where(path => !Path.GetFileName(path).Contains("_wpftmp", StringComparison.Ordinal)));
        }

        return files;
    }

    private static List<MetadataReference> LAuditReferenceRead(
        string root, string[] packs, string reference, string configuration, string owned)
    {
        Dictionary<string, (Version LAuditVersion, string LAuditPath)> chosen = new(StringComparer.OrdinalIgnoreCase);
        string runtime = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        string shared = Path.GetDirectoryName(Path.GetDirectoryName(runtime)!)!;
        foreach (string pack in packs)
        {
            string folder = Path.Combine(shared, pack, Path.GetFileName(runtime));
            if (!Directory.Exists(folder))
            {
                throw new InvalidOperationException(
                    $"The shared framework '{pack}' is not installed beside the binder runtime at {folder}.");
            }

            foreach (string path in Directory.EnumerateFiles(folder, "*.dll"))
            {
                LAuditAssemblyAdd(chosen, path);
            }
        }

        string project = Path.Combine(
            root, reference.Replace('/', Path.DirectorySeparatorChar), Path.GetFileName(reference) + ".csproj");
        string built = LAuditTargetRead(project, "bin", configuration);
        if (!Directory.Exists(built))
        {
            throw new InvalidOperationException($"No build output under {built}; build the solution before the audit.");
        }

        foreach (string path in Directory.EnumerateFiles(built, "*.dll"))
        {
            string name = Path.GetFileNameWithoutExtension(path);
            if (!name.Equals(owned, StringComparison.OrdinalIgnoreCase)
                && !name.StartsWith(owned + ".", StringComparison.OrdinalIgnoreCase))
            {
                LAuditAssemblyAdd(chosen, path);
            }
        }

        return chosen.Values.Select(entry => (MetadataReference)MetadataReference.CreateFromFile(entry.LAuditPath)).ToList();
    }

    private static void LAuditAssemblyAdd(Dictionary<string, (Version LAuditVersion, string LAuditPath)> chosen, string path)
    {
        Version version;
        try
        {
            version = AssemblyName.GetAssemblyName(path).Version ?? new Version();
        }
        catch (BadImageFormatException)
        {
            return;
        }

        string name = Path.GetFileNameWithoutExtension(path);
        if (!chosen.TryGetValue(name, out (Version LAuditVersion, string LAuditPath) known) || known.LAuditVersion < version)
        {
            chosen[name] = (version, path);
        }
    }

    private static string[] LAuditListRead(JsonElement config, string key)
    {
        return config.GetProperty(key).EnumerateArray().Select(item => item.GetString()!).ToArray();
    }
}
