using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

public sealed class LEngine
{
    public LScheduleContract LEngineSchedule { get; }

    public LVideoFacade LEngineVideo { get; }

    public LAudioFacade LEngineAudio { get; }

    public LFilterFacade LEngineFilter { get; }

    public LWhitebalanceFacade LEngineWhitebalance { get; }

    public LLoudnessFacade LEngineLoudness { get; }

    public LEqualizerFacade LEngineEqualizer { get; }

    public LNoiseFacade LEngineNoise { get; }

    public LConvertFacade LEngineConvert { get; }

    public LFixFacade LEngineFix { get; }

    public LFixPlanFacade LEngineFixPlan { get; }

    public LRosterFacade LEngineRoster { get; }

    public LRosterMenuFacade LEngineRosterMenu { get; }

    public LRosterCardFacade LEngineRosterCard { get; }

    public LWorkspaceFacade LEngineWorkspace { get; }

    public LUsherFacade LEngineUsher { get; }

    public LEditFacade LEngineEdit { get; }

    public LEditAudioFacade LEngineEditAudio { get; }

    public LToneFacade LEngineTone { get; }

    public LViewerFacade LEngineViewer { get; }

    public LMergeFacade LEngineMerge { get; }

    public LConsoleFacade LEngineConsole { get; }

    public LMapFacade LEngineMap { get; }

    public LRelayFacade LEngineRelay { get; }

    public LLineageFacade LEngineLineage { get; }

    public LRosterDetailFacade LEngineRosterDetail { get; }

    public LFlowFacade LEngineFlow { get; }

    public LAboutFacade LEngineAbout { get; }

    public LEngine(LRig lRig)
    {
        ArgumentNullException.ThrowIfNull(lRig);

        LEngineSchedule = lRig.LRigSchedule;
        LEngineVideo = new LVideoFacade();
        LEngineAudio = new LAudioFacade();
        LEngineFilter = new LFilterFacade();
        LEngineWhitebalance = new LWhitebalanceFacade();
        LEngineLoudness = new LLoudnessFacade();
        LEngineEqualizer = new LEqualizerFacade();
        LEngineNoise = new LNoiseFacade();
        LEngineConvert = new LConvertFacade();
        LEngineFix = new LFixFacade();
        LEngineFixPlan = new LFixPlanFacade();
        LEngineRoster = new LRosterFacade();
        LEngineRosterMenu = new LRosterMenuFacade();
        LEngineRosterCard = new LRosterCardFacade();
        LEngineWorkspace = new LWorkspaceFacade();
        LEngineUsher = new LUsherFacade();
        LEngineEdit = new LEditFacade();
        LEngineEditAudio = new LEditAudioFacade(lRig.LRigTrace);
        LEngineTone = new LToneFacade(lRig.LRigPreview);
        LEngineViewer = new LViewerFacade(lRig.LRigTrace);
        LEngineMerge = new LMergeFacade(lRig.LRigTrace);
        LEngineConsole = new LConsoleFacade(lRig.LRigTool);
        LEngineMap = new LMapFacade(lRig.LRigTrace);
        LEngineRelay = new LRelayFacade(lRig.LRigMedia, lRig.LRigTrace);
        LEngineLineage = new LLineageFacade();
        LEngineRosterDetail = new LRosterDetailFacade();
        LEngineFlow = new LFlowFacade(lRig.LRigFlow);
        LEngineAbout = new LAboutFacade();
        LEngineSeam.LEngineSeamAttach(lRig);
    }
}
