using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;
using Cadroue.Tests.Interface;

using Xunit;

namespace Cadroue.Tests.Windows;

[Collection("Preset")]
public sealed class TFixTabCheckup
{
    private const string TFixSource = @"C:\media\clip.mp4";
    private const string TFixStranger = @"C:\media\stranger.mp4";

    private static (QFixTab, QClinic, QList, LDocket) TFixBuild()
    {
        QList list = TInterfaceList.TListCreate();
        LDocket docket = list.QListDocket;
        QFixTab tab = TInterface.TFixTabCreate(
            TInterface.TPresetSelectionCreate("Alpha"), TInterfaceViewer.TViewerCreate(), list);
        QClinic clinic = TInterface.TFixClinicRead(tab);
        TInterfaceList.TDocketPathsAdd(docket, TFixSource);
        TInterface.TClinicSourceSet(clinic, TFixSource);
        TInterface.TClinicStepSet(clinic, "Container");
        return (tab, clinic, list, docket);
    }

    [Fact]
    public void CheckupHandle_ListedPath_ReachesClinic_StrangerIgnored()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (QFixTab tab, QClinic clinic, _, _) = TFixBuild();

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
        (QFixTab tab, QClinic clinic, _, _) = TFixBuild();
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
        (QFixTab tab, QClinic clinic, _, _) = TFixBuild();
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
        (QFixTab tab, _, QList list, _) = TFixBuild();
        TInterfaceList.TListSelect(list, null);
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
        (QFixTab tab, _, QList list, _) = TFixBuild();
        var scanned = new System.Collections.Concurrent.ConcurrentBag<string>();
        using var ready = new ManualResetEventSlim();
        TInterface.TFixScannerAttach(path =>
        {
            scanned.Add(path);
            ready.Set();
        });
        try
        {
            TInterfaceList.TListSelect(list, TFixSource);
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
