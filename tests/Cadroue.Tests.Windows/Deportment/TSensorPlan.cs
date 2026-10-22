using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.UIDeportment;
using Cadroue.Tests.Interface;

using Xunit;

namespace Cadroue.Tests.Windows;

[Collection(nameof(TProgramCollection))]
public sealed class TSensorPlan
{
    [Fact]
    public void Plans_CoverEveryKindButBlank_WithRowsAndGroups()
    {
        QSensor sensor = TInterface.TSensorCreate();
        IReadOnlyList<QSensorPlan> plans = TInterface.TSensorPlansRead(sensor);

        Assert.Equal(5, plans.Count);
        Assert.DoesNotContain(plans, plan => plan.QSensorPlanKind == LDetectorKind.LDetectorKindBlank);
        QSensorPlan luminance = plans.Single(plan => plan.QSensorPlanKind == LDetectorKind.LDetectorKindLuminance);
        Assert.Equal(3, luminance.QSensorPlanRows.Count);
        Assert.True(luminance.QSensorPlanSpeed.QSensorGroupShown);
        Assert.False(luminance.QSensorPlanMode.QSensorGroupShown);
        Assert.Equal(3, luminance.QSensorPlanSpeed.QSensorGroupNames.Count);
        QSensorPlan scene = plans.Single(plan => plan.QSensorPlanKind == LDetectorKind.LDetectorKindScene);
        Assert.Equal(2, scene.QSensorPlanRows.Count);
        Assert.True(scene.QSensorPlanPreset);
        QSensorPlan still = plans.Single(plan => plan.QSensorPlanKind == LDetectorKind.LDetectorKindStill);
        Assert.True(still.QSensorPlanMode.QSensorGroupShown);
        QSensorPlan volume = plans.Single(plan => plan.QSensorPlanKind == LDetectorKind.LDetectorKindVolume);
        Assert.True(volume.QSensorPlanMetric.QSensorGroupShown);
    }

    [Fact]
    public void ValueSet_ByRow_RoundTripsThroughStep()
    {
        QSensor sensor = TInterface.TSensorCreate();
        const LDetectorKind kind = LDetectorKind.LDetectorKindLuminance;

        TInterface.TSensorValueSet(sensor, kind, QSensorPlan.QSensorPlanThreshold, 33);
        TInterface.TSensorValueSet(sensor, kind, QSensorPlan.QSensorPlanWindow, 2);
        TInterface.TSensorValueSet(sensor, kind, QSensorPlan.QSensorPlanMinimum, 4);

        LDetectorStep step = TInterface.TSensorStepRead(sensor, kind);
        Assert.Equal(step.LDetectorStepThreshold, TInterface.TSensorValueRead(sensor, kind, 0));
        Assert.Equal(step.LDetectorStepWindow, TInterface.TSensorValueRead(sensor, kind, 1));
        Assert.Equal(step.LDetectorStepMinimum, TInterface.TSensorValueRead(sensor, kind, 2));
        Assert.Equal(33, step.LDetectorStepThreshold);
        Assert.Equal(
            TInterface.TDetectorThresholdRead(kind).LDetectorBoundDefault,
            TInterface.TSensorDefaultRead(sensor, kind, 0));
    }

    [Fact]
    public void RadioSelects_MapIndexToMode_IgnoreOutOfRange()
    {
        QSensor sensor = TInterface.TSensorCreate();

        TInterface.TSensorModeSelect(sensor, 1);
        Assert.Equal(LDetectorStillMode.LDetectorStillTreat, sensor.QSensorMode);
        Assert.Equal(1, sensor.CDetector.CDetectorModeIndex);
        TInterface.TSensorModeSelect(sensor, 7);
        Assert.Equal(1, sensor.CDetector.CDetectorModeIndex);

        TInterface.TSensorSpeedSelect(sensor, 0);
        Assert.Equal(LDetectorLuminanceMode.LDetectorLuminanceFast, sensor.QSensorSpeed);
        TInterface.TSensorSpeedSelect(sensor, -1);
        Assert.Equal(0, sensor.CDetector.CDetectorSpeedIndex);

        TInterface.TSensorMetricSelect(sensor, 1);
        Assert.Equal(LDetectorMetricMode.LDetectorMetricRms, sensor.QSensorMetric);
        Assert.Equal("dB", TInterface.TSensorUnitRead(sensor, LDetectorKind.LDetectorKindVolume, 0));
        TInterface.TSensorMetricSelect(sensor, 0);
        Assert.Equal("LU", TInterface.TSensorUnitRead(sensor, LDetectorKind.LDetectorKindVolume, 0));
        Assert.Equal("s", TInterface.TSensorUnitRead(sensor, LDetectorKind.LDetectorKindVolume, 2));
    }

