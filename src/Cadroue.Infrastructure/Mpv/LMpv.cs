using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cadroue.Core;

namespace Cadroue.Infrastructure;

public sealed class LMpv : IDisposable
{
    private const string LMpvDecodeThreads = "4";
    private const int LMpvFormatDouble = 5;
    private const int LMpvFormatFlag = 3;

    private const int LMpvEventNone = 0;
    private const int LMpvEventShutdown = 1;
    private const int LMpvEventStarted = 6;
    private const int LMpvEventEnd = 7;
    private const int LMpvEventLoaded = 8;

    private static readonly TimeSpan LMpvTeardownBudget = TimeSpan.FromSeconds(5);

    private static readonly object lMpvTeardownGate = new();
    private static Task? lMpvTeardownTask;

    private readonly object lMpvScanGate = new();
    private CancellationTokenSource? lMpvScanCancellation;

    private nint lMpvContext;

    public LMpv()
    {
        LMpvLibrary.LMpvLibraryAttach();
    }

    public string LMpvLibraryPath { get; } = LMpvLibrary.LMpvLibraryRead();

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
            LMpvOptionSet("vd-lavc-threads", LMpvDecodeThreads);
            if (LMpvAdapter.LMpvAdapterRead(lWindowHandle) is { Length: > 0 } lAdapter)
            {
                LMpvOptionSet("d3d11-adapter", lAdapter);
            }
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

    public void LMpvSeek(TimeSpan lPosition)
    {
        string lSeconds = lPosition.TotalSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture);
        LMpvCommandRun("seek", lSeconds, "absolute+exact");
    }

    public void LMpvStop()
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

    public bool LMpvEndedRead()
    {
        LMpvContextValidate();
        int lResult = LMpvNative.mpv_get_property_flag(lMpvContext, "eof-reached", LMpvFormatFlag, out int lFlag);
        return lResult >= 0 && lFlag != 0;
    }

    public LMpvProbe LMpvMediaCheck(string lPath, TimeSpan lBudget, CancellationToken lToken)
    {
        CancellationTokenSource lScan = CancellationTokenSource.CreateLinkedTokenSource(lToken);
        CancellationTokenSource? lPrevious = Interlocked.Exchange(ref lMpvScanCancellation, lScan);
        lPrevious?.Cancel();
        lock (lMpvScanGate)
        {
            try
            {
                if (lScan.IsCancellationRequested)
                {
                    return LMpvProbe.LMpvProbeUnusable;
                }

                LMpvEventClear();
                LMpvOpen(lPath);
                return LMpvLoadedScan(lBudget, lScan.Token);
            }
            finally
            {
                lPrevious?.Dispose();
                if (Interlocked.CompareExchange(ref lMpvScanCancellation, null, lScan) == lScan)
                {
                    lScan.Dispose();
                }
            }
        }
    }

    public void LMpvScanCancel()
    {
        try
        {
            Volatile.Read(ref lMpvScanCancellation)?.Cancel();
        }
        catch (ObjectDisposedException)
        {
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

    public void Dispose()
    {
        LMpvDispose();
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
}
