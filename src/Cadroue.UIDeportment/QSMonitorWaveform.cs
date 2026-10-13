using Cadroue.Application;
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
        return lKey is null ? string.Empty : LLocalization.LLocalizationTextRead(lKey);
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
        lock (qMonitorLock)
        {
            qMonitorSourcePath = lPath;
            qMonitorDuration = lDuration;
            qMonitorRate = lRate;
        }

        qMonitorOrchestrator.LWaveformStart(lPath, lDuration, lRate > 0);
    }

    public void QSMonitorPlanApply(LWorkAudio lPlan)
    {
        lock (qMonitorLock)
        {
            qMonitorPlan = lPlan;
        }

        QSMonitorAfterStart();
    }

    public void QSMonitorUpdate()
    {
        QSMonitorPublish();
    }

    private void QSMonitorPeaksHandle(LWaveformNotice lNotice)
    {
        lock (qMonitorLock)
        {
            qMonitorBefore = LWaveform.LWaveformEnvelopeRead(lNotice.LWaveformPeaks);
            qMonitorAfter = qMonitorBefore;
            qMonitorPending = lNotice.LWaveformPending;
            qMonitorFailed = !lNotice.LWaveformPending && lNotice.LWaveformPeaks.Length == 0;
        }

        QSMonitorPublish();
        if (lNotice.LWaveformPeaks.Length > 0)
        {
            QSMonitorAfterStart();
        }
    }

    private void QSMonitorAfterStart()
    {
        CancellationToken lToken;
        string? lPath;
        TimeSpan lDuration;
        string lGraph;
        lock (qMonitorLock)
        {
            if (qMonitorDisposed)
            {
                return;
            }

            qMonitorCancelSource?.Cancel();
            qMonitorCancelSource?.Dispose();
            qMonitorCancelSource = new CancellationTokenSource();
            lToken = qMonitorCancelSource.Token;

            if (qMonitorBefore.Length == 0)
            {
                return;
            }

            lPath = qMonitorSourcePath;
            lDuration = qMonitorDuration;
            lGraph = qMonitorPlan.LWorkAudioFormat(qMonitorRate);
            qMonitorFailed = false;
            if (string.IsNullOrEmpty(lGraph)
                || string.IsNullOrWhiteSpace(lPath)
                || lDuration <= TimeSpan.Zero)
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

        QSMonitorPublish();
        if (lPath is not null)
        {
            QSMonitorAfterScan(lPath, lDuration, lGraph, lToken);
        }
    }

    private void QSMonitorAfterScan(string lPath, TimeSpan lDuration, string lGraph, CancellationToken lToken)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(QSMonitorDebounceMs, lToken).ConfigureAwait(false);
                LWaveformScanResult lScanned = await LWaveformScanner.LWaveformScan(lPath, lDuration, lToken, lGraph)
                    .ConfigureAwait(false);
                if (!lScanned.LWaveformComplete)
                {
                    LTrace.LTraceRecord(
                        LTraceKind.LTraceWarning,
                        $"Monitor after-scan unavailable for {System.IO.Path.GetFileName(lPath)}",
                        lScanned.LWaveformDetail);
                }

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

    private void QSMonitorPublish() => QSMonitorReady?.Invoke();

    public void Dispose()
    {
        lock (qMonitorLock)
        {
            if (qMonitorDisposed)
            {
                return;
            }

            qMonitorDisposed = true;
            qMonitorCancelSource?.Cancel();
            qMonitorCancelSource?.Dispose();
            qMonitorCancelSource = null;
        }

        qMonitorOrchestrator.LWaveformReady -= QSMonitorPeaksHandle;
        qMonitorOrchestrator.Dispose();
    }
}