    [Fact]
    public void Choice_IndexFollowsMatch_SelectAppliesPreset()
    {
        QSensor sensor = TInterface.TSensorCreate();
        const LDetectorKind kind = LDetectorKind.LDetectorKindScene;
        IReadOnlyList<string> tokens = TInterface.TDetectorTokensRead(kind);

        QInspectorChoice choice = TInterface.TSensorChoiceRead(sensor, kind);
        Assert.Equal(tokens.Count + 1, choice.QInspectorChoiceNames.Count);
        Assert.Equal(tokens.Count, choice.QInspectorChoiceCustom);
        Assert.Equal(tokens.ToList().IndexOf(LDetectorCatalog.LDetectorTokenDefault), choice.QInspectorChoiceIndex);

        TInterface.TSensorChoiceSelect(sensor, kind, 0);
        Assert.Equal(tokens[0], TInterface.TSensorTokenRead(sensor, kind));
        Assert.Equal(0, TInterface.TSensorChoiceRead(sensor, kind).QInspectorChoiceIndex);

        TInterface.TSensorChoiceSelect(sensor, kind, tokens.Count);
        Assert.Equal(tokens[0], TInterface.TSensorTokenRead(sensor, kind));

        TInterface.TSensorThresholdSet(
            sensor,
            kind,
            TInterface.TSensorStepRead(sensor, kind).LDetectorStepThreshold + 1);
        QInspectorChoice drifted = TInterface.TSensorChoiceRead(sensor, kind);

        Assert.Equal(drifted.QInspectorChoiceCustom, drifted.QInspectorChoiceIndex);
        Assert.Equal(drifted.QInspectorChoiceNames[^1], drifted.QInspectorChoiceText);
        Assert.NotEqual(
            Interface.TInterface.TLocalizationTextRead("Inspector.Common.Custom"),
            drifted.QInspectorChoiceText);
    }

    [Fact]
    public void Facade_AndInspectorGateShareState_AndNoticeOnce()
    {
        QInspector inspector = TInterfaceInspector.TInspectorCreate();
        QSensor sensor = inspector.QInspectorSensor;
        CDetector detector = inspector.QInspectorGate.CInspectorDetector;
        const LDetectorKind kind = LDetectorKind.LDetectorKindScene;
        int notices = 0;
        sensor.CDetector.CDetectorChanged += () => notices++;

        detector.CDetectorThresholdSet(kind, 33);
        sensor.CDetector.CDetectorMinimumSet(kind, 2);
        sensor.CDetector.CDetectorMinimumSet(kind, 2);

        Assert.Equal(33, sensor.QSensorStepRead(kind).LDetectorStepThreshold, 10);
        Assert.Equal(2, detector.CDetectorStepRead(kind).LDetectorStepMinimum);
        Assert.Equal(2, notices);
    }

