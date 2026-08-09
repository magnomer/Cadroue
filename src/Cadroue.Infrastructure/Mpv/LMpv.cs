using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Cadroue.Core;

namespace Cadroue.Infrastructure;

public sealed partial class LMpv : IDisposable
{
    private const string LMpvLibraryFile = "libmpv-2.dll";
    private const string LMpvProbeSource = "av://lavfi:testsrc=d=1:s=64x64";
    private static readonly TimeSpan LMpvProbeBudget = TimeSpan.FromSeconds(4);

    private const int LMpvEventShutdown = 1;
    private const int LMpvEventEndFile = 7;
    private const int LMpvEventFileLoaded = 8;

    private const int LMpvFormatDouble = 5;

    private static bool lMpvResolverActive;
    private static readonly object lMpvResolverGate = new();

    private nint lMpvHandle;

    public LMpv()
    {
        LMpvResolverAttach();
    }

    public string LMpvLibraryPath { get; } = LMpvLibraryRead();

    public bool LMpvHandleActive => lMpvHandle != nint.Zero;

    public static string LMpvLibraryRead()
    {
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

    public void LMpvHandleCreate(nint lWindowHandle)
    {
        if (lMpvHandle != nint.Zero)
        {
            throw new InvalidOperationException("mpv handle already created.");
        }

        nint lHandle = LMpvNative.mpv_create();
        if (lHandle == nint.Zero)
        {
            throw new InvalidOperationException($"mpv_create failed (libmpv path: {LMpvLibraryPath}).");
        }

        lMpvHandle = lHandle;
        if (lWindowHandle != nint.Zero)
        {
            LMpvOptionSet("wid", lWindowHandle.ToString());
        }
        else
        {
            LMpvOptionSet("vo", "null");
            LMpvOptionSet("ao", "null");
        }

        int lResult = LMpvNative.mpv_initialize(lMpvHandle);
        LMpvResultCheck(lResult, "mpv_initialize");
    }

    public void LMpvOptionSet(string lName, string lData)
    {
        LMpvHandleGuard();
        int lResult = LMpvNative.mpv_set_option_string(lMpvHandle, lName, lData);
        LMpvResultCheck(lResult, $"mpv_set_option_string {lName}={lData}");
    }

    public void LMpvPropertySet(string lName, string lData)
    {
        LMpvHandleGuard();
        int lResult = LMpvNative.mpv_set_property_string(lMpvHandle, lName, lData);
        LMpvResultCheck(lResult, $"mpv_set_property_string {lName}={lData}");
    }

    public void LMpvCommandRun(params string[] lArguments)
    {
        LMpvHandleGuard();
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
                lPointers[lIndex] = LMpvUtf8Create(lArguments[lIndex]);
            }

            lPointers[lArguments.Length] = nint.Zero;
            lArray = Marshal.AllocHGlobal(nint.Size * lPointers.Length);
            Marshal.Copy(lPointers, 0, lArray, lPointers.Length);

            int lResult = LMpvNative.mpv_command(lMpvHandle, lArray);
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

    public void LMpvOpen(string lPath)
    {
        LMpvCommandRun("loadfile", lPath);
    }

    public LMpvProbe LMpvOpenWait(string lPath, TimeSpan lBudget)
    {
        LMpvOpen(lPath);
        return LMpvFileLoadedWait(lBudget);
    }

    public void LMpvSeek(TimeSpan lPosition)
    {
        string lSeconds = lPosition.TotalSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture);
        LMpvCommandRun("seek", lSeconds, "absolute+exact");
    }

    public void LMpvStop()
    {
        LMpvCommandRun("stop");
    }

    public void LMpvDecodeInterrupt()
    {
        LMpvCommandRun("stop");
    }

    public void LMpvPlaySet(bool lPlaying)
    {
        LMpvPropertySet("pause", lPlaying ? "no" : "yes");
    }

    public void LMpvVolumeSet(double lVolume)
    {
        LMpvPropertySet("volume", lVolume.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    public TimeSpan LMpvTimeRead()
    {
        LMpvHandleGuard();
        int lResult = LMpvNative.mpv_get_property(lMpvHandle, "time-pos", LMpvFormatDouble, out double lSeconds);
        if (lResult < 0 || double.IsNaN(lSeconds) || lSeconds < 0)
        {
            return TimeSpan.Zero;
        }

        return TimeSpan.FromSeconds(lSeconds);
    }

    public static LMpvProbe LMpvCheck()
    {
        try
        {
            using LMpv lMpv = new();
            lMpv.LMpvHandleCreate(nint.Zero);
            lMpv.LMpvOpen(LMpvProbeSource);
            return lMpv.LMpvFileLoadedWait(LMpvProbeBudget);
        }
        catch (DllNotFoundException)
        {
            return LMpvProbe.LMpvProbeUnusable;
        }
        catch (InvalidOperationException)
        {
            return LMpvProbe.LMpvProbeUnusable;
        }
        catch
        {
            return LMpvProbe.LMpvProbeUnknown;
        }
    }

    public LMpvProbe LMpvFileLoadedWait(TimeSpan lBudget)
    {
        LMpvHandleGuard();
        DateTime lDeadline = DateTime.UtcNow + lBudget;
        while (true)
        {
            double lRemaining = (lDeadline - DateTime.UtcNow).TotalSeconds;
            if (lRemaining <= 0)
            {
                return LMpvProbe.LMpvProbeUnusable;
            }

            nint lEvent = LMpvNative.mpv_wait_event(lMpvHandle, Math.Min(0.1, lRemaining));
            if (lEvent == nint.Zero)
            {
                continue;
            }

            int lEventId = Marshal.ReadInt32(lEvent);
            if (lEventId == LMpvEventFileLoaded)
            {
                return LMpvProbe.LMpvProbeUsable;
            }

            if (lEventId == LMpvEventEndFile || lEventId == LMpvEventShutdown)
            {
                return LMpvProbe.LMpvProbeUnusable;
            }
        }
    }

    public void LMpvDispose()
    {
        if (lMpvHandle == nint.Zero)
        {
            return;
        }

        LMpvNative.mpv_terminate_destroy(lMpvHandle);
        lMpvHandle = nint.Zero;
    }

    public void Dispose()
    {
        LMpvDispose();
    }

    private void LMpvHandleGuard()
    {
        if (lMpvHandle == nint.Zero)
        {
            throw new InvalidOperationException("mpv handle is not created.");
        }
    }

    private static void LMpvResultCheck(int lResult, string lAction)
    {
        if (lResult < 0)
        {
            throw new InvalidOperationException($"{lAction} failed: {LMpvNative.mpv_error_string(lResult)} ({lResult}).");
        }
    }

    private static nint LMpvUtf8Create(string lValue)
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
