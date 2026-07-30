using System.Diagnostics;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text.Json;
using Cadroue.Core;

namespace Cadroue.UIShell;

internal sealed class LFlyleafLocalInstallResult
{
    public bool LFlyleafLocalInstallSuccess { get; init; }

    public string LFlyleafLocalInstallMessage { get; init; } = string.Empty;
}

internal sealed class LFlyleafLocalRecord
{
    public string AssemblyFolder { get; set; } = string.Empty;

    public string SourceFolder { get; set; } = string.Empty;
}

internal static class LFlyleafLocal
{
    private const string LFlyleafLocalFolderName = "local-flyleaf";
    private const string LFlyleafSourceFolderName = "source";
    private const string LFlyleafRuntimeFolderName = "runtime";
    private const string LFlyleafRecordFileName = "local-flyleaf.json";
    private const string LFlyleafRepositoryUrl = "https://github.com/SuRGeoNix/Flyleaf.git";

    private static string? lFlyleafLocalAssemblyFolder;
    private static readonly ConcurrentDictionary<string, string> lFlyleafLocalLoadedPaths = new(StringComparer.OrdinalIgnoreCase);
    private static bool lFlyleafLocalResolverRegistered;

    [ModuleInitializer]
    internal static void LFlyleafLocalModuleInit()
    {
        try
        {
            LPreferenceState lPreferenceState = LPreferenceStateStore.LPreferenceStateLoad();
            LTrace.LTraceVerbose = lPreferenceState.LPreferenceLogVerbose;
            LDepot.LDepotRootSet(lPreferenceState.LPreferenceWorkspaceFolder);
            LFlyleafLocalResolverRegister();
        }
        catch (Exception lException)
        {
            LAppLog.LError("Local Flyleaf module initialization failed", lException);
        }
    }

    public static bool LFlyleafLocalActive =>
        LFlyleafLocalAssemblyLoadedCheck("FlyleafLib");

