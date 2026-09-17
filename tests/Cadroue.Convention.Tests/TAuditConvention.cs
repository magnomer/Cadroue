using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace Convention.Tests;

public sealed class TAuditConvention
{
    public const int TAuditGeneration = 8;

    private static readonly Regex TAuditLiteralPattern = new(
        @"(?<hole>\$@?""(?:[^""\\]|\\.)*"")|@?""(?:[^""\\]|\\.)*""|//.*$",
        RegexOptions.Compiled);

    private static readonly Regex TAuditHolePattern = new(@"\{[^{}]*\}", RegexOptions.Compiled);

    private static readonly Regex TAuditTypePattern = new(
        @"^\s*(?:(?:public|internal|private|protected|sealed|static|partial|readonly|ref|abstract)\s+)*"
        + @"(?<kind>class|struct|record\s+struct|record|interface|enum)\s+\w+",
        RegexOptions.Compiled);

    private static readonly Regex TAuditFieldPattern = new(
        @"^\s*(?:public|internal|private|protected)\s+(?:(?:static|volatile|new)\s+)*"
        + @"(?<type>[A-Za-z]\w*)(?:\?|\[\])*\s+(?<name>\w+)\s*(?:=(?!>)|;)",
        RegexOptions.Compiled);

    private readonly ITestOutputHelper TAuditOutput;

    public TAuditConvention(ITestOutputHelper output) => TAuditOutput = output;

    public static string TAuditReportFormat(string audit, string report)
    {
        return $"{audit} GENERATION {TAuditGeneration}\n{report}";
    }

    [Fact]
    public void AuditGate_Veneer_CarriesNoIoOrProcess()
    {
        TAuditTokenCheck(
            TAuditGateSetting.TAuditVeneerRoot,
            TAuditGateSetting.TAuditVeneerForbidden,
            TAuditGateSetting.TAuditVeneerTolerated,
            TAuditGateSetting.TAuditVeneerScoped);
    }

    [Fact]
    public void AuditGate_Deportment_CarriesNoWpfOrIo()
    {
        TAuditTokenCheck(
            TAuditGateSetting.TAuditDeportmentRoot,
            TAuditGateSetting.TAuditDeportmentForbidden,
            TAuditGateSetting.TAuditDeportmentTolerated,
            []);
    }

    [Fact]
    public void AuditGate_Veneer_CarriesOnlyTransientScalars()
    {
        TAuditScalarCheck();
    }

    private static string TAuditLineNormalize(string line) => TAuditLiteralPattern.Replace(
        line,
        match => match.Groups["hole"].Success
            ? string.Concat(TAuditHolePattern.Matches(match.Value).Select(hole => hole.Value))
            : string.Empty);

    private void TAuditTokenCheck(
        string root,
        string[] forbidden,
        string[] tolerated,
        (string TAuditFile, string TAuditSpelling)[] scoped)
    {
        Regex[] patterns = forbidden.Select(entry => new Regex(entry, RegexOptions.Compiled)).ToArray();
        List<string> hits = [];
        foreach ((string relative, string[] lines) in TAuditGateRead(root))
        {
            string[] spellings = tolerated
                .Concat(scoped.Where(entry => entry.TAuditFile == relative).Select(entry => entry.TAuditSpelling))
                .ToArray();
            for (int index = 0; index < lines.Length; index++)
            {
                string bare = TAuditLineNormalize(lines[index]);
                foreach (string spelling in spellings)
                {
                    bare = bare.Replace(spelling, string.Empty, StringComparison.Ordinal);
                }

                Regex? hit = patterns.FirstOrDefault(pattern => pattern.IsMatch(bare));
                if (hit is not null)
                {
                    hits.Add($"  {root}/{relative}:{index + 1} {hit}");
                }
            }
        }

        TAuditOutput.WriteLine(TAuditReportFormat("AUDITGATE", $"{root}: {hits.Count} forbidden token line(s)."));
        Assert.True(hits.Count == 0, TAuditReportFormat(
            "AUDITGATE",
            $"{hits.Count} line(s) in {root} break the Veneer/Deportment gate (C-5, U-VD).\n"
            + string.Join('\n', hits)));
    }

