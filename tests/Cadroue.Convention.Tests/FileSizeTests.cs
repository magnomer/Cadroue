using Xunit;
using Xunit.Abstractions;

namespace Cadroue.Convention.Tests;

public sealed class FileSizeTests
{
    private const int LineLimit = 500;

    private readonly ITestOutputHelper _output;

    public FileSizeTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public void SourceFiles_StayWithinTheAdvisoryLineLimit()
    {
        string repoRoot = AuditSource.RepoRoot();
        IReadOnlyList<string> sources = AuditSource.Files(repoRoot);

        List<(string Path, int Lines)> oversize = [];
        foreach (string path in sources)
        {
            int lines = File.ReadLines(path).Count();
            if (lines > LineLimit)
            {
                oversize.Add((path, lines));
            }
        }

        if (oversize.Count == 0)
        {
            return;
        }

        _output.WriteLine($"ADVISORY (not a failure): {oversize.Count} file(s) exceed the {LineLimit}-line guideline.");
        _output.WriteLine("This does not block compilation and is not a defect on its own.");
        _output.WriteLine("Do NOT force-trim a file just to fit the number. Prefer extracting a coherent");
        _output.WriteLine("responsibility into a new single-purpose file (C-NLRF-2, C-SRFR); splitting is");
        _output.WriteLine("usually the right move, occasionally a documented exception is (C-EXRE).");
        _output.WriteLine("");

        foreach ((string path, int lines) in oversize.OrderByDescending(entry => entry.Lines))
        {
            string relative = Path.GetRelativePath(repoRoot, path).Replace('\\', '/');
            _output.WriteLine($"  {lines,5}  {relative}");
        }
    }
}