    [Theory]
    [InlineData(LDetectorKind.LDetectorKindBlank, "Blank", "Inspector.Step.Blank")]
    [InlineData(LDetectorKind.LDetectorKindScene, "Scene", "Inspector.Step.Scene")]
    [InlineData(LDetectorKind.LDetectorKindStill, "Still Image", "Inspector.Step.Still")]
    [InlineData(LDetectorKind.LDetectorKindLuminance, "Luminance", "Inspector.Step.Luminance")]
    [InlineData(LDetectorKind.LDetectorKindSilence, "Silence", "Inspector.Step.Silence")]
    [InlineData(LDetectorKind.LDetectorKindVolume, "Volume", "Inspector.Step.Volume")]
    public void Frame_StillImageStatesClassifyTitleEmptyAndVisibility(
        LDetectorKind kind, string step, string titleKey)
    {
        QInspector inspector = TInterfaceInspector.TInspectorCreate();

        inspector.QInspectorStepSet(step);
        IReadOnlyDictionary<LDetectorKind, bool> shown = inspector.QInspectorSensorCheck();

        Assert.True(inspector.QInspectorSensorCheck(kind));
        Assert.False(inspector.QInspectorEmptyShown);
        Assert.Equal(Interface.TInterface.TLocalizationTextRead(titleKey), inspector.QInspectorTitleRead());
        Assert.All(shown, entry => Assert.Equal(entry.Key == kind, entry.Value));
    }

    [Fact]
    public void Repaint_DoesNotRequestSweep()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
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
                string veneerFolder = Path.GetFullPath(Path.Combine(
                    AppContext.BaseDirectory, "../../../../../src/Cadroue.UIVeneer/bin"));
                string veneerAssembly = Directory.GetFiles(
                        veneerFolder, "Cadroue.UIVeneer.dll", SearchOption.AllDirectories)
                    .OrderByDescending(File.GetLastWriteTimeUtc)
                    .First();
                System.Reflection.Assembly.LoadFrom(veneerAssembly);
                program.Resources["PIconRoot"] =
                    "pack://application:,,,/Cadroue.UIVeneer;component/";
                ((System.Collections.Concurrent.ConcurrentDictionary<string, ImageSource>)typeof(QIcon)
                    .GetField("qIconCache", System.Reflection.BindingFlags.Static
                        | System.Reflection.BindingFlags.NonPublic)!.GetValue(null)!).Clear();
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

                var frameContract = new ResourceDictionary
                {
                    Source = new Uri(
                        "/Cadroue.UIVeneer;component/Wing/Inspector/PInspector.xaml",
                        UriKind.RelativeOrAbsolute)
                };
                var frameSurface = (FrameworkElement)frameContract["PInspector"];
                QInspector inspector = TInterfaceInspector.TInspectorCreate();
                inspector.QInspectorFrame.QInspectorFrameBuild(frameSurface);
                CSweep sweep = inspector.QInspectorGate.CInspectorSweep;
                QSensorSweep driver = inspector.QInspectorSensor.QSensorSweep;
                int runs = 0;
                int stops = 0;
                sweep.CSweepRunApplied += () => runs++;
                sweep.CSweepStopApplied += () => stops++;

                driver.QSensorSweepBuild();
                sweep.CSweepRunningSet(true);
                sweep.CSweepPersistentSet(true);
                sweep.CSweepRunningSet(false);
                sweep.CSweepPersistentSet(false);

                Assert.Equal(0, runs);
                Assert.Equal(0, stops);
                Assert.False(QContract.QContractFind<CheckBox>(
                    driver.QSensorSweepBody, "PSensorRunPersistent").IsChecked);
                Assert.Equal(
                    Visibility.Collapsed,
                    QContract.QContractFind<ProgressBar>(driver.QSensorSweepBody, "PSensorProgress").Visibility);

                QSensor sensor = inspector.QInspectorSensor;
                const LDetectorKind kind = LDetectorKind.LDetectorKindScene;
                Dictionary<LDetectorKind, StackPanel> sensorSurfaces = sensor.QSensorPlans.ToDictionary(
                    plan => plan.QSensorPlanKind, plan => plan.PSensorBody);
                TextBlock title = QContract.QContractFind<TextBlock>(frameSurface, "PInspectorTitle");
                TextBlock vacancy = QContract.QContractFind<TextBlock>(frameSurface, "PInspectorVacancy");
                StackPanel blank = inspector.QInspectorBlank.QBlankBody;

                foreach (StackPanel section in sensorSurfaces.Values)
                {
                    foreach (string group in new[] { "PSensorModes", "PSensorSpeeds", "PSensorMetrics" })
                    {
                        Grid field = QContract.QContractFind<Grid>(section, group);
                        var options = (StackPanel)((Border)field.Children[1]).Child;
                        Assert.Single(options.Children.Cast<RadioButton>(), option => option.IsChecked == true);
                    }
                }

