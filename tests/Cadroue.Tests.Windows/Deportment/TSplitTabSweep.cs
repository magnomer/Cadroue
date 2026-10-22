using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;
using System.Reflection;
using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.ShellEngine;
using Cadroue.UIDeportment;
using Cadroue.Tests.Interface;

using Xunit;

namespace Cadroue.Tests.Windows;

[Collection("Preset")]
public sealed class TSplitTabSweep
{
    private const string TSplitSource = @"C:\media\clip.mp4";

    private static (QSplitTab, QInspector, QList, LDocket, QFlow) TSplitBuild()
    {
        QList list = TInterfaceList.TListCreate();
        LDocket docket = list.QListDocket;
        QFlow flow = TInterface.TFlowCreate();
        QSplitTab tab = TInterface.TSplitTabCreate(
            TInterface.TPresetSelectionCreate("Alpha"), TInterfaceViewer.TViewerCreate(), list, flow);
        QInspector inspector = TInterface.TSplitInspectorRead(tab);
        return (tab, inspector, list, docket, flow);
    }

    [Fact]
    public void StepsRead_EnabledKindsInCatalogOrder()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (QSplitTab tab, QInspector inspector, _, _, _) = TSplitBuild();
        QSensor sensor = inspector.QInspectorSensor;
        TInterface.TSensorEnabledSet(sensor, LDetectorKind.LDetectorKindVolume, true);
        TInterface.TSensorEnabledSet(sensor, LDetectorKind.LDetectorKindScene, true);
        TInterface.TBlankStepSet(
            inspector.QInspectorBlank,
            TInterface.TBlankStepCreate(LDetectorType.LDetectorTypeBlack) with { LDetectorBlankEnabled = true });

        IReadOnlyList<LDetectorKind> steps = TInterface.TSplitStepsRead(tab);

