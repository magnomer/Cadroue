using System.Diagnostics;
using Xunit;

namespace Convention.Tests;

internal static class TAuditRatchetFile
{
    public const string TAuditSettingSuffix = "Setting.cs";

    public const string TAuditLedgerSuffix = "Ledger.json";

    public static readonly string TAuditSettingFolder =
        $"tests/{TAuditNameSetting.TAuditProject}.Tests.Convention/";

    public static IReadOnlyList<string> TAuditTreeRead()
    {
        string folder = Path.Combine(TAuditSource.TAuditRootRead(), TAuditSettingFolder);
        return Directory.EnumerateFiles(folder)
            .Select(path => Path.GetFileName(path))
            .Where(TAuditHeldCheck)
            .Order(StringComparer.Ordinal)
            .ToList();
    }

    public static IReadOnlyList<string> TAuditHeadRead()
    {
        string? listing = TAuditGitRead("ls-tree", "--name-only", "HEAD", TAuditSettingFolder);
        Assert.True(listing is not null, TAuditConvention.TAuditReportFormat(
            TAuditRatchet.TAuditRatchetAudit,
            $"git cannot list {TAuditSettingFolder} at HEAD, so no setting can be held."));
        return listing.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(path => path[(path.LastIndexOf('/') + 1)..])
            .Where(TAuditHeldCheck)
            .Order(StringComparer.Ordinal)
            .ToList();
    }

    private static bool TAuditHeldCheck(string name)
    {
        return name.StartsWith("TAudit", StringComparison.Ordinal)
               && (name.EndsWith(TAuditSettingSuffix, StringComparison.Ordinal)
                   || name.EndsWith(TAuditLedgerSuffix, StringComparison.Ordinal));
    }

    public static string TAuditWorkingRead(string name)
    {
        return File.ReadAllText(Path.Combine(TAuditSource.TAuditRootRead(), TAuditSettingFolder, name));
    }

    public static string? TAuditCommittedRead(string path)
    {
        return TAuditGitRead("show", $"HEAD:{path}");
    }

    private static string? TAuditGitRead(params string[] arguments)
    {
        ProcessStartInfo info = new("git")
        {
            WorkingDirectory = TAuditSource.TAuditRootRead(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (string argument in arguments)
        {
            info.ArgumentList.Add(argument);
        }

        using Process? process = Process.Start(info);
        if (process is null)
        {
            return null;
        }

        string output = process.StandardOutput.ReadToEnd();
        process.StandardError.ReadToEnd();
        process.WaitForExit();
        return process.ExitCode == 0 ? output : null;
    }
}