                StackPanel sensorSurface = sensorSurfaces[kind];
                Assert.True(program.Resources.Contains("PSensorBody"));
                ComboBox profiles = QContract.QContractFind<ComboBox>(sensorSurface, "PSensorProfiles");
                int profileCount = profiles.Items.Count;
                sensor.QSensorBuild(kind, sensorSurface);
                Assert.Equal(profileCount, profiles.Items.Count);
                Assert.DoesNotContain(
                    typeof(QSensor).GetMethods(),
                    method => method.Name == nameof(QSensor.QSensorUpdate)
                        && method.GetParameters().Any(parameter => parameter.ParameterType.Name.StartsWith("Func")));
                foreach ((LDetectorKind stateKind, string stateStep, string stateTitle) in new[]
                         {
                             (LDetectorKind.LDetectorKindBlank, "Blank", "Inspector.Step.Blank"),
                             (LDetectorKind.LDetectorKindScene, "Scene", "Inspector.Step.Scene"),
                             (LDetectorKind.LDetectorKindStill, "Still Image", "Inspector.Step.Still"),
                             (LDetectorKind.LDetectorKindLuminance, "Luminance", "Inspector.Step.Luminance"),
                             (LDetectorKind.LDetectorKindSilence, "Silence", "Inspector.Step.Silence"),
                             (LDetectorKind.LDetectorKindVolume, "Volume", "Inspector.Step.Volume"),
                         })
                {
                    inspector.QInspectorStepSet(stateStep);
                    IReadOnlyDictionary<LDetectorKind, bool> shown = inspector.QInspectorSensorCheck();
                    Assert.Throws<NotSupportedException>(() =>
                        ((IDictionary<LDetectorKind, bool>)shown)[kind] = !shown[kind]);
                    sensor.QSensorUpdate(shown);

                    Assert.True(inspector.QInspectorSensorCheck(stateKind));
                    Assert.False(inspector.QInspectorEmptyShown);
                    Assert.Equal(
                        Interface.TInterface.TLocalizationTextRead(stateTitle), inspector.QInspectorTitleRead());
                    Assert.Equal(Interface.TInterface.TLocalizationTextRead(stateTitle), title.Text);
                    Assert.Equal(Visibility.Collapsed, vacancy.Visibility);
                    Assert.Equal(stateKind == LDetectorKind.LDetectorKindBlank, blank.Visibility == Visibility.Visible);
                    foreach ((LDetectorKind sectionKind, StackPanel section) in sensorSurfaces)
                    {
                        Assert.Equal(sectionKind == stateKind, section.Visibility == Visibility.Visible);
                    }
                }

                CheckBox enablement = QContract.QContractFind<CheckBox>(sensorSurface, "PSensorEnablement");
                int notices = 0;
                sensor.CDetector.CDetectorChanged += () => notices++;

                profiles.SelectedIndex = profiles.SelectedIndex == 0 ? 1 : 0;
                Assert.Equal(1, notices);

                enablement.IsChecked = !TInterface.TSensorEnabledRead(sensor, kind);
                Assert.Equal(enablement.IsChecked, TInterface.TSensorEnabledRead(sensor, kind));
                Assert.Equal(2, notices);

                Grid threshold = QContract.QContractFind<Grid>(sensorSurface, "PSensorThreshold");
                var track = (Slider)threshold.Children[1];
                double changed =
                    TInterface.TSensorValueRead(sensor, kind, QSensorPlan.QSensorPlanThreshold) > track.Minimum
                    ? track.Minimum
                    : track.Maximum;
                track.Value = changed;
                Assert.Equal(changed, TInterface.TSensorValueRead(sensor, kind, QSensorPlan.QSensorPlanThreshold));
                Assert.Equal(3, notices);

                double repainted = changed < track.Maximum ? track.Maximum : track.Minimum;
                inspector.QInspectorGate.CInspectorDetector.CDetectorThresholdSet(kind, repainted);
                Assert.Equal(repainted, track.Value);
                Assert.Equal(4, notices);
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        Assert.Null(failure);
    }
}
