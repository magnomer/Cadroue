using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cadroue.Core;

namespace Cadroue.Infrastructure;

public sealed partial class LMpv : IDisposable
{
    private const string LMpvLibraryFile = "libmpv-2.dll";

    private static readonly TimeSpan LMpvTeardownBudget = TimeSpan.FromSeconds(5);

    private static bool lMpvResolverActive;
    private static readonly object lMpvResolverGate = new();
    private static readonly object lMpvTeardownGate = new();
    private static Task? lMpvTeardownTask;

    private readonly object lMpvScanGate = new();
    private CancellationTokenSource? lMpvScanCancellation;

    private nint lMpvContext;

    public LMpv()
    {
        LMpvResolverAttach();
    }

    public string LMpvLibraryPath { get; } = LMpvLibraryRead();

    public bool LMpvContextActive => lMpvContext != nint.Zero;

    public static string LMpvLibraryRead()
    {
        string? lInstallFolder = LMpvFolderRead();
        if (lInstallFolder is not null)
        {
            return Path.Combine(lInstallFolder, LMpvLibraryFile);
        }

        string lFolder = LRenderer.LRendererFolderCurrent;
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

    public static bool LMpvAvailableCheck() =>
        LMpvLibraryCheck() && LMpvResultRead() == LMpvProbe.LMpvProbeUsable;

    public void LMpvContextCreate(nint lWindowHandle)
    {
        if (lMpvContext != nint.Zero)
        {
            throw new InvalidOperationException("mpv handle already created.");
        }

        Task? lTeardown;
        lock (lMpvTeardownGate)
        {
            lTeardown = lMpvTeardownTask;
        }

        try
        {
            lTeardown?.Wait(LMpvTeardownBudget);
        }
        catch (AggregateException)
        {
        }

        nint lHandle = LMpvNative.mpv_create();
        if (lHandle == nint.Zero)
        {
            throw new InvalidOperationException($"mpv_create failed (libmpv path: {LMpvLibraryPath}).");
        }

        lMpvContext = lHandle;
        LMpvOptionSet("background-color", "#FFFFFF");
        LMpvOptionSet("sub-auto", "no");
        LMpvOptionSet("input-default-bindings", "no");
        LMpvOptionSet("input-vo-keyboard", "no");
        LMpvOptionSet("osc", "no");
        LMpvOptionSet("keep-open", "yes");
        if (lWindowHandle != nint.Zero)
        {
            LMpvOptionSet("wid", lWindowHandle.ToString());
        }
        else
        {
            LMpvOptionSet("vo", "null");
            LMpvOptionSet("ao", "null");
        }

        int lResult = LMpvNative.mpv_initialize(lMpvContext);
        LMpvResultCheck(lResult, "mpv_initialize");
    }

    public void LMpvOptionSet(string lName, string lData)
    {
        LMpvContextValidate();
        int lResult = LMpvNative.mpv_set_option_string(lMpvContext, lName, lData);
        LMpvResultCheck(lResult, $"mpv_set_option_string {lName}={lData}");
    }

    public void LMpvPropertySet(string lName, string lData)
    {
        LMpvContextValidate();
        int lResult = LMpvNative.mpv_set_property_string(lMpvContext, lName, lData);
        LMpvResultCheck(lResult, $"mpv_set_property_string {lName}={lData}");
    }

    public void LMpvCommandRun(params string[] lArguments)
    {
        LMpvContextValidate();
        if (lArguments.Length == 0)
        {
            throw new ArgumentException("mpv_command needs at least one argument.", nameof(lArguments));
        }

        nint[] lPointers = new nint[lArguments.Length + 1];
        nint lArray = nint.Zero;
        try
        {
            for (int lIndex = 0; lIndex < lArguments.Length; lIndex++)
            {
                lPointers[lIndex] = LMpvStringCreate(lArguments[lIndex]);
            }

            lPointers[lArguments.Length] = nint.Zero;
            lArray = Marshal.AllocHGlobal(nint.Size * lPointers.Length);
            Marshal.Copy(lPointers, 0, lArray, lPointers.Length);

            int lResult = LMpvNative.mpv_command(lMpvContext, lArray);
            LMpvResultCheck(lResult, $"mpv_command {string.Join(' ', lArguments)}");
        }
        finally
        {
            if (lArray != nint.Zero)
            {
                Marshal.FreeHGlobal(lArray);
            }

            foreach (nint lPointer in lPointers)
            {
                if (lPointer != nint.Zero)
                {
                    Marshal.FreeHGlobal(lPointer);
                }
            }
        }
    }

    public void LMpvDispose()
    {
        LMpvScanCancel();
        nint lContext;
        lock (lMpvScanGate)
        {
            lContext = Interlocked.Exchange(ref lMpvContext, nint.Zero);
        }

        if (lContext != nint.Zero)
        {
            LMpvTeardownStart(lContext);
        }
    }

    private static void LMpvTeardownStart(nint lContext)
    {
        lock (lMpvTeardownGate)
        {
            Task lPrevious = lMpvTeardownTask ?? Task.CompletedTask;
            lMpvTeardownTask = lPrevious.ContinueWith(
                _ => LMpvNative.mpv_terminate_destroy(lContext),
                CancellationToken.None,
                TaskContinuationOptions.LongRunning,
                TaskScheduler.Default);
        }
    }

    public void Dispose()
    {
        LMpvDispose();
    }

    private void LMpvContextValidate()
    {
        if (lMpvContext == nint.Zero)
        {
            throw new InvalidOperationException("mpv handle is not created.");
        }
    }

    private static void LMpvResultCheck(int lResult, string lAction)
    {
        if (lResult < 0)
        {
            nint lErrorPointer = LMpvNative.mpv_error_string(lResult);
            string lErrorText = Marshal.PtrToStringUTF8(lErrorPointer) ?? "unknown mpv error";
            throw new InvalidOperationException($"{lAction} failed: {lErrorText} ({lResult}).");
        }
    }

    private static nint LMpvStringCreate(string lValue)
    {
        byte[] lBytes = Encoding.UTF8.GetBytes(lValue);
        nint lPointer = Marshal.AllocHGlobal(lBytes.Length + 1);
        Marshal.Copy(lBytes, 0, lPointer, lBytes.Length);
        Marshal.WriteByte(lPointer, lBytes.Length, 0);
        return lPointer;
    }

    private static void LMpvResolverAttach()
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

            NativeLibrary.SetDllImportResolver(typeof(LMpvNative).Assembly, LMpvResolve);
            lMpvResolverActive = true;
        }
    }

    private static nint LMpvResolve(string lLibraryName, Assembly lAssembly, DllImportSearchPath? lSearchPath)
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
