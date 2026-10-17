using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Cadroue.Infrastructure;

public static class LMpvLibrary
{
    internal const string LMpvLibraryFile = "libmpv-2.dll";

    private static bool lMpvResolverActive;
    private static readonly object lMpvResolverGate = new();

    public static string LMpvLibraryRead()
    {
        string? lInstallFolder = LMpvInstall.LMpvFolderRead();
        if (lInstallFolder is not null)
        {
            return Path.Combine(lInstallFolder, LMpvLibraryFile);
        }

        string lFolder = LRendererFolder.LRendererFolderCurrent;
        if (!string.IsNullOrWhiteSpace(lFolder))
        {
            string lCandidate = Path.Combine(lFolder, LMpvLibraryFile);
            if (File.Exists(lCandidate))
            {
                return lCandidate;
            }
        }

        return LMpvLibraryFile;
    }

    public static bool LMpvLibraryCheck()
    {
        string lPath = LMpvLibraryRead();
        if (Path.IsPathRooted(lPath))
        {
            return File.Exists(lPath);
        }

        if (!NativeLibrary.TryLoad(lPath, out nint lHandle))
        {
            return false;
        }

        NativeLibrary.Free(lHandle);
        return true;
    }

    internal static void LMpvLibraryAttach()
    {
        if (lMpvResolverActive)
        {
            return;
        }

        lock (lMpvResolverGate)
        {
            if (lMpvResolverActive)
            {
                return;
            }

            NativeLibrary.SetDllImportResolver(typeof(LMpvNative).Assembly, LMpvLibraryResolve);
            lMpvResolverActive = true;
        }
    }

    private static nint LMpvLibraryResolve(string lLibraryName, Assembly lAssembly, DllImportSearchPath? lSearchPath)
    {
        if (!string.Equals(lLibraryName, LMpvNative.LMpvLibraryName, StringComparison.Ordinal))
        {
            return nint.Zero;
        }

        string lResolvedPath = LMpvLibraryRead();
        if (File.Exists(lResolvedPath) && NativeLibrary.TryLoad(lResolvedPath, out nint lLoaded))
        {
            return lLoaded;
        }

        return NativeLibrary.TryLoad(LMpvLibraryFile, lAssembly, lSearchPath, out nint lFallback)
            ? lFallback
            : nint.Zero;
    }
}
