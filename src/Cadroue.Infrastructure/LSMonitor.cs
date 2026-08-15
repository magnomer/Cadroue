using Cadroue.Core;
using Cadroue.Media;

namespace Cadroue.Infrastructure;

public readonly record struct LSMonitorEstimate(double[] LSMonitorBefore, double[] LSMonitorAfter);

public sealed class LSMonitor : IDisposable
{
    private readonly LWaveformOrchestrator lMonitorOrchestrator = new();
    private byte[] lMonitorPeaks = Array.Empty<byte>();
    private byte[] lMonitorRms = Array.Empty<byte>();
    private LWorkAudio lMonitorPlan = LWorkAudio.LWorkAudioCreate();
    private bool lMonitorScanning;

    public event Action<LSMonitorEstimate>? LSMonitorReady;

    public LSMonitor()
    {
        lMonitorOrchestrator.LWaveformReady += LSMonitorPeaksHandle;
    }

    public byte[] LSMonitorPeaks => lMonitorPeaks;

    public bool LSMonitorScanning => lMonitorScanning;

    public void LSMonitorSourceOpen(string? lPath, TimeSpan lDuration)
    {
        lMonitorScanning = !string.IsNullOrWhiteSpace(lPath) && lDuration > TimeSpan.Zero;
        lMonitorOrchestrator.LWaveformStart(lPath, lDuration);
    }

    public void LSMonitorPlanApply(LWorkAudio lPlan)
    {
        lMonitorPlan = lPlan;
        LSMonitorUpdate();
    }

    private void LSMonitorPeaksHandle(byte[] lPeaks)
    {
        lMonitorPeaks = lPeaks;
        lMonitorRms = lMonitorOrchestrator.LWaveformRmsCurrent;
        if (lPeaks.Length > 0)
        {
            lMonitorScanning = false;
        }

        LSMonitorUpdate();
    }

    public void LSMonitorUpdate()
    {
        double[] lBeforePeak = LWaveformEstimate.LWaveformEnvelopeRead(lMonitorPeaks);
        if (lBeforePeak.Length == 0)
        {
            LSMonitorReady?.Invoke(new LSMonitorEstimate(Array.Empty<double>(), Array.Empty<double>()));
            return;
        }

        double[] lBeforeRms = LWaveformEstimate.LWaveformEnvelopeRead(lMonitorRms);
        LSMonitorChainApply(ref lBeforePeak, ref lBeforeRms);
        double[] lAfter = LSMonitorNormalizeApply(lBeforePeak, lBeforeRms);
        LSMonitorReady?.Invoke(new LSMonitorEstimate(lBeforePeak, lAfter));
    }

    private void LSMonitorChainApply(ref double[] lPeak, ref double[] lRms)
    {
        foreach (LWorkAudioStep lStep in lMonitorPlan.LWorkAudioSteps)
        {
            if (lStep.LWorkStepKind == LAudioKind.LAudioKindLeveling)
            {
                break;
            }

            if (lStep is LWorkVolumeStep { LWorkStepActive: true } lVolume)
            {
                double lFactor = Math.Pow(10.0, lVolume.LWorkVolumeGain / 20.0);
                lPeak = LWaveformEstimate.LWaveformGainApply(lPeak, lFactor);
                if (lRms.Length > 0)
                {
                    lRms = LWaveformEstimate.LWaveformGainApply(lRms, lFactor);
                }
            }
        }
    }

    private double[] LSMonitorNormalizeApply(double[] lPeak, double[] lRms)
    {
        LWorkNormalizeStep? lStep = null;
        foreach (LWorkAudioStep lCandidate in lMonitorPlan.LWorkAudioSteps)
        {
            if (lCandidate is LWorkNormalizeStep { LWorkStepActive: true } lNormalize)
            {
                lStep = lNormalize;
                break;
            }
        }

        if (lStep is null)
        {
            return lPeak;
        }

        return lStep.LWorkNormalizeMode == LLeveling.LLevelingDynamic
            ? LWaveformEstimate.LWaveformDynamicApply(
                lPeak, lStep.LWorkNormalizeFrame, lStep.LWorkNormalizeGauss,
                lStep.LWorkNormalizeGain, lStep.LWorkNormalizeCompress)
            : LWaveformEstimate.LWaveformLoudnessApply(
                lPeak, lRms, lStep.LWorkNormalizeTarget, lStep.LWorkNormalizePeak,
                lStep.LWorkNormalizeRange, lStep.LWorkTwoPass);
    }

    public void Dispose()
    {
        lMonitorOrchestrator.LWaveformReady -= LSMonitorPeaksHandle;
        lMonitorOrchestrator.Dispose();
    }
}
