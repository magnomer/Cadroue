using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.UIDeportment;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using System.Windows.Media;

using Xunit;

namespace Cadroue.Tests.Windows;

[Collection(nameof(TProgramCollection))]
public sealed class TInspectorPlanApplication
{
    [Fact]
    public void CropRepaint_DoesNotRequestConduct()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                System.Windows.Application program = System.Windows.Application.Current
                    ?? new System.Windows.Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                program.Resources["PToolDimmed"] = 0.5;
                program.Resources["PToolIcon"] = Brushes.Black;
                program.Resources["PIconRoot"] = "pack://application:,,,/";
                foreach (string sheet in new[] { "PCheckbox", "PTextbox", "Button/PButton" })
                {
                    using Stream stream = File.OpenRead(Path.GetFullPath(Path.Combine(
                        AppContext.BaseDirectory, $"../../../../../src/Cadroue.UIVeneer/Kit/{sheet}.xaml")));
                    program.Resources.MergedDictionaries.Add((ResourceDictionary)XamlReader.Load(stream));
                }
                var icons = (System.Collections.Concurrent.ConcurrentDictionary<string, ImageSource>)typeof(QIcon)
                    .GetField("qIconCache", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
                icons[$"pack://application:,,,/PAsset/PPanel/PProcessingCrop.svg|{Brushes.Black}"] = new DrawingImage();
                QInspector inspector = TInterfaceInspector.TInspectorCreate();
                Action<object, string, object> set = (target, name, value) => target.GetType()
                    .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(target, value);
                var tool = new ToggleButton();
                QInspectorCrop crop = inspector.QInspectorCrop;
                set(crop, "qInspectorApplyBox", new CheckBox());
                set(crop, "qInspectorCropPersistent", new CheckBox());
                set(crop, "qInspectorCropStack", new StackPanel());
                set(crop, "qInspectorCropTool", tool);
                set(crop, "qInspectorToolIcon", new Image());
                set(inspector.QInspectorOrientation, "qInspectorOrientationHorizontal", new CheckBox());
                set(inspector.QInspectorOrientation, "qInspectorOrientationVertical", new CheckBox());
                var rotate = new ComboBox();
                rotate.Items.Add("None");
                rotate.Items.Add("90");
                set(inspector.QInspectorOrientation, "qInspectorOrientationRotate", rotate);
                typeof(QInspectorOrientation).GetMethod(
                    "QInspectorRotateAttach", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(inspector.QInspectorOrientation, null);
                set(inspector.QInspectorRatio, "qInspectorRatioCustom", new StackPanel());
                set(inspector.QInspectorRatio, "qInspectorRatioFixed", new CheckBox());
                set(inspector.QInspectorRatio, "qInspectorRatioHeight", new TextBox());
                set(inspector.QInspectorRatio, "qInspectorRatioLenient", new CheckBox());
                set(inspector.QInspectorRatio, "qInspectorRatioNotice", new TextBlock());
                var preset = new ComboBox();
                preset.Items.Add("Custom");
                set(inspector.QInspectorRatio, "qInspectorRatioPreset", preset);
                set(inspector.QInspectorRatio, "qInspectorRatioWidth", new TextBox());
                typeof(QInspectorRatio).GetMethod(
                    "QInspectorRatioAttach", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(inspector.QInspectorRatio, null);
                set(crop.QInspectorGrid, "qInspectorGridBottom", new TextBox());
                var left = new TextBox { Text = "9" };
                set(crop.QInspectorGrid, "qInspectorGridLeft", left);
                set(crop.QInspectorGrid, "qInspectorGridRight", new TextBox());
                set(crop.QInspectorGrid, "qInspectorGridTop", new TextBox());
                set(crop.QInspectorGrid, "qInspectorGridResolution", new TextBlock());
                typeof(QInspectorGrid).GetMethod(
                    "QInspectorGridAttach", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(crop.QInspectorGrid, [crop, new Button()]);
                var port = new TCropPort(inspector);
                inspector.QInspectorGate.CInspectorCrop.CCropAccept(port);
                inspector.QInspectorGate.CInspectorWhitebalance.CWhitebalanceCapableSet(true, true);
                inspector.QInspectorGate.CInspectorWhitebalance.CWhitebalanceToolSet(true, true);
                typeof(Cadroue.Conduct.CInspectorFrame).GetField(
                    "_cInspectorToolArmed", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .SetValue(inspector.QInspectorGate.CInspectorFrame, true);
                int requests = 0;
                inspector.QInspectorGate.CInspectorFrame.CInspectorToolChanged += _ => requests++;
                var window = new Window { Content = left, ShowInTaskbar = false };
                window.Show();
                left.Focus();
                Assert.True(left.IsKeyboardFocusWithin);

                try
                {
                    crop.QInspectorCropAttach();
                    crop.QInspectorCropUpdate();
                }
                catch (IOException) { /* The event path runs before the unavailable Veneer icon is read. */ }

                Assert.True(inspector.QInspectorGate.CInspectorWhitebalance.CWhitebalanceArmed);
                Assert.Equal(0, requests);
                Assert.Equal(0, port.TCropPortRequests);
                try { rotate.SelectedIndex = 1; }
                catch (IOException) { /* The request reaches the port before the unavailable icon repaint. */ }
                Assert.Equal(1, port.TCropPortRequests);
                tool.IsChecked = false;
                tool.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, tool));
                Assert.Equal(1, requests);
                window.Close();
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

    [Fact]
    public void PlanApply_SetsEverySection_RaisesOneVideoNotice()
    {
        QInspector inspector = TInterfaceInspector.TInspectorCreate();
        TInterfaceInspector.TInspectorSourceSet(inspector, 1920, 1080);
        int notices = 0;
        TInterfaceInspector.TInspectorVideoAttach(inspector, () => notices++);
        LWorkVideo video = Interface.TInterface.TWorkVideoCreate(
        [
            Interface.TInterface.TWorkBrightnessCreate(true, 12),
            Interface.TInterface.TWorkContrastCreate(true, 110),
            Interface.TInterface.TWorkExposureCreate(true, 0.5),
            Interface.TInterface.TWorkGammaCreate(true, 20)
        ]);
        LEditPlan plan = Interface.TInterface.TEditPlanCreate(
            Interface.TInterface.TWorkCropCreate(10, 20, 30, 40, 90, true, false), video, true) with
        {
            LEditSkip = true,
            LEditRatioFixed = true,
            LEditRatioLenient = true,
            LEditRatioWidth = 4,
            LEditRatioHeight = 3
        };

        TInterfaceInspector.TInspectorPlanApply(inspector, plan);

        Assert.Equal(1, notices);
        Assert.Equal(plan.LEditCrop, inspector.QInspectorCrop.QInspectorCropbox.LCropboxStateCrop);
        Assert.True(inspector.QInspectorCrop.QInspectorActive);
        Assert.True(inspector.QInspectorCrop.QInspectorRatioFixed);
        Assert.True(inspector.QInspectorCrop.QInspectorRatioLenient);
        Assert.Equal(3, inspector.QInspectorCrop.QInspectorPresetIndex);
        Assert.Equal(12, TInterfaceInspector.TInspectorStepRead(
            inspector, LColorKind.LColorKindBrightness).LWorkStepValue);
        Assert.Equal(110, TInterfaceInspector.TInspectorStepRead(
            inspector, LColorKind.LColorKindContrast).LWorkStepValue);
        Assert.Equal(0.5, TInterfaceInspector.TInspectorStepRead(
            inspector, LColorKind.LColorKindExposure).LWorkStepValue);
        Assert.True(TInterfaceInspector.TInspectorStepRead(inspector, LColorKind.LColorKindGamma).LWorkStepActive);
        Assert.False(TInterfaceInspector.TInspectorStepRead(
            inspector, LColorKind.LColorKindSaturation).LWorkStepActive);
        Assert.True(inspector.QInspectorSkip.QSkipActive);
    }

    [Fact]
    public void VideoApply_RaisesOnce_SingleSetRaisesEach()
    {
        QInspector inspector = TInterfaceInspector.TInspectorCreate();
        int notices = 0;
        TInterfaceInspector.TInspectorVideoAttach(inspector, () => notices++);

        TInterfaceInspector.TInspectorVideoApply(inspector, Interface.TInterface.TWorkVideoCreate(
        [
            Interface.TInterface.TWorkBrightnessCreate(true, 5),
            Interface.TInterface.TWorkContrastCreate(true, 120)
        ]));
        Assert.Equal(1, notices);

        TInterface.TToneValueSet(inspector.QInspectorVideo.QInspectorTone, LColorKind.LColorKindBrightness, 7);
        TInterface.TToneValueSet(inspector.QInspectorVideo.QInspectorTone, LColorKind.LColorKindContrast, 90);

        Assert.Equal(3, notices);
    }

    [Fact]
    public void PersistentChange_RaisesOncePerFlip_NotOnValues()
    {
        QInspector inspector = TInterfaceInspector.TInspectorCreate();
        int notices = 0;
        TInterfaceInspector.TInspectorPersistentAttach(inspector, () => notices++);

        TInterface.TToneValueSet(inspector.QInspectorVideo.QInspectorTone, LColorKind.LColorKindBrightness, 7);
        Assert.Equal(0, notices);

        TInterfaceInspector.TInspectorPersistentSet(inspector, LColorKind.LColorKindBrightness, true);
        TInterfaceInspector.TInspectorPersistentSet(inspector, LColorKind.LColorKindBrightness, true);
        TInterface.TSkipPersistentSet(inspector.QInspectorSkip, true);
        TInterfaceCrop.TCropboxPersistentSet(inspector.QInspectorCrop.QInspectorCropbox, true);
        TInterface.TVolumePersistentSet(inspector.QInspectorAudio.QInspectorVolume, true);

        Assert.Equal(4, notices);
    }

    [Fact]
    public void AudioApply_RaisesOneAudioNotice()
    {
        QInspector inspector = TInterfaceInspector.TInspectorCreate();
        QInspectorFrame frame = inspector.QInspectorFrame;
        int notices = 0;
        inspector.QInspectorAudio.QInspectorAudioChanged += () => notices++;

        LWorkAudioStep volume = Interface.TInterface.TWorkVolumeCreate(true, 3);
        TInterfaceInspector.TInspectorAudioApply(
            inspector.QInspectorAudio, Interface.TInterface.TWorkAudioCreate([volume], false));

        Assert.Equal(1, notices);
        Assert.Equal(3, inspector.QInspectorAudio.QInspectorVolume.CVolumeGain);
        frame.QInspectorFrameSet("Normalize");
        Assert.True(TInterfaceInspector.TInspectorSectionCheck(inspector, "Normalize"));
        Assert.Equal(1, notices);
    }

    [Fact]
    public void Section_Shown_Title_FollowStep()
    {
        QInspector inspector = TInterfaceInspector.TInspectorCreate();

        inspector.QInspectorFrame.QInspectorFrameSet("Noise Reduction");

        Assert.True(TInterfaceInspector.TInspectorSectionCheck(inspector, "Noise Reduction"));
        Assert.False(TInterfaceInspector.TInspectorSectionCheck(inspector, "Crop"));
        Assert.True(inspector.QInspectorPersistentShown);
        Assert.False(inspector.QInspectorEmptyShown);
        Assert.Equal(
            Interface.TInterface.TLocalizationTextRead("Inspector.Step.NoiseReduction"),
            TInterfaceInspector.TInspectorTitleRead(inspector));

        inspector.QInspectorFrame.QInspectorFrameSet("Nowhere");

        Assert.False(inspector.QInspectorPersistentShown);
        Assert.True(inspector.QInspectorEmptyShown);
        string title = Interface.TInterface.TLocalizationTextRead("Inspector.Header.Title");
        Assert.Equal(title, TInterfaceInspector.TInspectorTitleRead(inspector));
    }

    [Fact]
    public void StepSet_AwayFromCrop_DisarmsTool_RaisesToolNotice()
    {
        QInspector inspector = TInterfaceInspector.TInspectorCreate();
        var armed = new List<bool>();
        inspector.QInspectorToolChanged += armed.Add;
        TInterfaceInspector.TInspectorStepSet(inspector, "Crop");
        TInterfaceInspector.TInspectorToolSet(inspector, true);

        TInterfaceInspector.TInspectorStepSet(inspector, "Gamma");

        Assert.False(inspector.QInspectorToolArmed);
        Assert.Equal(new[] { true, false }, armed);
    }

    [Fact]
    public void ToolSet_FalseWhileWhitebalanceArmed_KeepsWhitebalance()
    {
        QInspector inspector = TInterfaceInspector.TInspectorCreate();
        inspector.QInspectorGate.CInspectorWhitebalance.CWhitebalanceCapableSet(true, true);
        inspector.QInspectorGate.CInspectorWhitebalance.CWhitebalanceToolSet(true, true);

        inspector.QInspectorGate.CInspectorFrame.CInspectorToolSet(false);

        Assert.True(inspector.QInspectorGate.CInspectorWhitebalance.CWhitebalanceArmed);
        Assert.False(inspector.QInspectorGate.CInspectorFrame.CInspectorToolArmed);
    }

    [Fact]
    public void ToolSet_RepeatedRequests_AreSilent()
    {
        QInspector inspector = TInterfaceInspector.TInspectorCreate();
        int changes = 0;
        int tools = 0;
        inspector.QInspectorGate.CInspectorFrame.CInspectorChanged += () => changes++;
        inspector.QInspectorGate.CInspectorFrame.CInspectorToolChanged += _ => tools++;

        inspector.QInspectorGate.CInspectorFrame.CInspectorToolSet(true);
        inspector.QInspectorGate.CInspectorFrame.CInspectorToolSet(true);
        inspector.QInspectorGate.CInspectorFrame.CInspectorToolSet(false);
        inspector.QInspectorGate.CInspectorFrame.CInspectorToolSet(false);

        Assert.Equal(2, changes);
        Assert.Equal(2, tools);
    }

    [Fact]
    public void TipResolve_DisabledThenPreview_ThenApplyKeys()
    {
        QInspector inspector = TInterfaceInspector.TInspectorCreate();

        QInspectorTip disabled = TInterfaceInspector.TInspectorTipResolve(
            inspector, true, false, true, "Disabled", "Preview");
        QInspectorTip preview = TInterfaceInspector.TInspectorTipResolve(
            inspector, true, true, false, "Disabled", "Preview");
        QInspectorTip plain = TInterfaceInspector.TInspectorTipResolve(
            inspector, true, true, true, "Disabled", "Preview");
        CInspectorTip disabledPlan = CInspectorTip.CInspectorTipResolve(
            true, false, false, "Disabled", "Preview");
        CInspectorTip previewPlan = CInspectorTip.CInspectorTipResolve(
            true, true, false, "Disabled", "Preview");
        CInspectorTip plainPlan = CInspectorTip.CInspectorTipResolve(
            true, true, true, "Disabled", "Preview");

        Assert.False(disabled.QInspectorTipEnabled);
        Assert.Equal(Interface.TInterface.TLocalizationTextRead("Disabled"), disabled.QInspectorTipBox);
        Assert.True(preview.QInspectorTipEnabled);
        Assert.Equal(Interface.TInterface.TLocalizationTextRead("Preview"), preview.QInspectorTipNotice);
        Assert.Null(plain.QInspectorTipNotice);
        Assert.Equal(Interface.TInterface.TLocalizationTextRead("Inspector.Common.Apply"), plain.QInspectorTipBox);
        Assert.Equal("Disabled", disabledPlan.CInspectorNotice?.CInspectorNoticeKey);
        Assert.Equal("Preview", previewPlan.CInspectorNotice?.CInspectorNoticeKey);
        Assert.Null(plainPlan.CInspectorNotice);
    }
}
