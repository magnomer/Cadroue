using System.Windows.Threading;
using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.UIDeportment;
using Cadroue.Tests.Interface;

using Xunit;

namespace Cadroue.Tests.Windows;

[Collection("Preset")]
public sealed class TFixTabCheckup
{
    private const string TFixSource = @"C:\media\clip.mp4";
    private const string TFixStranger = @"C:\media\stranger.mp4";

    private static (QFixTab, CClinic, QList, LDocket) TFixBuild()
    {
        QList list = TInterfaceList.TListCreate();
        LDocket docket = list.QListDocket;
        QFixTab tab = TInterface.TFixTabCreate(
            TInterface.TPresetSelectionCreate("Alpha"), TInterfaceViewer.TViewerCreate(), list);
        CClinic clinic = TInterface.TFixClinicRead(tab);
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
        (QFixTab tab, CClinic clinic, _, _) = TFixBuild();

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
        (QFixTab tab, CClinic clinic, _, _) = TFixBuild();
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
        (QFixTab tab, CClinic clinic, _, _) = TFixBuild();
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
    public void DiagnosisRequest_SelectedUnlockedItem_ScansOnce()
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

    [Fact]
    public void QueuedPublication_CloseAndReattachBeforeDispatch_DoesNotReachClinic()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            var scanner = LCheckup.LCheckupScannerSeam;
            QFixTab? closingTab = null;
            QFixTab? reattachedTab = null;
            try
            {
                Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
                using TPreset presets = new();
                presets.TPresetSeedCreate("Alpha");

                (closingTab, CClinic closingClinic, _, _) = TFixBuild();
                using var closingReady = new ManualResetEventSlim();
                using var closingProgress = new ManualResetEventSlim();
                using var closingStopped = new ManualResetEventSlim();
                LCheckup.LCheckupScannerSeam = async (_, _, token, progress) =>
                {
                    try
                    {
                        progress!.Report(0.4);
                        await Task.Delay(Timeout.InfiniteTimeSpan, token);
                    }
                    catch (OperationCanceledException) when (token.IsCancellationRequested)
                    {
                    }
                    finally
                    {
                        closingStopped.Set();
                    }

                    return Array.Empty<LDossier>();
                };
                TInterface.TFixFaceAttach(closingTab, dispatcher);
                TInterface.TFixCheckupAttach(closingTab, _ => closingReady.Set());
                TInterface.TFixProgressAttach(closingTab, (_, _) => closingProgress.Set());
                TInterface.TFixCheckupStart(closingTab, TFixSource);
                Assert.True(closingReady.Wait(TimeSpan.FromSeconds(5)));
                Assert.True(closingProgress.Wait(TimeSpan.FromSeconds(5)));

                TInterface.TFixClose(closingTab);
                closingTab = null;
                Assert.True(closingStopped.Wait(TimeSpan.FromSeconds(5)));
                var closingFrame = new DispatcherFrame();
                dispatcher.BeginInvoke(
                    DispatcherPriority.Background, new Action(() => closingFrame.Continue = false));
                Dispatcher.PushFrame(closingFrame);

                Assert.NotEqual(
                    LCheckupOutcome.LCheckupOutcomeScanning, TInterface.TClinicOutcomeRead(closingClinic));
                Assert.Equal(0, TInterface.TClinicProgressRead(closingClinic));

                (reattachedTab, CClinic reattachedClinic, _, _) = TFixBuild();
                using var reattachedReady = new ManualResetEventSlim();
                using var reattachedProgress = new ManualResetEventSlim();
                using var currentReady = new ManualResetEventSlim();
                var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                IProgress<double>? currentProgress = null;
                Action<LCheckupResult> checkupSignal = result =>
                {
                    reattachedReady.Set();
                    if (result.LCheckupOutcome == LCheckupOutcome.LCheckupOutcomeClean)
                    {
                        currentReady.Set();
                    }
                };
                Action<string, double> progressSignal = (_, _) => reattachedProgress.Set();
                LCheckup.LCheckupScannerSeam = async (_, _, token, progress) =>
                {
                    currentProgress = progress;
                    progress!.Report(0.4);
                    await release.Task.WaitAsync(token);
                    return Array.Empty<LDossier>();
                };
                TInterface.TFixFaceAttach(reattachedTab, dispatcher);
                TInterface.TFixCheckupAttach(reattachedTab, checkupSignal);
                TInterface.TFixProgressAttach(reattachedTab, progressSignal);
                TInterface.TFixCheckupStart(reattachedTab, TFixSource);
                Assert.True(reattachedReady.Wait(TimeSpan.FromSeconds(5)));
                Assert.True(reattachedProgress.Wait(TimeSpan.FromSeconds(5)));

                TInterface.TFixFaceDetach(reattachedTab);
                TInterface.TFixCheckupDetach(reattachedTab, checkupSignal);
                TInterface.TFixProgressDetach(reattachedTab, progressSignal);
                reattachedProgress.Reset();
                TInterface.TFixFaceAttach(reattachedTab, dispatcher);
                TInterface.TFixCheckupAttach(reattachedTab, checkupSignal);
                TInterface.TFixProgressAttach(reattachedTab, progressSignal);
                var reattachedFrame = new DispatcherFrame();
                dispatcher.BeginInvoke(
                    DispatcherPriority.Background, new Action(() => reattachedFrame.Continue = false));
                Dispatcher.PushFrame(reattachedFrame);

                Assert.NotEqual(
                    LCheckupOutcome.LCheckupOutcomeScanning, TInterface.TClinicOutcomeRead(reattachedClinic));
                Assert.Equal(0, TInterface.TClinicProgressRead(reattachedClinic));

                currentProgress!.Report(0.6);
                Assert.True(reattachedProgress.Wait(TimeSpan.FromSeconds(5)));
                var progressFrame = new DispatcherFrame();
                dispatcher.BeginInvoke(
                    DispatcherPriority.Background, new Action(() => progressFrame.Continue = false));
                Dispatcher.PushFrame(progressFrame);
                Assert.Equal(0.6, TInterface.TClinicProgressRead(reattachedClinic));

                release.SetResult();
                Assert.True(currentReady.Wait(TimeSpan.FromSeconds(5)));
                var readyFrame = new DispatcherFrame();
                dispatcher.BeginInvoke(
                    DispatcherPriority.Background, new Action(() => readyFrame.Continue = false));
                Dispatcher.PushFrame(readyFrame);
                Assert.Equal(
                    LCheckupOutcome.LCheckupOutcomeClean, TInterface.TClinicOutcomeRead(reattachedClinic));

                TInterface.TFixClose(reattachedTab);
                reattachedTab = null;
            }
            catch (Exception exception)
            {
                failure = exception;
            }
            finally
            {
                if (closingTab is not null)
                {
                    TInterface.TFixClose(closingTab);
                }
                if (reattachedTab is not null)
                {
                    TInterface.TFixClose(reattachedTab);
                }
                LCheckup.LCheckupScannerSeam = scanner;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        Assert.Null(failure);
    }
}
