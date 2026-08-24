using Xunit;

namespace Cadroue.Tests;

public sealed class SmartModeContractTests
{
    [Fact]
    public void SmartJob_WholeEncodeFallbackExistsOnlyForPlanWithoutMiddle()
    {
        string source = File.ReadAllText(SourcePathRead("src", "Cadroue.ShellEngine", "LJobSmart.cs"));
        int fallbackCount = source.Split("LEncodeWholeBuild", StringSplitOptions.None).Length - 1;

        Assert.Equal(1, fallbackCount);
    }

    private static string SourcePathRead(params string[] parts)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            string candidate = Path.Combine([directory.FullName, .. parts]);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("Could not locate production source file", Path.Combine(parts));
    }
}
