using Xunit;
using Xunit.Abstractions;

namespace Cadroue.Convention.Tests;

public sealed class TAuditSize
{
    private const int TAuditLineLimit = 500;

    private readonly ITestOutputHelper TAuditOutput;

    public TAuditSize(ITestOutputHelper output) => TAuditOutput = output;

    [Fact]
    public void SourceFiles_StayWithinTheAdvisoryLineLimit()
    {
        string repoRoot = TAuditSource.TAuditRootRead();
        IReadOnlyList<string> sources = TAuditSource.TAuditFileRead(repoRoot);

        List<(string Path, int Lines)> oversize = [];
        foreach (string path in sources)
        {
            int lines = File.ReadLines(path).Count();
            if (lines > TAuditLineLimit)
            {
                oversize.Add((path, lines));
            }
        }

        if (oversize.Count == 0)
        {
            return;
        }

        TAuditOutput.WriteLine($"ADVISORY (not a failure): {oversize.Count} file(s) exceed the {TAuditLineLimit}-line guideline.");
        TAuditOutput.WriteLine("This does not block compilation and is not a defect on its own.");
        TAuditOutput.WriteLine("Do NOT force-trim a file just to fit the number. Prefer extracting a coherent");
        TAuditOutput.WriteLine("responsibility into a new single-purpose file (C-NLRF-2, C-SRFR); splitting is");
        TAuditOutput.WriteLine("usually the right move, occasionally a documented exception is (C-EXRE).");
        TAuditOutput.WriteLine("");

        foreach ((string path, int lines) in oversize.OrderByDescending(entry => entry.Lines))
        {
            string relative = Path.GetRelativePath(repoRoot, path).Replace('\\', '/');
            TAuditOutput.WriteLine($"  {lines,5}  {relative}");
        }
    }
}
