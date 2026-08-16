using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Cadroue.Core;

namespace Cadroue.Infrastructure;

public sealed partial class LMpv : IDisposable
{
    private const string LMpvLibraryFile = "libmpv-2.dll";
    private const string LMpvProbeSource = "av://lavfi:testsrc=d=1:s=64x64";
    private static readonly TimeSpan LMpvProbeBudget = TimeSpan.FromSeconds(4);

    private const int LMpvEventNone = 0;
    private const int LMpvEventShutdown = 1;
    private const int LMpvEventStarted = 6;
    private const int LMpvEventEnd = 7;
    private const int LMpvEventLoaded = 8;

    private const int LMpvFormatDouble = 5;

    private static bool lMpvResolverActive;
    private static readonly object lMpvResolverGate = new();

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

    public void LMpvContextCreate(nint lWindowHandle)
    {
        if (lMpvContext != nint.Zero)
        {
            throw new InvalidOperationException("mpv handle already created.");
        }

        nint lHandle = LMpvNative.mpv_create();
        if (lHandle == nint.Zero)
        {
            throw new InvalidOperationException($"mpv_create failed (libmpv path: {LMpvLibraryPath}).");
        }

        lMpvContext = lHandle;
        LMpvOptionSet("sub-auto", "no");
        LMpvOptionSet("input-default-bindings", "no");
        LMpvOptionSet("input-vo-keyboard", "no");
        LMpvOptionSet("osc", "no");
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

    public void LMpvOpen(string lPath)
    {
        LMpvCommandRun("loadfile", lPath);
    }

    public LMpvProbe LMpvMediaCheck(string lPath, TimeSpan lBudget, CancellationToken lToken)
    {
        LMpvEventClear();
        LMpvOpen(lPath);
        return LMpvLoadedScan(lBudget, lToken);
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
        double lLinear = Math.Clamp(lVolume, 0, 100) / 100.0;
        double lCurved = 100.0 * Math.Cbrt(lLinear);
        LMpvPropertySet("volume", lCurved.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    public void LMpvFilterSet(string lFilterChain)
    {
        LMpvPropertySet("vf", lFilterChain ?? string.Empty);
    }

    public void LMpvAudioSet(string lFilterChain)
    {
        LMpvPropertySet("af", string.IsNullOrEmpty(lFilterChain)
            ? string.Empty
            : "lavfi=[" + lFilterChain + "]");
    }

    public TimeSpan LMpvTimeRead()
    {
        LMpvContextValidate();
        int lResult = LMpvNative.mpv_get_property(lMpvContext, "time-pos", LMpvFormatDouble, out double lSeconds);
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
            lMpv.LMpvContextCreate(nint.Zero);
            lMpv.LMpvOpen(LMpvProbeSource);
            return lMpv.LMpvLoadedScan(LMpvProbeBudget, CancellationToken.None);
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

    public void LMpvEventClear()
    {
        LMpvContextValidate();
        while (true)
        {
            nint lEvent = LMpvNative.mpv_wait_event(lMpvContext, 0);
            if (lEvent == nint.Zero)
            {
                return;
            }

            if (Marshal.ReadInt32(lEvent) == LMpvEventNone)
            {
                return;
            }
        }
    }

    public LMpvProbe LMpvLoadedScan(TimeSpan lBudget, CancellationToken lToken)
    {
        LMpvContextValidate();
        DateTime lDeadline = DateTime.UtcNow + lBudget;
        bool lStarted = false;
        while (true)
        {
            if (lToken.IsCancellationRequested)
            {
                return LMpvProbe.LMpvProbeUnusable;
            }

            double lRemaining = (lDeadline - DateTime.UtcNow).TotalSeconds;
            if (lRemaining <= 0)
            {
                return LMpvProbe.LMpvProbeUnusable;
            }

            nint lEvent = LMpvNative.mpv_wait_event(lMpvContext, Math.Min(0.1, lRemaining));
            if (lEvent == nint.Zero)
            {
                continue;
            }

            int lEventId = Marshal.ReadInt32(lEvent);
            if (lEventId == LMpvEventStarted)
            {
                lStarted = true;
                continue;
            }

            if (lEventId == LMpvEventLoaded)
            {
                return LMpvProbe.LMpvProbeUsable;
            }

            if (lEventId == LMpvEventShutdown)
            {
                return LMpvProbe.LMpvProbeUnusable;
            }

            if (lEventId == LMpvEventEnd && lStarted)
            {
                return LMpvProbe.LMpvProbeUnusable;
            }
        }
    }

    public void LMpvDispose()
    {
        nint lContext = Interlocked.Exchange(ref lMpvContext, nint.Zero);
        if (lContext == nint.Zero)
        {
            return;
        }

        var lTeardown = new Thread(() =>
        {
            try
            {
                LMpvNative.mpv_terminate_destroy(lContext);
            }
            catch
            {
            }
        })
        {
            IsBackground = true,
            Name = "LMpvTeardown"
        };
        lTeardown.Start();
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
