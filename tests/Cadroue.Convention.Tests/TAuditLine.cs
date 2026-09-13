using Xunit;
using Xunit.Abstractions;

namespace Convention.Tests;

public sealed class TAuditLine
{
    private readonly ITestOutputHelper TAuditOutput;

    public TAuditLine(ITestOutputHelper output) => TAuditOutput = output;

    [Fact]
    public void AuditLine_OversizeFile_ReportsAsAdvisory()
    {
        string repoRoot = TAuditSource.TAuditRootRead();
        TAuditScope scope = new(
            TAuditLineSetting.TAuditLineRoots,
            TAuditLineSetting.TAuditLineInclude,
            TAuditLineSetting.TAuditLineSegments,
            [],
            [],
            []);
        IReadOnlyList<string> sources = TAuditSource.TAuditFileRead(repoRoot, scope);

        List<(string TAuditPath, int TAuditLines)> oversize = [];
        foreach (string path in sources)
        {
            int lines = File.ReadLines(path).Count();
            if (lines >= TAuditLineSetting.TAuditLineLimit)
            {
                oversize.Add((path, lines));
            }
        }

        if (oversize.Count == 0)
        {
            return;
        }

        TAuditOutput.WriteLine(TAuditConvention.TAuditReportFormat(
            "AUDITLINES",
            $"ADVISORY (not a failure): {oversize.Count} file(s) reach the {TAuditLineSetting.TAuditLineLimit}-line guideline."));
        TAuditOutput.WriteLine("This does not block compilation and is not a defect on its own.");
        TAuditOutput.WriteLine("Do NOT force-trim a file just to fit the number. Prefer extracting a coherent");
        TAuditOutput.WriteLine("responsibility into a new single-purpose file (C-NLRF-2, C-SRFR); splitting is");
        TAuditOutput.WriteLine("usually the right move, occasionally a documented exception is (C-EXRE).");
        TAuditOutput.WriteLine("");

        foreach ((string path, int lines) in oversize.OrderByDescending(entry => entry.TAuditLines))
        {
            string relative = Path.GetRelativePath(repoRoot, path).Replace('\\', '/');
            TAuditOutput.WriteLine($"  {lines,5}  {relative}");
        }
    }
}