    private void TAuditScalarCheck()
    {
        string root = TAuditGateSetting.TAuditVeneerRoot;
        Regex guard = new($"^{TAuditGateSetting.TAuditGuardPattern}$", RegexOptions.Compiled);
        List<string> hits = [];
        foreach ((string relative, string[] lines) in TAuditGateRead(root))
        {
            if (TAuditGateSetting.TAuditScalarExempt.Contains(relative, StringComparer.Ordinal))
            {
                continue;
            }

            Stack<(string TAuditKind, int TAuditDepth)> types = new();
            string? pending = null;
            int depth = 0;
            for (int index = 0; index < lines.Length; index++)
            {
                string bare = TAuditLineNormalize(lines[index]);
                if (TAuditTypePattern.Match(bare) is { Success: true } type)
                {
                    pending = type.Groups["kind"].Value;
                }

                if (TAuditFieldPattern.Match(bare) is { Success: true } field
                    && !bare.Contains(" const ", StringComparison.Ordinal)
                    && !bare.Contains(" readonly ", StringComparison.Ordinal)
                    && types.Count > 0
                    && !types.Peek().TAuditKind.Contains("struct", StringComparison.Ordinal))
                {
                    string name = field.Groups["name"].Value;
                    bool scalar = TAuditGateSetting.TAuditScalarType.Contains(
                        field.Groups["type"].Value, StringComparer.Ordinal);
                    bool transient = TAuditGateSetting.TAuditScalarSuffix.Any(
                        suffix => name.EndsWith(suffix, StringComparison.Ordinal));
                    bool known = TAuditGateSetting.TAuditScalarKnown.Contains((relative, name));
                    if (guard.IsMatch(name))
                    {
                        hits.Add($"  {root}/{relative}:{index + 1} guard field {name}");
                    }
                    else if (scalar && !transient && !known)
                    {
                        hits.Add($"  {root}/{relative}:{index + 1} scalar field {name}");
                    }
                }

                foreach (char symbol in bare)
                {
                    if (symbol == '{')
                    {
                        depth++;
                        if (pending is not null)
                        {
                            types.Push((pending, depth));
                            pending = null;
                        }
                    }
                    else if (symbol == '}')
                    {
                        if (types.Count > 0 && types.Peek().TAuditDepth == depth)
                        {
                            types.Pop();
                        }

                        depth--;
                    }
                }

                if (pending is not null && bare.TrimEnd().EndsWith(';'))
                {
                    pending = null;
                }
            }
        }

        TAuditOutput.WriteLine(TAuditReportFormat("AUDITGATE", $"{root}: {hits.Count} scalar/guard field line(s)."));
        Assert.True(hits.Count == 0, TAuditReportFormat(
            "AUDITGATE",
            $"{hits.Count} field(s) in {root} carry state outside the gesture-transient set (C-5, U-VD).\n"
            + string.Join('\n', hits)));
    }

    private static IEnumerable<(string TAuditRelative, string[] TAuditLines)> TAuditGateRead(string root)
    {
        string repoRoot = TAuditSource.TAuditRootRead();
        TAuditScope scope = new(
            [root],
            TAuditGateSetting.TAuditGateInclude,
            TAuditGateSetting.TAuditGateSegments,
            [],
            [],
            []);
        string rootFull = Path.Combine(repoRoot, root.Replace('/', Path.DirectorySeparatorChar));
        foreach (string path in TAuditSource.TAuditFileRead(repoRoot, scope))
        {
            yield return (Path.GetRelativePath(rootFull, path).Replace('\\', '/'), File.ReadAllLines(path));
        }
    }

    [Fact]
    public void AuditConvention_SettingGenerations_MatchTheTooling()
    {
        (string TSidecarName, int TSidecarGeneration)[] sidecars =
        [
            (nameof(TAuditNameRegistry), TAuditNameRegistry.TAuditGeneration),
            (nameof(TAuditNameSetting), TAuditNameSetting.TAuditGeneration),
            (nameof(TAuditLineSetting), TAuditLineSetting.TAuditGeneration),
            (nameof(TAuditCommentSetting), TAuditCommentSetting.TAuditGeneration),
            (nameof(TAuditGateSetting), TAuditGateSetting.TAuditGeneration),
        ];

        string[] stale = sidecars
            .Where(sidecar => sidecar.TSidecarGeneration != TAuditGeneration)
            .Select(sidecar => $"  {sidecar.TSidecarName} is generation {sidecar.TSidecarGeneration}")
            .ToArray();

        Assert.True(stale.Length == 0, TAuditReportFormat(
            "AUDITCONVENTION",
            $"{stale.Length} sidecar(s) differ from this tooling. Rerun the audit that generates each.\n"
            + string.Join('\n', stale)));
    }
}
