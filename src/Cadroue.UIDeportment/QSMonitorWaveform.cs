using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.Media;

namespace Cadroue.UIDeportment;

public sealed class QSMonitorWaveform : IDisposable
{
    private const int QSMonitorDebounceMs = 150;

    private readonly LWaveformOrchestrator qMonitorOrchestrator = new();
    private readonly object qMonitorLock = new();
    private string? qMonitorSourcePath;
    private TimeSpan qMonitorDuration;
    private int qMonitorRate;
    private LWorkAudio qMonitorPlan = LWorkAudio.LWorkAudioCreate();
    private double[] qMonitorBefore = Array.Empty<double>();
    private double[] qMonitorAfter = Array.Empty<double>();
    private bool qMonitorPending;
    private bool qMonitorFailed;
    private CancellationTokenSource? qMonitorCancelSource;
    private bool qMonitorDisposed;

    public event Action? QSMonitorReady;

    public QSMonitorWaveform()
    {
        qMonitorOrchestrator.LWaveformReady += QSMonitorPeaksHandle;
    }

    public string QSMonitorStatusRead(bool lAfter)
    {
        double[] lEnvelope;
        bool lBeforeReady;
        bool lPending;
        bool lFailed;
        lock (qMonitorLock)
        {
            lEnvelope = lAfter ? qMonitorAfter : qMonitorBefore;
            lBeforeReady = qMonitorBefore.Length > 0;
            lPending = qMonitorPending;
            lFailed = qMonitorFailed;
        }

        string? lKey = lPending
            ? (lBeforeReady && lAfter ? "NormalizePreview.Updating" : "NormalizePreview.Loading")
            : lEnvelope.Length > 0
                ? null
                : lFailed ? "NormalizePreview.Unavailable" : "NormalizePreview.Empty";
        return lKey is null ? string.Empty : QLocalizationCatalog.QLocalizationTextRead(lKey);
    }

    public double[] QSMonitorEnvelopeRead(bool lAfter)
    {
        lock (qMonitorLock)
        {
            return lAfter ? qMonitorAfter : qMonitorBefore;
        }
    }

    public void QSMonitorSourceOpen(string? lPath, TimeSpan lDuration, int lRate = 0)
    {
        QSMonitorSourceSet(lPath, lDuration, lRate);
        qMonitorOrchestrator.LWaveformStart(lPath, lDuration, lRate > 0);
    }

    private void QSMonitorSourceSet(string? lPath, TimeSpan lDuration, int lRate)
    {
        lock (qMonitorLock)
        {
            qMonitorSourcePath = lPath;
            qMonitorDuration = lDuration;
            qMonitorRate = lRate;
        }
    }

    public void QSMonitorPlanApply(LWorkAudio lPlan)
    {
        QSMonitorPlanSet(lPlan);
        QSMonitorAfterStart();
    }

    private void QSMonitorPlanSet(LWorkAudio lPlan)
    {
        lock (qMonitorLock)
        {
            qMonitorPlan = lPlan;
        }
    }

    public void QSMonitorUpdate()
    {
        QSMonitorPublish();
    }

    private void QSMonitorPeaksHandle(LWaveformNotice lNotice)
    {
        QSMonitorPeaksSet(lNotice);
        QSMonitorPublish();
        if (lNotice.LWaveformPeaks.Length > 0)
        {
            QSMonitorAfterStart();
        }
    }

    private void QSMonitorPeaksSet(LWaveformNotice lNotice)
    {
        lock (qMonitorLock)
        {
            qMonitorBefore = LWaveform.LWaveformEnvelopeRead(lNotice.LWaveformPeaks);
            qMonitorAfter = qMonitorBefore;
            qMonitorPending = lNotice.LWaveformPending;
            qMonitorFailed = QSMonitorFailedCheck(lNotice);
        }
    }

    private static bool QSMonitorFailedCheck(LWaveformNotice lNotice) =>
        !lNotice.LWaveformPending && lNotice.LWaveformPeaks.Length == 0;