    private static bool LFlyleafLocalAssemblyLoadedCheck(string lAssemblyName)
    {
        if (lFlyleafLocalAssemblyFolder is null
            || !lFlyleafLocalLoadedPaths.TryGetValue(lAssemblyName, out string? lAssemblyPath))
        {
            return false;
        }

        string lLocalFolder = Path.GetFullPath(lFlyleafLocalAssemblyFolder);
        string lLoadedPath = Path.GetFullPath(lAssemblyPath);
        return lLoadedPath.StartsWith(lLocalFolder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    public static bool LFlyleafLocalAssemblyCheck(Assembly lAssembly)
    {
        if (lAssembly.GetName().Name is not { } lAssemblyName)
        {
            return false;
        }

        return LFlyleafLocalAssemblyLoadedCheck(lAssemblyName);
    }

    public static string LFlyleafLocalLoadedReportRead(Assembly lAssembly)
    {
        string lAssemblyName = lAssembly.GetName().Name ?? "(unknown)";
        string lLocation = lFlyleafLocalLoadedPaths.TryGetValue(lAssemblyName, out string? lLoadedPath)
            ? lLoadedPath
            : "(unknown)";
        string lLocal = lFlyleafLocalAssemblyFolder ?? "(none)";
        return LFlyleafLocalAssemblyCheck(lAssembly)
            ? $"Local Flyleaf loaded: {lLocation}"
            : $"NuGet Flyleaf loaded: {lLocation}; local target: {lLocal}";
    }

    public static string LFlyleafLocalRootRead() => Path.Combine(LDepot.LDepotRootRead(), LFlyleafLocalFolderName);

    public static void LFlyleafLocalResolverRegister()
    {
        if (lFlyleafLocalResolverRegistered)
        {
            return;
        }

        lFlyleafLocalResolverRegistered = true;
        lFlyleafLocalAssemblyFolder = LFlyleafLocalAssemblyFolderRead();
        LFlyleafLocalNuGetShadowApply(lFlyleafLocalAssemblyFolder is not null);
        LFlyleafLocalLoad("FlyleafLib");
        LFlyleafLocalLoad("FlyleafLib.Controls.WPF");
        AssemblyLoadContext.Default.Resolving += (_, lName) =>
        {
            if (lFlyleafLocalAssemblyFolder is null || string.IsNullOrWhiteSpace(lName.Name))
            {
                return null;
            }

            string lAssemblyPath = Path.Combine(lFlyleafLocalAssemblyFolder, $"{lName.Name}.dll");
            return LFlyleafLocalLoadPath(lAssemblyPath);
        };
    }

    private const string LFlyleafShadowSuffix = ".nugetdisabled";

    private static void LFlyleafLocalNuGetShadowApply(bool lLocalActive)
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
                    LAppLog.LInfo($"NuGet FlyleafLib shadowed off the probe path: {lShadowed}");
                }
            }
            else if (File.Exists(lShadowed) && !File.Exists(lBaseFlyleaf))
            {
                File.Move(lShadowed, lBaseFlyleaf);
                LAppLog.LInfo("NuGet FlyleafLib restored to the probe path (no local build active)");
            }
        }
        catch (Exception lException)
        {
            LAppLog.LError("Adjusting NuGet FlyleafLib on the probe path failed", lException);
        }
    }

    private static void LFlyleafLocalLoad(string lAssemblyName)
    {
        if (lFlyleafLocalAssemblyFolder is null)
        {
            return;
        }

        string lAssemblyPath = Path.Combine(lFlyleafLocalAssemblyFolder, $"{lAssemblyName}.dll");
        _ = LFlyleafLocalLoadPath(lAssemblyPath);
    }

    private static Assembly? LFlyleafLocalLoadPath(string lAssemblyPath)
    {
        if (!File.Exists(lAssemblyPath))
        {
            return null;
        }

        try
        {
            LFlyleafLocalLoadDiagnosticsRecord(lAssemblyPath);
            Assembly lAssembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(lAssemblyPath);
            if (lAssembly.GetName().Name is { } lAssemblyName)
            {
                lFlyleafLocalLoadedPaths[lAssemblyName] = lAssemblyPath;
            }

            LAppLog.LInfo($"Local Flyleaf assembly loaded: {lAssemblyPath}");
            return lAssembly;
        }
        catch (Exception lException)
        {
            LAppLog.LError($"Local Flyleaf assembly load failed: {lAssemblyPath}", lException);
            return null;
        }
    }

    private static void LFlyleafLocalLoadDiagnosticsRecord(string lAssemblyPath)
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
            LAppLog.LInfo(
                $"Local Flyleaf load probe: {lAssemblyName.Name} {lAssemblyName.Version}; "
                + $"already loaded [{(string.IsNullOrWhiteSpace(lLoaded) ? "none" : lLoaded)}]");
        }
        catch (Exception lException)
        {
            LAppLog.LError($"Local Flyleaf load probe failed: {lAssemblyPath}", lException);
        }
    }

    public static string LFlyleafLocalStatusRead()
    {
        string lRoot = LFlyleafLocalRootRead();
        if (LFlyleafLocalAssemblyFolderValidate(LFlyleafLocalAssemblyFolderRead()))
        {
            return LLocalization.LLocalizationFormat(
                "Flyleaf.Local.Status.Installed",
                LDepot.LDepotRootRead(),
                LFlyleafLocalAssemblyFolderRead());
        }

        return LLocalization.LLocalizationFormat("Flyleaf.Local.Status.NotInstalled", lRoot);
    }

    public static async Task<LFlyleafLocalInstallResult> LFlyleafLocalInstallAsync()
    {
        string lRoot = LFlyleafLocalRootRead();
        string lSource = Path.Combine(lRoot, LFlyleafSourceFolderName);
        Directory.CreateDirectory(lRoot);

        try
        {
            if (!Directory.Exists(Path.Combine(lSource, ".git")))
            {
                if (Directory.Exists(lSource))
                {
                    Directory.Delete(lSource, true);
                }

                await LFlyleafLocalRunAsync("git", $"clone --depth 1 {LFlyleafRepositoryUrl} \"{lSource}\"", lRoot);
            }
            else
            {
                await LFlyleafLocalRunAsync("git", "reset --hard", lSource);
                await LFlyleafLocalRunAsync("git", "pull --ff-only", lSource);
            }

            string lShaderFile = LFlyleafLocalShaderFileFind(lSource);
            LAppLog.LInfo($"Local Flyleaf shader patch target: {lShaderFile}");
            LFlyleafLocalShaderPatch(lShaderFile);
            string lProjectFile = LFlyleafLocalProjectFileFind(lSource, "FlyleafLib.csproj");
            await LFlyleafLocalRunAsync("dotnet", $"build \"{lProjectFile}\" -c Release", lSource);

            string lAssemblyFolder = LFlyleafLocalRuntimeFolderCreate(lSource);
            LFlyleafLocalRecordWrite(new LFlyleafLocalRecord
            {
                AssemblyFolder = lAssemblyFolder,
                SourceFolder = lSource
            });

            return new LFlyleafLocalInstallResult
            {
                LFlyleafLocalInstallSuccess = true,
                LFlyleafLocalInstallMessage = LLocalization.LLocalizationTextRead("Flyleaf.Local.Install.Completed")
            };
        }
        catch (Exception lException)
        {
            LAppLog.LError("Local Flyleaf install failed", lException);
            return new LFlyleafLocalInstallResult
            {
                LFlyleafLocalInstallSuccess = false,
                LFlyleafLocalInstallMessage = LLocalization.LLocalizationFormat(
                    "Flyleaf.Local.Install.Failed",
                    lException.Message)
            };
        }
    }

    private static string? LFlyleafLocalAssemblyFolderRead()
    {
        try
        {
            string lRecordPath = LFlyleafLocalRecordPathRead();
            if (!File.Exists(lRecordPath))
            {
                return null;
            }

            LFlyleafLocalRecord? lRecord = JsonSerializer.Deserialize<LFlyleafLocalRecord>(File.ReadAllText(lRecordPath));
            if (LFlyleafLocalAssemblyFolderValidate(lRecord?.AssemblyFolder))
            {
                return lRecord!.AssemblyFolder;
            }

            if (!string.IsNullOrWhiteSpace(lRecord?.SourceFolder)
                && Directory.Exists(lRecord.SourceFolder)
                && LFlyleafLocalAssemblyFolderTryFind(lRecord.SourceFolder) is not null)
            {
                lRecord.AssemblyFolder = LFlyleafLocalRuntimeFolderCreate(lRecord.SourceFolder);
                LFlyleafLocalRecordWrite(lRecord);
                return lRecord.AssemblyFolder;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private static bool LFlyleafLocalAssemblyFolderValidate(string? lAssemblyFolder)
    {
        if (string.IsNullOrWhiteSpace(lAssemblyFolder)
            || !File.Exists(Path.Combine(lAssemblyFolder, "FlyleafLib.dll"))
            || !File.Exists(Path.Combine(lAssemblyFolder, "FlyleafLib.deps.json")))
        {
            return false;
        }

        string lFolder = Path.GetFullPath(lAssemblyFolder);
        string lRuntimeFolder = Path.GetFullPath(Path.Combine(LFlyleafLocalRootRead(), LFlyleafRuntimeFolderName));
        return string.Equals(lFolder, lRuntimeFolder, StringComparison.OrdinalIgnoreCase)
            && !lFolder.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
            && !lFolder.EndsWith($"{Path.DirectorySeparatorChar}ref", StringComparison.OrdinalIgnoreCase)
            && !lFolder.EndsWith($"{Path.DirectorySeparatorChar}refint", StringComparison.OrdinalIgnoreCase);
    }

    private static string LFlyleafLocalRecordPathRead() =>
        Path.Combine(LFlyleafLocalRootRead(), LFlyleafRecordFileName);

    private static void LFlyleafLocalRecordWrite(LFlyleafLocalRecord lRecord)
    {
        File.WriteAllText(
            LFlyleafLocalRecordPathRead(),
            JsonSerializer.Serialize(lRecord, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static string LFlyleafLocalShaderFileFind(string lSource)
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

    private static string LFlyleafLocalProjectFileFind(string lSource, string lProjectName)
    {
        string? lFile = Directory.EnumerateFiles(lSource, lProjectName, SearchOption.AllDirectories)
            .FirstOrDefault();
        return lFile ?? throw new FileNotFoundException($"{lProjectName} was not found.");
    }

    private static string LFlyleafLocalAssemblyFolderFind(string lSource)
    {
        string? lAssembly = LFlyleafLocalAssemblyFileFind(lSource);
        return lAssembly is null
            ? throw new FileNotFoundException("Built FlyleafLib.dll was not found.")
            : Path.GetDirectoryName(lAssembly)!;
    }

    private static string LFlyleafLocalRuntimeFolderCreate(string lSource)
    {
        string lBuiltFolder = LFlyleafLocalAssemblyFolderFind(lSource);
        string lRuntimeFolder = Path.Combine(LFlyleafLocalRootRead(), LFlyleafRuntimeFolderName);
        Directory.CreateDirectory(lRuntimeFolder);

        foreach (string lFile in Directory.EnumerateFiles(lBuiltFolder, "*", SearchOption.TopDirectoryOnly))
        {
            File.Copy(lFile, Path.Combine(lRuntimeFolder, Path.GetFileName(lFile)), true);
        }

        return lRuntimeFolder;
    }

    private static string? LFlyleafLocalAssemblyFolderTryFind(string lSource)
    {
        string? lAssembly = LFlyleafLocalAssemblyFileFind(lSource);
        return lAssembly is null ? null : Path.GetDirectoryName(lAssembly);
    }

    private static string? LFlyleafLocalAssemblyFileFind(string lSource)
    {
        return Directory.EnumerateFiles(lSource, "FlyleafLib.dll", SearchOption.AllDirectories)
            .Where(lPath => lPath.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(lPath => lPath.Contains($"{Path.DirectorySeparatorChar}Release{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(lPath => lPath.Contains("net10.0-windows", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();
    }

    private static void LFlyleafLocalShaderPatch(string lShaderFile)
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
                LAppLog.LInfo("Local Flyleaf contrast shader already patched");
                return;
            }

            throw new InvalidOperationException($"Flyleaf contrast shader formula was not found in {lShaderFile}.");
        }

        File.WriteAllText(lShaderFile, lPatched);
        LAppLog.LInfo("Local Flyleaf contrast shader patched");
    }

    private static async Task LFlyleafLocalRunAsync(string lFileName, string lArguments, string lWorkingDirectory)
    {
        LAppLog.LInfo($"Local Flyleaf command: {lFileName} {lArguments}");
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
