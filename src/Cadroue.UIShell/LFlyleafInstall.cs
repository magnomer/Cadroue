using System.Diagnostics;
using System.IO;

using Cadroue.Infrastructure;

namespace Cadroue.UIShell;

internal sealed class LFlyleafInstallResult
{
    public bool LFlyleafInstallSuccess { get; init; }

    public string LFlyleafInstallMessage { get; init; } = string.Empty;
}

internal static partial class LFlyleaf
{
    private const string LFlyleafRepositoryUrl = "https://github.com/SuRGeoNix/Flyleaf.git";

    public static async Task<LFlyleafInstallResult> LFlyleafInstallStart()
    {
        string lRoot = LFlyleafRootRead();
        string lSource = Path.Combine(lRoot, LFlyleafSourceName);
        Directory.CreateDirectory(lRoot);

        try
        {
            if (!Directory.Exists(Path.Combine(lSource, ".git")))
            {
                if (Directory.Exists(lSource))
                {
                    Directory.Delete(lSource, true);
                }

                await LFlyleafCommandRun("git", $"clone --depth 1 {LFlyleafRepositoryUrl} \"{lSource}\"", lRoot);
            }
            else
            {
                await LFlyleafCommandRun("git", "reset --hard", lSource);
                await LFlyleafCommandRun("git", "pull --ff-only", lSource);
            }

            string lShaderFile = LFlyleafShaderFind(lSource);
            LTraceLog.LTraceInfoRecord($"Local Flyleaf shader patch target: {lShaderFile}");
            LFlyleafShaderApply(lShaderFile);
            string lProjectFile = LFlyleafProjectFind(lSource, "FlyleafLib.csproj");
            await LFlyleafCommandRun("dotnet", $"build \"{lProjectFile}\" -c Release", lSource);

            string lAssemblyFolder = LFlyleafRuntimeCreate(lSource);
            LFlyleafRecordSave(new LFlyleafRecord
            {
                LFlyleafAssemblyFolder = lAssemblyFolder,
                LFlyleafSourceFolder = lSource
            });

            return new LFlyleafInstallResult
            {
                LFlyleafInstallSuccess = true,
                LFlyleafInstallMessage = LLocalization.LLocalizationTextRead("Flyleaf.Local.Install.Completed")
            };
        }
        catch (Exception lException)
        {
            LTraceLog.LTraceErrorRecord("Local Flyleaf install failed", lException);
            return new LFlyleafInstallResult
            {
                LFlyleafInstallSuccess = false,
                LFlyleafInstallMessage = LLocalization.LLocalizationFormat(
                    "Flyleaf.Local.Install.Failed",
                    lException.Message)
            };
        }
    }

    private static string LFlyleafShaderFind(string lSource)
    {
        string? lFile = Directory.EnumerateFiles(lSource, "*.cs", SearchOption.AllDirectories)
            .FirstOrDefault(lPath =>
            {
                string lText = File.ReadAllText(lPath);
                return lText.Contains("pow(c.x, 2.0 - Config.contrast)", StringComparison.Ordinal)
                    || lText.Contains("(2.0 - Config.contrast)", StringComparison.Ordinal)
                    || lText.Contains("(c.x - 0.5) * Config.contrast + 0.5", StringComparison.Ordinal)
                    || lText.Contains("(c - 0.5) * Config.contrast + 0.5", StringComparison.Ordinal);
            });
        return lFile ?? throw new FileNotFoundException("Flyleaf contrast shader source was not found.");
    }

    private static string LFlyleafProjectFind(string lSource, string lProjectName)
    {
        string? lFile = Directory.EnumerateFiles(lSource, lProjectName, SearchOption.AllDirectories)
            .FirstOrDefault();
        return lFile ?? throw new FileNotFoundException($"{lProjectName} was not found.");
    }

    private static string LFlyleafBuiltFind(string lSource)
    {
        string? lAssembly = LFlyleafAssemblyFind(lSource);
        return lAssembly is null
            ? throw new FileNotFoundException("Built FlyleafLib.dll was not found.")
            : Path.GetDirectoryName(lAssembly)!;
    }

    private static string LFlyleafRuntimeCreate(string lSource)
    {
        string lBuiltFolder = LFlyleafBuiltFind(lSource);
        string lRuntimeFolder = Path.Combine(LFlyleafRootRead(), LFlyleafRuntimeName);
        Directory.CreateDirectory(lRuntimeFolder);

        foreach (string lFile in Directory.EnumerateFiles(lBuiltFolder, "*", SearchOption.TopDirectoryOnly))
        {
            File.Copy(lFile, Path.Combine(lRuntimeFolder, Path.GetFileName(lFile)), true);
        }

        return lRuntimeFolder;
    }

    private static string? LFlyleafSourceFind(string lSource)
    {
        string? lAssembly = LFlyleafAssemblyFind(lSource);
        return lAssembly is null ? null : Path.GetDirectoryName(lAssembly);
    }

    private static string? LFlyleafAssemblyFind(string lSource)
    {
        return Directory.EnumerateFiles(lSource, "FlyleafLib.dll", SearchOption.AllDirectories)
            .Where(lPath => lPath.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(lPath => lPath.Contains($"{Path.DirectorySeparatorChar}Release{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(lPath => lPath.Contains("net10.0-windows", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();
    }

    private static void LFlyleafShaderApply(string lShaderFile)
    {
        string lText = File.ReadAllText(lShaderFile);
        string lPatched = lText
            .Replace(
                "c.x = lerp(c.x, pow(c.x, 2.0 - Config.contrast), smoothstep(0.0, 1.0, c.x));",
                "c.x = (c.x - 0.5) * Config.contrast + 0.5;")
            .Replace(
                "c = (c - 0.5) * (2.0 - Config.contrast) + 0.5;",
                "c = (c - 0.5) * Config.contrast + 0.5;");

        if (string.Equals(lText, lPatched, StringComparison.Ordinal))
        {
            if (lText.Contains("(c.x - 0.5) * Config.contrast + 0.5", StringComparison.Ordinal)
                || lText.Contains("(c - 0.5) * Config.contrast + 0.5", StringComparison.Ordinal))
            {
                LTraceLog.LTraceInfoRecord("Local Flyleaf contrast shader already patched");
                return;
            }

            throw new InvalidOperationException($"Flyleaf contrast shader formula was not found in {lShaderFile}.");
        }

        File.WriteAllText(lShaderFile, lPatched);
        LTraceLog.LTraceInfoRecord("Local Flyleaf contrast shader patched");
    }

    private static async Task LFlyleafCommandRun(string lFileName, string lArguments, string lWorkingDirectory)
    {
        LTraceLog.LTraceInfoRecord($"Local Flyleaf command: {lFileName} {lArguments}");
        using var lProcess = new Process
        {
            StartInfo = new ProcessStartInfo(lFileName, lArguments)
            {
                WorkingDirectory = lWorkingDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };

        lProcess.Start();
        string lOutput = await lProcess.StandardOutput.ReadToEndAsync();
        string lError = await lProcess.StandardError.ReadToEndAsync();
        await lProcess.WaitForExitAsync();
        if (lProcess.ExitCode != 0)
        {
            throw new InvalidOperationException($"{lFileName} failed with exit code {lProcess.ExitCode}.\n{lOutput}\n{lError}");
        }
    }
}