        Assert.Equal(
            [LDetectorKind.LDetectorKindBlank, LDetectorKind.LDetectorKindScene, LDetectorKind.LDetectorKindVolume],
            steps);
    }

    [Fact]
    public void StepsRead_NothingEnabled_IsEmpty()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (QSplitTab tab, _, _, _, _) = TSplitBuild();

        Assert.Empty(TInterface.TSplitStepsRead(tab));
    }

    [Fact]
    public void StepsRead_UsesDetectorGateState_InCatalogOrder()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (QSplitTab tab, QInspector inspector, _, _, _) = TSplitBuild();
        CDetector detector = inspector.QInspectorGate.CInspectorDetector;
        detector.CDetectorEnabledSet(LDetectorKind.LDetectorKindVolume, true);
        detector.CDetectorEnabledSet(LDetectorKind.LDetectorKindScene, true);

        Assert.Equal(
            [LDetectorKind.LDetectorKindScene, LDetectorKind.LDetectorKindVolume],
            TInterface.TSplitStepsRead(tab));
    }

    [Fact]
    public async Task SweepStart_NoSelection_DoesNotRun()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (QSplitTab tab, QInspector inspector, _, _, _) = TSplitBuild();
        TInterface.TSensorEnabledSet(inspector.QInspectorSensor, LDetectorKind.LDetectorKindScene, true);
        var busy = new List<bool>();
        TInterface.TSplitBusyAttach(tab, busy.Add);

        await TInterface.TSplitSweepStart(tab);

        Assert.Empty(busy);
        Assert.False(TInterface.TSplitSweepCheck(tab));
        Assert.False(inspector.QInspectorGate.CInspectorSweep.CSweepRunning);
    }

    [Fact]
    public async Task SweepStart_NoSpool_DoesNotRun()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (QSplitTab tab, QInspector inspector, QList list, LDocket docket, _) = TSplitBuild();
        TInterfaceList.TDocketPathsAdd(docket, TSplitSource);
        TInterfaceList.TListSelect(list, TSplitSource);
        TInterface.TSensorEnabledSet(inspector.QInspectorSensor, LDetectorKind.LDetectorKindScene, true);
        var busy = new List<bool>();
        TInterface.TSplitBusyAttach(tab, busy.Add);

        await TInterface.TSplitSweepStart(tab);

        Assert.Empty(busy);
        Assert.False(TInterface.TSplitSweepCheck(tab));
    }

    [Fact]
    public async Task SweepStart_NothingEnabled_DoesNotRun()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (QSplitTab tab, _, QList list, LDocket docket, QFlow flow) = TSplitBuild();
        TInterfaceList.TDocketPathsAdd(docket, TSplitSource);
        TInterfaceList.TListSelect(list, TSplitSource);
        TInterface.TFlowCommandSet(flow, true);
        TInterface.TFlowAttach(
            flow, TInterfaceViewer.TViewerInfoCreate(
                TimeSpan.FromSeconds(60), 1920, 1080), TSplitSource, TimeSpan.Zero);
        var busy = new List<bool>();
        TInterface.TSplitBusyAttach(tab, busy.Add);

        await TInterface.TSplitSweepStart(tab);

        Assert.Empty(busy);
        Assert.False(TInterface.TSplitSweepCheck(tab));
    }

    [Fact]
    public void SweepCancel_Idle_IsNoop()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (QSplitTab tab, QInspector inspector, _, _, _) = TSplitBuild();

        inspector.QInspectorFrame.QInspectorFrameSet("Scene");
        Assert.True(inspector.QInspectorSensorCheck(LDetectorKind.LDetectorKindScene));
        TInterface.TSplitSweepCancel(tab);
        TInterface.TSplitClose(tab);

        Assert.False(TInterface.TSplitSweepCheck(tab));
    }

    [Fact]
    public void SweepButton_RunThenStop_RequestsOnceAndSynchronizesState()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            Func<CancellationToken, Task> claim = LSweepEngine.LSweepClaimSeam;
            try
            {
                Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
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
                program.Resources["PPanelText"] = Brushes.Black;
                program.Resources["PPanelMargin"] = new Thickness(4);
                program.Resources["PIconRoot"] = "pack://application:,,,/";
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
                (QSplitTab tab, QInspector inspector, QList list, LDocket docket, QFlow flow) = TSplitBuild();
                TInterfaceList.TDocketPathsAdd(docket, TSplitSource);
                TInterfaceList.TListSelect(list, TSplitSource);
                TInterface.TFlowCommandSet(flow, true);
                TInterface.TFlowAttach(
                    flow, TInterfaceViewer.TViewerInfoCreate(
                        TimeSpan.FromSeconds(60), 1920, 1080), TSplitSource, TimeSpan.Zero);
                TInterface.TSensorEnabledSet(
                    inspector.QInspectorSensor, LDetectorKind.LDetectorKindScene, true);
                LSweepEngine.LSweepClaimSeam = token => Task.Delay(Timeout.InfiniteTimeSpan, token);

                int runs = 0;
                int stops = 0;
                var states = new List<(bool, bool)>();
                inspector.QInspectorGate.CInspectorSweep.CSweepRunApplied += () => runs++;
                inspector.QInspectorGate.CInspectorSweep.CSweepStopApplied += () => stops++;
                TInterface.TSplitBusyAttach(
                    tab, busy => states.Add((busy, inspector.QInspectorGate.CInspectorSweep.CSweepRunning)));
                QSensorSweep driver = inspector.QInspectorSensor.QSensorSweep;
                driver.QSensorSweepBuild();
                Button button = QContract.QContractFind<Button>(driver.QSensorSweepBody, "PSensorRunButton");

                button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Assert.Equal((1, 0), (runs, stops));
                Assert.True(TInterface.TSplitSweepCheck(tab));
                button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Assert.Equal((1, 1), (runs, stops));

                DateTime deadline = DateTime.UtcNow.AddSeconds(5);
                while (TInterface.TSplitSweepCheck(tab) && DateTime.UtcNow < deadline)
                {
                    var frame = new DispatcherFrame();
                    dispatcher.BeginInvoke(DispatcherPriority.Background, () => frame.Continue = false);
                    Dispatcher.PushFrame(frame);
                }

                Assert.False(TInterface.TSplitSweepCheck(tab));
                Assert.Equal([(true, true), (false, false)], states);

                program.Resources.Remove("PSensorSweepBody");
                string veneerFolder = Path.GetFullPath(Path.Combine(
                    AppContext.BaseDirectory, "../../../../../src/Cadroue.UIVeneer/bin"));
                string veneerAssembly = Directory.GetFiles(
                        veneerFolder, "Cadroue.UIVeneer.dll", SearchOption.AllDirectories)
                    .OrderByDescending(File.GetLastWriteTimeUtc)
                    .First();
                Assembly.LoadFrom(veneerAssembly);
                program.Resources["PIconRoot"] =
                    "pack://application:,,,/Cadroue.UIVeneer;component/";
                ((System.Collections.Concurrent.ConcurrentDictionary<string, ImageSource>)typeof(QIcon)
                    .GetField("qIconCache", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!).Clear();
                foreach (string sheet in new[]
                         {
                             "Kit/PTool", "Kit/PCheckbox", "Kit/Button/PButton", "Kit/Dropdown/PDropdown",
                             "Kit/PSlider", "Kit/PTextbox", "Kit/PRadio",
                             "Wing/Inspector/PInspectorCrop", "Wing/Inspector/PInspectorOrientation",
                             "Wing/Inspector/PInspectorCropGrid", "Wing/Inspector/PInspectorRatio",
                             "Wing/Inspector/PTone", "Wing/Inspector/PGamma", "Wing/Inspector/PExposure",
                             "Wing/Inspector/Curve/PCurveCanvas", "Wing/Inspector/Curve/PCurve",
                             "Wing/Inspector/Whitebalance/PWhitebalanceNeutral",
                             "Wing/Inspector/Whitebalance/PWhitebalanceWheel",
                             "Wing/Inspector/Whitebalance/PWhitebalance", "Wing/Inspector/PVolume",
                             "Wing/Inspector/PLoudness", "Wing/Inspector/PNoise", "Wing/Inspector/PFilter",
                             "Wing/Inspector/PEqualizer", "Wing/Inspector/PSkip", "Wing/Inspector/PBlank",
                             "Wing/Sensor/PSensorSweep", "Wing/Sensor/PSensor",
                         })
                {
                    var contract = new ResourceDictionary
                    {
                        Source = new Uri(
                            $"/Cadroue.UIVeneer;component/{sheet}.xaml", UriKind.RelativeOrAbsolute)
                    };
                    foreach (object key in contract.Keys.Cast<object>().ToArray())
                    {
                        program.Resources.Remove(key);
                    }

                    program.Resources.MergedDictionaries.Add(contract);
                }

                Func<FrameworkElement> surfaceBuild = () =>
                {
                    var contract = new ResourceDictionary
                    {
                        Source = new Uri(
                            "/Cadroue.UIVeneer;component/Wing/Inspector/PInspector.xaml",
                            UriKind.RelativeOrAbsolute)
                    };
                    return (FrameworkElement)contract["PInspector"];
                };
                QInspector[] inspectors =
                [
                    TInterface.TEditInspectorRead(TInterface.TEditTabCreate(
                        TInterface.TPresetSelectionCreate("Alpha"),
                        TInterfaceViewer.TViewerCreate(),
                        TInterfaceList.TListCreate())),
                    TInterface.TAudioInspectorRead(TInterface.TAudioTabCreate(
                        TInterface.TPresetSelectionCreate("Alpha"),
                        TInterfaceViewer.TViewerCreate(),
                        TInterfaceList.TListCreate())),
                    inspector,
                ];
                foreach (QInspector owner in inspectors)
                {
                    FrameworkElement first = surfaceBuild();
                    owner.QInspectorFrame.QInspectorFrameBuild(first);
                    Grid sections = QContract.QContractFind<Grid>(first, "PInspectorSections");
                    Assert.Equal(15, sections.Children.OfType<StackPanel>()
                        .Count(section => section.Tag is string));
                    Assert.Equal(5, sections.Children.OfType<ContentControl>().Count());
                    int blank = sections.Children.IndexOf(owner.QInspectorBlank.QBlankBody);
                    int lastTool = sections.Children.Cast<UIElement>()
                        .Select((section, index) => (section, index))
                        .Where(entry => entry.section is StackPanel { Tag: string })
                        .Max(entry => entry.index);
                    int firstSensor = sections.Children.Cast<UIElement>()
                        .Select((section, index) => (section, index))
                        .Where(entry => entry.section is ContentControl)
                        .Min(entry => entry.index);
                    Assert.True(lastTool < blank && blank < firstSensor);

                    QSensorPlan plan = owner.QInspectorSensor.QSensorPlans.Single(
                        entry => entry.QSensorPlanKind == LDetectorKind.LDetectorKindScene);
                    CheckBox oldCheck = QContract.QContractFind<CheckBox>(plan.PSensorBody, "PSensorEnablement");
                    ComboBox oldPreset = QContract.QContractFind<ComboBox>(plan.PSensorBody, "PSensorProfiles");
                    Grid oldThreshold = QContract.QContractFind<Grid>(plan.PSensorBody, "PSensorThreshold");
                    Slider oldSlider = (Slider)oldThreshold.Children[1];
                    TextBox oldText = (TextBox)oldThreshold.Children[3];
                    Grid oldModes = QContract.QContractFind<Grid>(plan.PSensorBody, "PSensorModes");
                    RadioButton oldRadio = ((StackPanel)((Border)oldModes.Children[1]).Child).Children
                        .Cast<RadioButton>().First(option => option.IsChecked != true);
                    int notices = 0;
                    owner.QInspectorGate.CInspectorDetector.CDetectorChanged += () => notices++;

                    owner.QInspectorFrame.QInspectorFrameBuild(surfaceBuild());
                    Assert.Equal(0, notices);
                    oldCheck.IsChecked = oldCheck.IsChecked != true;
                    oldSlider.Value = oldSlider.Value == oldSlider.Minimum ? oldSlider.Maximum : oldSlider.Minimum;
                    oldText.Text = "17";
                    oldText.RaiseEvent(new RoutedEventArgs(UIElement.LostFocusEvent));
                    oldPreset.SelectedIndex = oldPreset.SelectedIndex == 0 ? 1 : 0;
                    oldRadio.IsChecked = true;
                    Assert.Equal(0, notices);

                    CheckBox currentCheck = QContract.QContractFind<CheckBox>(plan.PSensorBody, "PSensorEnablement");
                    currentCheck.IsChecked = currentCheck.IsChecked != true;
                    Assert.Equal(1, notices);
                    owner.QInspectorFrame.QInspectorFrameDetach();
                    Grid currentThreshold = QContract.QContractFind<Grid>(plan.PSensorBody, "PSensorThreshold");
                    ((Slider)currentThreshold.Children[1]).Value = oldSlider.Value;
                    ((TextBox)currentThreshold.Children[3]).RaiseEvent(
                        new RoutedEventArgs(UIElement.LostFocusEvent));
                    QContract.QContractFind<ComboBox>(plan.PSensorBody, "PSensorProfiles").SelectedIndex = 0;
                    Grid currentModes = QContract.QContractFind<Grid>(plan.PSensorBody, "PSensorModes");
                    ((StackPanel)((Border)currentModes.Children[1]).Child).Children.Cast<RadioButton>()
                        .First().IsChecked = true;
                    currentCheck.IsChecked = currentCheck.IsChecked != true;
                    Assert.Equal(1, notices);
                }

                tab.QSplitClose();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
            finally
            {
                LSweepEngine.LSweepClaimSeam = claim;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        Assert.Null(failure);
    }

    [Fact]
    public void LockHandle_LockedDisablesSectionEditing()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (QSplitTab tab, _, _, _, QFlow flow) = TSplitBuild();

        TInterface.TSplitLockHandle(tab, true);
        Assert.False(flow.QFlowEdit.QFlowSectionEditable);

        TInterface.TSplitLockHandle(tab, false);
        Assert.True(flow.QFlowEdit.QFlowSectionEditable);
    }
}
