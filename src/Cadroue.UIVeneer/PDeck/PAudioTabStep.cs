using Cadroue.Core;
using Cadroue.UIVeneer.PPanel;
using PFlowControl = Cadroue.UIVeneer.PFlow.PFlow;
using Cadroue.Application;
using Cadroue.Infrastructure;
using Cadroue.ShellEngine;
using Cadroue.Media;

namespace Cadroue.UIVeneer.PDeck;

public sealed partial class PAudioTab
{
    private void PAudioActiveUpdate()
    {
        foreach (string pStepName in pProcessing.PProcessingStepsRead())
        {
            if (PAudioKindRead(pStepName) is LAudioKind pStepKind)
            {
                pProcessing.PProcessingActiveSet(pStepName, pInspector.PInspectorStepRead(pStepKind).LWorkStepActive);
            }
        }
    }

    private void PAudioChangeHandle()
    {
        PAudioActiveUpdate();
        PAudioPlanSave();
        pAudioMonitor.LSMonitorPlanApply(PAudioProcessingRead());
        PAudioViewerDefer();
    }

    private void PAudioViewerDefer()
    {
        if (pAudioViewerTimer is null)
        {
            pAudioViewerTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(150)
            };
            pAudioViewerTimer.Tick += PAudioViewerHandle;
        }

        pAudioViewerTimer.Stop();
        pAudioViewerTimer.Start();
    }

    private void PAudioViewerHandle(object? pSender, EventArgs pArgs)
    {
        pAudioViewerTimer?.Stop();
        PAudioViewerApply();
    }

    private void PAudioViewerApply()
    {
        LWorkAudio pAudioPlan = PAudioProcessingRead();
        pViewer.PViewerAudioSet(
            pAudioPlan.LWorkAudioSkip ? string.Empty : pAudioPlan.LWorkAudioFormat(pAudioOwnerRate));
    }

    private void PAudioMonitorShow() =>
        PSMonitor.PSMonitorShow(System.Windows.Window.GetWindow(this), pAudioMonitor, pFlow, pViewer);

    private LWorkAudio PAudioProcessingRead()
    {
        var pSteps = new List<LWorkAudioStep>();
        foreach (string pStepName in pProcessing.PProcessingStepsRead())
        {
            if (PAudioKindRead(pStepName) is LAudioKind pStepKind)
            {
                pSteps.Add(pInspector.PInspectorStepRead(pStepKind));
            }
        }

        return new LWorkAudio(pSteps) { LWorkAudioSkip = pInspector.PSkipActiveCheck() };
    }

    private static LAudioKind? PAudioKindRead(string pStepName) => pStepName switch
    {
        "Volume" => LAudioKind.LAudioKindVolume,
        "Normalize" => LAudioKind.LAudioKindLeveling,
        "Noise Reduction" => LAudioKind.LAudioKindDenoise,
        "High Pass" => LAudioKind.LAudioKindHighpass,
        "Low Pass" => LAudioKind.LAudioKindLowpass,
        "Equalizer" => LAudioKind.LAudioKindEqualizer,
        _ => null
    };
}
