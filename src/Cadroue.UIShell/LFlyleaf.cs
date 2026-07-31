using System.Diagnostics;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text.Json;
using Cadroue.Core;

namespace Cadroue.UIShell;

internal sealed class LFlyleafInstallResult
{
    public bool LFlyleafInstallSuccess { get; init; }

    public string LFlyleafInstallMessage { get; init; } = string.Empty;
}

internal sealed class LFlyleafRecord
{
    public string LFlyleafAssemblyFolder { get; set; } = string.Empty;

    public string LFlyleafSourceFolder { get; set; } = string.Empty;
}

internal static class LFlyleaf
{
    private const string LFlyleafRootFolder = "local-flyleaf";
    private const string LFlyleafSourceName = "source";
    private const string LFlyleafRuntimeName = "runtime";
    private const string LFlyleafRecordName = "local-flyleaf.json";
    private const string LFlyleafRepositoryUrl = "https://github.com/SuRGeoNix/Flyleaf.git";

    private static string? lFlyleafAssemblyFolder;
    private static readonly ConcurrentDictionary<string, string> lFlyleafLoadedPaths = new(StringComparer.OrdinalIgnoreCase);
    private static bool lFlyleafResolverActive;

    [ModuleInitializer]
    internal static void LFlyleafModuleStart()
    {
        try
        {
            LPreferenceState lPreferenceState = LPreferenceStateStore.LPreferenceStateLoad();
            LTrace.LTraceVerbose = lPreferenceState.LPreferenceLogVerbose;
            LDepot.LDepotRootSet(lPreferenceState.LPreferenceWorkspaceFolder);
            LFlyleafResolverAttach();
        }
        catch (Exception lException)
        {
            LTraceLog.LTraceErrorRecord("Local Flyleaf module initialization failed", lException);
        }
    }

    public static bool LFlyleafActive =>
        LFlyleafLoadedCheck("FlyleafLib");