    private void QSMonitorAfterStart()
    {
        if (!QSMonitorAfterPrepare(
            out string? lPath, out TimeSpan lDuration, out string lGraph, out CancellationToken lToken))
        {
            return;
        }

        QSMonitorPublish();
        if (lPath is not null)
        {
            QSMonitorAfterScan(lPath, lDuration, lGraph, lToken);
        }
    }

    private bool QSMonitorAfterPrepare(
        out string? lPath, out TimeSpan lDuration, out string lGraph, out CancellationToken lToken)
    {
        lPath = null;
        lDuration = TimeSpan.Zero;
        lGraph = string.Empty;
        lToken = CancellationToken.None;
        lock (qMonitorLock)
        {
            if (qMonitorDisposed)
            {
                return false;
            }

            qMonitorCancelSource?.Cancel();
            qMonitorCancelSource?.Dispose();
            qMonitorCancelSource = new CancellationTokenSource();
            lToken = qMonitorCancelSource.Token;

            if (qMonitorBefore.Length == 0)
            {
                return false;
            }

            lPath = qMonitorSourcePath;
            lDuration = qMonitorDuration;
            lGraph = qMonitorPlan.LWorkAudioFormat(qMonitorRate);
            qMonitorFailed = false;
            if (!QSMonitorScanCheck(lGraph, lPath, lDuration))
            {
                qMonitorAfter = qMonitorBefore;
                qMonitorPending = false;
                lPath = null;
            }
            else
            {
                qMonitorPending = true;
            }
        }

        return true;
    }

    private static bool QSMonitorScanCheck(string lGraph, string? lPath, TimeSpan lDuration) =>
        !(string.IsNullOrEmpty(lGraph)
            || string.IsNullOrWhiteSpace(lPath)
            || lDuration <= TimeSpan.Zero);

    private void QSMonitorAfterScan(string lPath, TimeSpan lDuration, string lGraph, CancellationToken lToken)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(QSMonitorDebounceMs, lToken).ConfigureAwait(false);
                LWaveformScanResult lScanned = await LWaveformScanner.LWaveformScan(lPath, lDuration, lToken, lGraph)
                    .ConfigureAwait(false);
                QSMonitorScanRecord(lPath, lScanned);
                QSMonitorAfterApply(
                    lToken,
                    LWaveform.LWaveformEnvelopeRead(lScanned.LWaveformPeaks),
                    !lScanned.LWaveformComplete);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception lException)
            {
                LTraceLog.LTraceErrorRecord("Monitor after-scan could not be generated", lException);
                QSMonitorAfterApply(lToken, Array.Empty<double>(), true);
            }
        }, CancellationToken.None);
    }

    private void QSMonitorAfterApply(CancellationToken lToken, double[] lAfter, bool lFailed)
    {
        lock (qMonitorLock)
        {
            if (lToken.IsCancellationRequested)
            {
                return;
            }

            qMonitorAfter = lAfter;
            qMonitorFailed = lFailed;
            qMonitorPending = false;
        }

        QSMonitorPublish();
    }

    private static void QSMonitorScanRecord(string lPath, LWaveformScanResult lScanned)
    {
        if (!lScanned.LWaveformComplete)
        {
            LTrace.LTraceRecord(
                LTraceKind.LTraceWarning,
                $"Monitor after-scan unavailable for {System.IO.Path.GetFileName(lPath)}",
                lScanned.LWaveformDetail);
        }
    }

    private void QSMonitorPublish() => QSMonitorReady?.Invoke();

    public void Dispose()
    {
        if (!QSMonitorScanRelease())
        {
            return;
        }

        QSMonitorPeaksDetach();
        qMonitorOrchestrator.Dispose();
    }

    private bool QSMonitorScanRelease()
    {
        lock (qMonitorLock)
        {
            if (qMonitorDisposed)
            {
                return false;
            }

            qMonitorDisposed = true;
            qMonitorCancelSource?.Cancel();
            qMonitorCancelSource?.Dispose();
            qMonitorCancelSource = null;
        }

        return true;
    }

    private void QSMonitorPeaksDetach() => qMonitorOrchestrator.LWaveformReady -= QSMonitorPeaksHandle;
}
