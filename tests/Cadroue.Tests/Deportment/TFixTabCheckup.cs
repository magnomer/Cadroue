using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

[Collection("Preset")]
public sealed class TFixTabCheckup
{
    private const string TFixSource = @"C:\media\clip.mp4";
    private const string TFixStranger = @"C:\media\stranger.mp4";

    private static (LFixTab, LClinic, LList, LDocket) TFixBuild()
    {
        LClinic clinic = TInterface.TClinicCreate();
        LDocket docket = TInterface.TDocketCreate();
        LList list = TInterface.TListCreate(docket);
        LFixTab tab = TInterface.TFixTabCreate(
            TInterface.TPresetSelectionCreate("Alpha"),
            clinic,
            TInterface.TViewerCreate(),
            list,
            docket,
            TInterface.TProcessingCreate());
        TInterface.TDocketPathsAdd(docket, TFixSource);
        TInterface.TClinicSourceSet(clinic, TFixSource);
        TInterface.TClinicStepSet(clinic, "Container");
        return (tab, clinic, list, docket);
    }

    [Fact]
    public void CheckupHandle_ListedPath_ReachesClinic_StrangerIgnored()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (LFixTab tab, LClinic clinic, _, _) = TFixBuild();

        TInterface.TFixCheckupHandle(
            tab, TFixStranger, LFlawKind.LFlawKindContainer, LCheckupOutcome.LCheckupOutcomeClean);
        Assert.NotEqual(LCheckupOutcome.LCheckupOutcomeClean, TInterface.TClinicOutcomeRead(clinic));

        TInterface.TFixCheckupHandle(
            tab, TFixSource, LFlawKind.LFlawKindContainer, LCheckupOutcome.LCheckupOutcomeClean);
        Assert.Equal(LCheckupOutcome.LCheckupOutcomeClean, TInterface.TClinicOutcomeRead(clinic));
        TInterface.TFixClose(tab);
    }

    [Fact]
    public void ProgressHandle_ListedPath_ReachesClinic_StrangerIgnored()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (LFixTab tab, LClinic clinic, _, _) = TFixBuild();
        TInterface.TFixCheckupHandle(
            tab, TFixSource, LFlawKind.LFlawKindContainer, LCheckupOutcome.LCheckupOutcomeScanning);

        TInterface.TFixProgressHandle(tab, TFixStranger, 0.9);
        Assert.Equal(0, TInterface.TClinicProgressRead(clinic));

        TInterface.TFixProgressHandle(tab, TFixSource, 0.4);
        Assert.Equal(0.4, TInterface.TClinicProgressRead(clinic));
        TInterface.TFixClose(tab);
    }

    [Fact]
    public void ClearHandle_RemovesResultsForRemovedPaths()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (LFixTab tab, LClinic clinic, _, _) = TFixBuild();
        TInterface.TFixCheckupHandle(
            tab, TFixSource, LFlawKind.LFlawKindContainer, LCheckupOutcome.LCheckupOutcomeClean);

        TInterface.TFixClearHandle(tab, TFixSource);

        Assert.NotEqual(LCheckupOutcome.LCheckupOutcomeClean, TInterface.TClinicOutcomeRead(clinic));
        TInterface.TFixClose(tab);
    }

    [Fact]
    public void DiagnosisRun_NoSelection_StartsNothing()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (LFixTab tab, _, LList list, _) = TFixBuild();
        TInterface.TListSelect(list, null);
        var scanned = new System.Collections.Concurrent.ConcurrentBag<string>();
        TInterface.TFixScannerAttach(scanned.Add);
        try
        {
            TInterface.TFixDiagnosisRun(tab);
            Thread.Sleep(100);

            Assert.Empty(scanned);
        }
        finally
        {
            TInterface.TFixScannerAttach(null);
            TInterface.TFixClose(tab);
        }
    }

    [Fact]
    public void DiagnosisRun_SelectedUnlockedItem_ScansOnce()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (LFixTab tab, _, LList list, _) = TFixBuild();
        var scanned = new System.Collections.Concurrent.ConcurrentBag<string>();
        using var ready = new ManualResetEventSlim();
        TInterface.TFixScannerAttach(path =>
        {
            scanned.Add(path);
            ready.Set();
        });
        try
        {
            TInterface.TListSelect(list, TFixSource);
            TInterface.TFixDiagnosisRun(tab);

            Assert.True(ready.Wait(TimeSpan.FromSeconds(5)));
            Assert.Equal([TFixSource], scanned);
        }
        finally
        {
            TInterface.TFixScannerAttach(null);
            TInterface.TFixClose(tab);
        }
    }
}