    private static bool LFlyleafLoadedCheck(string lAssemblyName)
    {
        if (lFlyleafAssemblyFolder is null
            || !lFlyleafLoadedPaths.TryGetValue(lAssemblyName, out string? lAssemblyPath))
        {
            return false;
        }

        string lLocalFolder = Path.GetFullPath(lFlyleafAssemblyFolder);
        string lLoadedPath = Path.GetFullPath(lAssemblyPath);
        return lLoadedPath.StartsWith(lLocalFolder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    public static bool LFlyleafAssemblyCheck(Assembly lAssembly)
    {
        if (lAssembly.GetName().Name is not { } lAssemblyName)
        {
            return false;
        }

        return LFlyleafLoadedCheck(lAssemblyName);
    }

    public static string LFlyleafReportRead(Assembly lAssembly)
    {
        string lAssemblyName = lAssembly.GetName().Name ?? "(unknown)";
        string lLocation = lFlyleafLoadedPaths.TryGetValue(lAssemblyName, out string? lLoadedPath)
            ? lLoadedPath
            : "(unknown)";
        string lLocal = lFlyleafAssemblyFolder ?? "(none)";
        return LFlyleafAssemblyCheck(lAssembly)
            ? $"Local Flyleaf loaded: {lLocation}"
            : $"NuGet Flyleaf loaded: {lLocation}; local target: {lLocal}";
    }

    public static string LFlyleafRootRead() => Path.Combine(LDepot.LDepotRootRead(), LFlyleafRootFolder);

    public static void LFlyleafResolverAttach()
    {
        if (lFlyleafResolverActive)
        {
            return;
        }

        lFlyleafResolverActive = true;
        lFlyleafAssemblyFolder = LFlyleafFolderRead();
        LFlyleafShadowApply(lFlyleafAssemblyFolder is not null);
        LFlyleafLoad("FlyleafLib");
        LFlyleafLoad("FlyleafLib.Controls.WPF");
        AssemblyLoadContext.Default.Resolving += (_, lName) =>
        {
            if (lFlyleafAssemblyFolder is null || string.IsNullOrWhiteSpace(lName.Name))
            {
                return null;
            }

            string lAssemblyPath = Path.Combine(lFlyleafAssemblyFolder, $"{lName.Name}.dll");
            return LFlyleafPathLoad(lAssemblyPath);
        };
    }

    private const string LFlyleafShadowSuffix = ".nugetdisabled";

    private static void LFlyleafShadowApply(bool lLocalActive)
    {
        string lBaseFlyleaf = Path.Combine(AppContext.BaseDirectory, "FlyleafLib.dll");
        string lShadowed = lBaseFlyleaf + LFlyleafShadowSuffix;
        try
        {
            if (lLocalActive)
            {
                if (File.Exists(lBaseFlyleaf))
                {
                    if (File.Exists(lShadowed))
                    {
                        File.Delete(lShadowed);
                    }

                    File.Move(lBaseFlyleaf, lShadowed);
                    LTraceLog.LTraceInfoRecord($"NuGet FlyleafLib shadowed off the probe path: {lShadowed}");
                }
            }
            else if (File.Exists(lShadowed) && !File.Exists(lBaseFlyleaf))
            {
                File.Move(lShadowed, lBaseFlyleaf);
                LTraceLog.LTraceInfoRecord("NuGet FlyleafLib restored to the probe path (no local build active)");
            }
        }
        catch (Exception lException)
        {
            LTraceLog.LTraceErrorRecord("Adjusting NuGet FlyleafLib on the probe path failed", lException);
        }
    }

    private static void LFlyleafLoad(string lAssemblyName)
    {
        if (lFlyleafAssemblyFolder is null)
        {
            return;
        }

        string lAssemblyPath = Path.Combine(lFlyleafAssemblyFolder, $"{lAssemblyName}.dll");
        _ = LFlyleafPathLoad(lAssemblyPath);
    }

    private static Assembly? LFlyleafPathLoad(string lAssemblyPath)
    {
        if (!File.Exists(lAssemblyPath))
        {
            return null;
        }

        try
        {
            LFlyleafProbeRecord(lAssemblyPath);
            Assembly lAssembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(lAssemblyPath);
            if (lAssembly.GetName().Name is { } lAssemblyName)
            {
                lFlyleafLoadedPaths[lAssemblyName] = lAssemblyPath;
            }

            LTraceLog.LTraceInfoRecord($"Local Flyleaf assembly loaded: {lAssemblyPath}");
            return lAssembly;
        }
        catch (Exception lException)
        {
            LTraceLog.LTraceErrorRecord($"Local Flyleaf assembly load failed: {lAssemblyPath}", lException);
            return null;
        }
    }

    private static void LFlyleafProbeRecord(string lAssemblyPath)
    {
        try
        {
            AssemblyName lAssemblyName = AssemblyName.GetAssemblyName(lAssemblyPath);
            string lLoaded = string.Join(
                "; ",
                AppDomain.CurrentDomain.GetAssemblies()
                    .Where(lAssembly => string.Equals(
                        lAssembly.GetName().Name,
                        lAssemblyName.Name,
                        StringComparison.OrdinalIgnoreCase))
                    .Select(lAssembly => $"{lAssembly.GetName().Name} {lAssembly.GetName().Version}"));
            LTraceLog.LTraceInfoRecord(
                $"Local Flyleaf load probe: {lAssemblyName.Name} {lAssemblyName.Version}; "
                + $"already loaded [{(string.IsNullOrWhiteSpace(lLoaded) ? "none" : lLoaded)}]");
        }
        catch (Exception lException)
        {
            LTraceLog.LTraceErrorRecord($"Local Flyleaf load probe failed: {lAssemblyPath}", lException);
        }
    }

    public static string LFlyleafStatusRead()
    {
        string lRoot = LFlyleafRootRead();
        if (LFlyleafFolderValidate(LFlyleafFolderRead()))
        {
            return LLocalization.LLocalizationFormat(
                "Flyleaf.Local.Status.Installed",
                LDepot.LDepotRootRead(),
                LFlyleafFolderRead());
        }

        return LLocalization.LLocalizationFormat("Flyleaf.Local.Status.NotInstalled", lRoot);
    }

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

    private static string? LFlyleafFolderRead()
    {
        try
        {
            string lRecordPath = LFlyleafRecordFind();
            if (!File.Exists(lRecordPath))
            {
                return null;
            }

            LFlyleafRecord? lRecord = JsonSerializer.Deserialize<LFlyleafRecord>(File.ReadAllText(lRecordPath));
            if (LFlyleafFolderValidate(lRecord?.LFlyleafAssemblyFolder))
            {
                return lRecord!.LFlyleafAssemblyFolder;
            }

            if (!string.IsNullOrWhiteSpace(lRecord?.LFlyleafSourceFolder)
                && Directory.Exists(lRecord.LFlyleafSourceFolder)
                && LFlyleafSourceFind(lRecord.LFlyleafSourceFolder) is not null)
            {
                lRecord.LFlyleafAssemblyFolder = LFlyleafRuntimeCreate(lRecord.LFlyleafSourceFolder);
                LFlyleafRecordSave(lRecord);
                return lRecord.LFlyleafAssemblyFolder;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private static bool LFlyleafFolderValidate(string? lAssemblyFolder)
    {
        if (string.IsNullOrWhiteSpace(lAssemblyFolder)
            || !File.Exists(Path.Combine(lAssemblyFolder, "FlyleafLib.dll"))
            || !File.Exists(Path.Combine(lAssemblyFolder, "FlyleafLib.deps.json")))
        {
            return false;
        }

        string lFolder = Path.GetFullPath(lAssemblyFolder);
        string lRuntimeFolder = Path.GetFullPath(Path.Combine(LFlyleafRootRead(), LFlyleafRuntimeName));
        return string.Equals(lFolder, lRuntimeFolder, StringComparison.OrdinalIgnoreCase)
            && !lFolder.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
            && !lFolder.EndsWith($"{Path.DirectorySeparatorChar}ref", StringComparison.OrdinalIgnoreCase)
            && !lFolder.EndsWith($"{Path.DirectorySeparatorChar}refint", StringComparison.OrdinalIgnoreCase);
    }

    private static string LFlyleafRecordFind() =>
        Path.Combine(LFlyleafRootRead(), LFlyleafRecordName);

    private static void LFlyleafRecordSave(LFlyleafRecord lRecord)
    {
        File.WriteAllText(
            LFlyleafRecordFind(),
            JsonSerializer.Serialize(lRecord, new JsonSerializerOptions { WriteIndented = true }));
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
