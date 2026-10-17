using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace Cadroue.Infrastructure;

public static class LFlyleaf
{
    private static string? lFlyleafAssemblyFolder;
    private static readonly ConcurrentDictionary<string, string> lFlyleafLoadedPaths =
        new(StringComparer.OrdinalIgnoreCase);
    private static bool lFlyleafResolverActive;

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

    public static void LFlyleafResolverAttach()
    {
        if (lFlyleafResolverActive)
        {
            return;
        }

        lFlyleafResolverActive = true;
        lFlyleafAssemblyFolder = LFlyleafFolder.LFlyleafFolderRead();
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
                    LTraceLog.LTraceLoadingRecord(
                        "NuGet FlyleafLib disabled",
                        $"NuGet FlyleafLib shadowed off the probe path: {lShadowed}");
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

            LTraceLog.LTraceLoadingRecord(
                "Local Flyleaf loaded",
                $"Local Flyleaf assembly loaded: {lAssemblyPath}");
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
            LTraceLog.LTraceLoadingRecord(
                "Local Flyleaf probed",
                $"Local Flyleaf load probe: {lAssemblyName.Name} {lAssemblyName.Version}; "
                + $"already loaded [{(string.IsNullOrWhiteSpace(lLoaded) ? "none" : lLoaded)}]");
        }
        catch (Exception lException)
        {
            LTraceLog.LTraceErrorRecord($"Local Flyleaf load probe failed: {lAssemblyPath}", lException);
        }
    }
}
