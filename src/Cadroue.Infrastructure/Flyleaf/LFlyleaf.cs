using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text.Json;
using Cadroue.Core;

namespace Cadroue.Infrastructure;

internal sealed class LFlyleafRecord
{
    public string LFlyleafAssemblyFolder { get; set; } = string.Empty;

    public string LFlyleafSourceFolder { get; set; } = string.Empty;
}

public static partial class LFlyleaf
{
    private const string LFlyleafRootFolder = "local-flyleaf";
    private const string LFlyleafSourceName = "source";
    private const string LFlyleafRuntimeName = "runtime";
    private const string LFlyleafRecordName = "local-flyleaf.json";

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

    public static bool LFlyleafInstalledCheck() => LFlyleafFolderValidate(LFlyleafFolderRead());

    public static string? LFlyleafFolderRead()
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
}
