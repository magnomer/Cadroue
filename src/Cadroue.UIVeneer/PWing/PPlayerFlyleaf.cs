using System;
using System.Collections.Generic;
using FlyleafLib;
using FlyleafLib.Controls.WPF;
using FlyleafLib.MediaPlayer;

using Cadroue.Infrastructure;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PWing;

internal sealed class PPlayerFlyleaf
{
    private readonly Player pPlayer;
    private readonly FlyleafHost pPlayerHost;
    private readonly QPlayer lPlayer;
    private readonly PPlayerPreview pPlayerPreview;
    private readonly PPlayerTrace pPlayerTrace;

    public PPlayerFlyleaf(FlyleafHost pHost, QPlayer lPlayerOwner, Config pConfig)
    {
        lPlayerOwner.QPlayerCreateStart();
        pPlayerHost = pHost;
        lPlayer = lPlayerOwner;
        pPlayer = new Player(pConfig);
        pPlayerPreview = new PPlayerPreview(pPlayer);
        pPlayerTrace = new PPlayerTrace(pPlayer);
        pPlayerPreview.PPlayerFilterPrepare();
        PPlayerSeekAttach();
        PPlayerHostSet();
        pPlayerTrace.PPlayerSetupRecord(lPlayer);
    }

    private void PPlayerSeekAttach() => pPlayer.SeekCompleted += PPlayerSeekHandle;

    private void PPlayerHostSet() => pPlayerHost.Player = pPlayer;

    public static nint PPlayerHandleRead(System.Windows.Window? pWindow)
    {
        try
        {
            return new System.Windows.Interop.WindowInteropHelper(pWindow!).Handle;
        }
        catch (ArgumentNullException)
        {
            return nint.Zero;
        }
    }

    public QPlayerSeam PPlayerSeamRead() => new(
        PPlayerOpen,
        pPlayer.Play,
        pPlayer.Pause,
        pPlayer.Stop,
        PPlayerSeek,
        PPlayerVolumeSet,
        null,
        null,
        pPlayerPreview.PPlayerPreviewApply,
        null,
        PPlayerTimeRead,
        PPlayerEndedRead,
        pPlayerTrace.PPlayerFactsRecord,
        PPlayerDispose);

    private void PPlayerOpen(string pSourcePath)
    {
        var pResult = pPlayer.Open(pSourcePath);
        pPlayerOpens[pResult.Success](pResult);
    }

    private static readonly IReadOnlyDictionary<bool, Action<OpenCompletedArgs>> pPlayerOpens =
        new Dictionary<bool, Action<OpenCompletedArgs>>
        {
            [true] = _ => { },
            [false] = pResult =>
                throw new InvalidOperationException(LRendererError.LRendererErrorResolve(pResult.Error)),
        };

    private static readonly HashSet<Status> pPlayerEnded = [Status.Ended];

    private void PPlayerSeek(TimeSpan pPosition) => pPlayer.SeekAccurate((int)pPosition.TotalMilliseconds);

    private void PPlayerVolumeSet(double pVolume) => pPlayer.Audio.Volume = (int)Math.Round(pVolume);

    private TimeSpan PPlayerTimeRead() => TimeSpan.FromTicks(pPlayer.CurTime);

    private bool PPlayerEndedRead() => pPlayerEnded.Contains(pPlayer.Status);

    private void PPlayerSeekHandle(object? pSender, int pSeekMilliseconds) =>
        lPlayer.QPlayerSeekHandle(pSeekMilliseconds);

    private void PPlayerDispose()
    {
        PPlayerSeekDetach();
        PPlayerEngineDispose();
    }

    private void PPlayerSeekDetach() => pPlayer.SeekCompleted -= PPlayerSeekHandle;

    private void PPlayerEngineDispose()
    {
        pPlayerHost.Player = null;
        try
        {
            pPlayer.Stop();
            pPlayer.Dispose();
        }
        catch
        {
        }
    }
}
