using System.Reflection;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Convention.Tests;

internal static class TAuditReference
{
    private const string TAuditReferenceSource = "src/*.csproj";

    public static List<string> TAuditGeneratedRead()
    {
        List<string> files = [];
        foreach (string project in TAuditProjectRead())
        {
            string folder = TAuditTargetRead(project, "obj");
            bool markup = Directory.EnumerateFiles(
                    Path.GetDirectoryName(project)!, "*.xaml", SearchOption.AllDirectories)
                .Any(path => !TAuditBinder.TAuditRelativeRead(path).Split('/').Contains("obj", StringComparer.Ordinal));
            Assert.True(!markup || Directory.Exists(folder), TAuditConvention.TAuditReportFormat(
                "AUDITBINDER", $"No generated markup code under {folder}; build the solution before the audit."));
            if (!Directory.Exists(folder))
            {
                continue;
            }

            files.AddRange(Directory.EnumerateFiles(folder, "*.g.cs", SearchOption.AllDirectories)
                .Where(path => !Path.GetFileName(path).Contains("_wpftmp", StringComparison.Ordinal)));
        }

        return files;
    }

    private static IReadOnlyList<string> TAuditProjectRead()
    {
        TAuditScope scope = new([], [TAuditReferenceSource], [], [], [], []);
        IReadOnlyList<string> projects = TAuditSource.TAuditFileRead(TAuditBinder.TAuditRoot, scope);
        Assert.True(projects.Count > 0, TAuditConvention.TAuditReportFormat(
            "AUDITBINDER", "No tracked project file was enumerated; the binder would compile nothing."));
        return projects;
    }

    private static string TAuditTargetRead(string project, string output)
    {
        string framework = XDocument.Load(project).Descendants()
            .Where(node => node.Name.LocalName == "TargetFramework")
            .Select(node => node.Value.Trim())
            .FirstOrDefault() ?? string.Empty;
        return Path.Combine(
            Path.GetDirectoryName(project)!, output, TAuditTruthSetting.TAuditConfiguration, framework);
    }

    public static List<MetadataReference> TAuditReferenceRead()
    {
        Dictionary<string, (Version TAuditVersion, string TAuditPath)> chosen = new(StringComparer.OrdinalIgnoreCase);
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
                TAuditAssemblyAdd(chosen, path);
            }
        }

        string project = Path.Combine(
            TAuditBinder.TAuditRoot,
            TAuditTruthSetting.TAuditReferenceRoot.Replace('/', Path.DirectorySeparatorChar),
            Path.GetFileName(TAuditTruthSetting.TAuditReferenceRoot) + ".csproj");
        string built = TAuditTargetRead(project, "bin");
        Assert.True(Directory.Exists(built), TAuditConvention.TAuditReportFormat(
            "AUDITBINDER", $"No build output under {built}; build the solution before the audit."));
        string owned = TAuditNameSetting.TAuditProject;
        foreach (string path in Directory.EnumerateFiles(built, "*.dll"))
        {
            string name = Path.GetFileNameWithoutExtension(path);
            if (!name.Equals(owned, StringComparison.OrdinalIgnoreCase)
                && !name.StartsWith(owned + ".", StringComparison.OrdinalIgnoreCase))
            {
                TAuditAssemblyAdd(chosen, path);
            }
        }

        return chosen.Values
            .Select(entry => (MetadataReference)MetadataReference.CreateFromFile(entry.TAuditPath))
            .ToList();
    }

    private static void TAuditAssemblyAdd(
        Dictionary<string, (Version TAuditVersion, string TAuditPath)> chosen, string path)
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
        if (!chosen.TryGetValue(name, out (Version TAuditVersion, string TAuditPath) known)
            || known.TAuditVersion < version)
        {
            chosen[name] = (version, path);
        }
    }
}
