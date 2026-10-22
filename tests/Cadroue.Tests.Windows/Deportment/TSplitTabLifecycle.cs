using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;
using Cadroue.Application;
using Cadroue.Core;
using Cadroue.ShellEngine;
using Cadroue.UIDeportment;
using Cadroue.Tests.Interface;

using Xunit;

namespace Cadroue.Tests.Windows;

[Collection("Preset")]
public sealed class TSplitTabLifecycle
{
    private const string TSplitSource = @"C:\media\clip.mp4";

    [Fact]
    public void SweepButton_AfterClose_DoesNotStart()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            Func<CancellationToken, Task> claim = LSweepEngine.LSweepClaimSeam;
            QSplitTab? tab = null;
            Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
            try
            {
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(dispatcher));
                System.Windows.Application program = System.Windows.Application.Current
                    ?? new System.Windows.Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                program.Resources["PToolFont"] = new FontFamily("Segoe UI");
                program.Resources["PToolText"] = Brushes.Black;
                program.Resources["PToolMuted"] = Brushes.Gray;
                program.Resources["PToolIcon"] = Brushes.Black;
                program.Resources["PToolAccent"] = Brushes.Blue;
                program.Resources["PToolDimmed"] = 0.5;
                program.Resources["PPanelLine"] = Brushes.Gray;
                foreach (string sheet in new[]
                         {
                             "Kit/PCheckbox", "Kit/Button/PButton", "Wing/Sensor/PSensorSweep",
                         })
                {
                    string path = Path.GetFullPath(Path.Combine(
                        AppContext.BaseDirectory, $"../../../../../src/Cadroue.UIVeneer/{sheet}.xaml"));
                    if (sheet.StartsWith("Wing/", StringComparison.Ordinal))
                    {
                        var document = System.Xml.Linq.XDocument.Load(path);
                        System.Xml.Linq.XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
                        foreach (System.Xml.Linq.XElement surface in document.Root!.Elements())
                        {
                            string key = (string)surface.Attribute(x + "Key")!;
                            surface.Attribute(x + "Key")!.Remove();
                            surface.Attribute(x + "Shared")?.Remove();
                            surface.SetAttributeValue(System.Xml.Linq.XNamespace.Xmlns + "x", x.NamespaceName);
                            program.Resources[key] = XamlReader.Parse(surface.ToString());
                        }

                        continue;
                    }

                    string contract = File.ReadAllText(path)
                        .Replace(" x:Shared=\"False\"", string.Empty, StringComparison.Ordinal);
                    using var reader = System.Xml.XmlReader.Create(new StringReader(contract));
                    program.Resources.MergedDictionaries.Add((ResourceDictionary)XamlReader.Load(reader));
                }

                using TPreset presets = new();
                presets.TPresetSeedCreate("Alpha");
                QList list = TInterfaceList.TListCreate();
                LDocket docket = list.QListDocket;
                QFlow flow = TInterface.TFlowCreate();
                tab = TInterface.TSplitTabCreate(
                    TInterface.TPresetSelectionCreate("Alpha"), TInterfaceViewer.TViewerCreate(), list, flow);
                QInspector inspector = TInterface.TSplitInspectorRead(tab);
                TInterfaceList.TDocketPathsAdd(docket, TSplitSource);
                TInterfaceList.TListSelect(list, TSplitSource);
                TInterface.TFlowCommandSet(flow, true);
                TInterface.TFlowAttach(
                    flow, TInterfaceViewer.TViewerInfoCreate(
                        TimeSpan.FromSeconds(60), 1920, 1080), TSplitSource, TimeSpan.Zero);
                TInterface.TSensorEnabledSet(
                    inspector.QInspectorSensor, LDetectorKind.LDetectorKindScene, true);
                int scans = 0, runs = 0, stops = 0;
                inspector.QInspectorGate.CInspectorSweep.CSweepRunApplied += () => runs++;
                inspector.QInspectorGate.CInspectorSweep.CSweepStopApplied += () => stops++;
                LSweepEngine.LSweepClaimSeam = token => { scans++; return Task.Delay(-1, token); };
                var busy = new List<bool>();
                TInterface.TSplitBusyAttach(tab, busy.Add);
                QSensorSweep driver = inspector.QInspectorSensor.QSensorSweep;
                driver.QSensorSweepBuild();
                Button button = QContract.QContractFind<Button>(driver.QSensorSweepBody, "PSensorRunButton");
                CheckBox persistent = QContract.QContractFind<CheckBox>(
                    driver.QSensorSweepBody, "PSensorRunPersistent");

                tab.QSplitClose();
                button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                persistent.IsChecked = true;
                persistent.RaiseEvent(new RoutedEventArgs(CheckBox.ClickEvent));
                var frame = new DispatcherFrame();
                dispatcher.BeginInvoke(DispatcherPriority.Background, () => frame.Continue = false);
                Dispatcher.PushFrame(frame);

                Assert.Equal((0, 0), (runs, stops));
                Assert.Equal(0, scans);
                Assert.Empty(busy);
                Assert.False(TInterface.TSplitSweepCheck(tab));
                Assert.False(inspector.QInspectorGate.CInspectorSweep.CSweepRunning);
                Assert.False(inspector.QInspectorGate.CInspectorSweep.CSweepPersistent);
            }
            catch (Exception exception)
            {
                failure = exception;
            }
            finally
            {
                if (tab is not null)
                {
                    TInterface.TSplitSweepCancel(tab);
                    DateTime deadline = DateTime.UtcNow.AddSeconds(5);
                    while (TInterface.TSplitSweepCheck(tab) && DateTime.UtcNow < deadline)
                    {
                        var frame = new DispatcherFrame();
                        dispatcher.BeginInvoke(DispatcherPriority.Background, () => frame.Continue = false);
                        Dispatcher.PushFrame(frame);
                    }
                }

                LSweepEngine.LSweepClaimSeam = claim;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        Assert.Null(failure);
    }

    [Fact]
    public async Task Close_DetachesPathLockAndDropHandlers()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        QList list = TInterfaceList.TListCreate();
        LDocket docket = list.QListDocket;
        QFlow flow = TInterface.TFlowCreate();
        QViewer viewer = TInterfaceViewer.TViewerCreate();
        QSplitTab tab = TInterface.TSplitTabCreate(
            TInterface.TPresetSelectionCreate("Alpha"), viewer, list, flow);
        string locked = Path.Combine(Path.GetTempPath(), $"cadroue-locked-{Guid.NewGuid():N}.mp4");
        string dropped = Path.Combine(Path.GetTempPath(), $"cadroue-dropped-{Guid.NewGuid():N}.mp4");
        File.WriteAllBytes(dropped, []);
        try
        {
            TInterfaceList.TDocketDeliveredAdd(docket, locked, true);
            var opened = new List<string>();
            var drops = new List<IReadOnlyList<string>>();
            viewer.QViewerWorkspace.CWorkspaceOpened += opened.Add;
            TInterfaceViewer.TViewerDropAttach(viewer, drops.Add);
            tab.QSplitStart();

            TInterface.TSplitClose(tab);
            TInterfaceList.TListSelect(list, locked);
            TInterfaceViewer.TViewerDropHandle(viewer, [dropped], true);
            await Task.Delay(250);

            Assert.Empty(opened);
            Assert.True(flow.QFlowEdit.QFlowSectionEditable);
            Assert.Equal([new[] { dropped }], drops);
            Assert.DoesNotContain(TInterfaceList.TDocketItemsRead(docket), item => item.LDocketEntryPath == dropped);
        }
        finally
        {
            File.Delete(dropped);
        }
    }
}
