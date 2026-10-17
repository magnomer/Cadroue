using System;
using Cadroue.Conduct;
using Cadroue.ShellEngine;

namespace Cadroue.Tests;

// Builds an atelier on test ports; a test swaps the ports it watches with `port => port with { ... }`.
internal static class TAtelier
{
    internal static CAtelier TAtelierCreate(Func<LAtelierPort, LAtelierPort>? port = null)
    {
        LAtelierPort ports = new()
        {
            LAtelierUsher = new TUsherPort(),
            LAtelierLineage = new TLineagePort(),
            LAtelierWorkspace = new TWorkspacePort(),
            LAtelierPlacement = new TPlacementPort(),
            LAtelierViewer = new TViewerPort(),
            LAtelierEdit = new TEditPort(),
            LAtelierRoster = new TRosterPort(),
            LAtelierFix = new TFixPort(),
            LAtelierConvert = new TConvertPort(),
            LAtelierTone = new TTonePort(),
            LAtelierEqualizer = Interface.TInterface.TEqualizerOutletCreate(),
            LAtelierClinic = Interface.TInterface.TClinicOutletCreate(),
            LAtelierRosterCard = new TRosterCardPort(),
            LAtelierMerge = new TMergePort(),
            LAtelierNoise = new TNoisePort(),
            LAtelierConsole = new TConsolePort(),
            LAtelierFlow = new TFlowPort(),
            LAtelierMap = new TMapPort(),
            LAtelierRosterDetail = new TRosterDetailPort(),
            LAtelierWhitebalance = new TWhitebalancePort(),
            LAtelierFilter = new TFilterPort(),
            LAtelierAudio = new TAudioPort(),
            LAtelierLoudness = new TLoudnessPort(),
            LAtelierVideo = new LVideoOutlet(Interface.TInterface.TInterfaceEngine),
            LAtelierScene = new TConsoleScenePort(),
            LAtelierList = new TListPort().TListCreate,
            LAtelierRelay = new TRelayPort(),
        };
        return new CAtelier(port is null ? ports : port(ports));
    }
}
