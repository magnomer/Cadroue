using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed class PInspectorCrop
{
    private const string PCropIcon = "/PAsset/PPanel/PProcessingCrop.svg";

    private static readonly Brush pInspectorActiveBrush = new SolidColorBrush(Color.FromRgb(0xD3, 0xE3, 0xFA));
    private static readonly Brush pInspectorArmedBrush = new SolidColorBrush(Color.FromRgb(0xDD, 0xE3, 0xEC));

    private static readonly IReadOnlyDictionary<string, Brush> pInspectorToolFaces = new Dictionary<string, Brush>
    {
        ["Active"] = pInspectorActiveBrush,
        ["Armed"] = pInspectorArmedBrush,
        ["Idle"] = Brushes.Transparent
    };

    private readonly Action pInspectorCancelHandler;
    private readonly PInspectorCropGrid pInspectorCropGrid;
    private readonly PInspectorRatio pInspectorRatio;
    private readonly PInspectorOrientation pInspectorOrientation;
    private readonly Image pInspectorToolIcon;
    private readonly ToggleButton pInspectorCropTool;
    private readonly CheckBox pInspectorApplyBox;
    private readonly StackPanel pInspectorCropStack;

    // The crop tool cancels the other picker tools before it arms; the owner of those tools hands that step in.
    internal PInspectorCrop(QInspector lInspector, Action pToolCancel)
    {
        QInspector = lInspector;
        pInspectorCancelHandler = pToolCancel;
        pInspectorCropGrid = new PInspectorCropGrid(QInspectorCrop);
        pInspectorRatio = new PInspectorRatio(QInspector.QInspectorRatio, QInspectorCrop);
        pInspectorOrientation = new PInspectorOrientation(QInspector.QInspectorOrientation);
        pInspectorToolIcon = new Image
        {
            Width = 18,
            Height = 18,
            Source = QIcon.QIconRead(PCropIcon, PInspectorLook.PInspectorIconBrush),
            Stretch = Stretch.Uniform
        };
        pInspectorCropTool = PInspectorToolBuild();

        pInspectorApplyBox = PInspectorSwitch.PInspectorSwitchBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Apply"),
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Crop.ApplyTooltip"));
        PInspectorPersistentBox = PInspectorSwitch.PInspectorSwitchBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Persistent"),
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Crop.PersistentTooltip"));

        pInspectorCropStack = new StackPanel();
        pInspectorCropStack.Children.Add(
            PInspectorField.PInspectorFieldBuild(QLocalizationCatalog.QLocalizationTextRead("Inspector.Crop.Tool"),
                pInspectorCropTool));
        pInspectorCropStack.Children.Add(pInspectorOrientation.PInspectorFlipField);
        pInspectorCropStack.Children.Add(pInspectorOrientation.PInspectorRotateField);
        pInspectorCropStack.Children.Add(pInspectorCropGrid.PInspectorGridBody);
        pInspectorCropStack.Children.Add(pInspectorRatio.PInspectorRatioBody);
        pInspectorCropStack.Children.Add(pInspectorRatio.PInspectorRatioFixed);
        pInspectorCropStack.Children.Add(pInspectorRatio.PInspectorRatioLenient);
        pInspectorCropStack.Children.Add(pInspectorRatio.PInspectorRatioNotice);

        PInspectorCropBody = new StackPanel
        {
            Margin = new Thickness(12, 12, 12, 12),
            Visibility = Visibility.Collapsed
        };
        PInspectorCropBody.Children.Add(pInspectorApplyBox);
        PInspectorCropBody.Children.Add(PInspectorField.PInspectorSeparatorBuild());
        PInspectorCropBody.Children.Add(pInspectorCropStack);

        PInspectorSwitch.PInspectorSwitchAttach(pInspectorApplyBox, QInspectorCrop.QInspectorApplySet);
        PInspectorSwitch.PInspectorSwitchAttach(PInspectorPersistentBox, QInspectorCrop.QInspectorPersistentSet);
    }

    private QInspector QInspector { get; }

    private QInspectorCrop QInspectorCrop => QInspector.QInspectorCrop;

    internal StackPanel PInspectorCropBody { get; }

    internal CheckBox PInspectorPersistentBox { get; }

    internal void PInspectorCropAttach()
    {
        PCropChangeAttach();
        PInspectorCropUpdate();
    }

    private void PCropChangeAttach() =>
        QInspectorCrop.QInspectorCropbox.LCropboxStateChanged += PInspectorCropUpdate;

    internal void PInspectorCropUpdate()
    {
        PCropStateApply();
        PInspectorState.PInspectorSectionUpdate(pInspectorCropStack, QInspectorCrop.QInspectorCropEnabled);
        pInspectorRatio.PInspectorRatioUpdate();
        pInspectorCropGrid.PInspectorResolutionApply();
        PInspectorToolUpdate();
    }

    private void PCropStateApply()
    {
        QInspectorCrop lCrop = QInspectorCrop;
        pInspectorApplyBox.IsChecked = PLook.PLookChecked[lCrop.QInspectorActive];
        PInspectorPersistentBox.IsChecked = PLook.PLookChecked[lCrop.QInspectorPersistent];
        pInspectorCropGrid.PInspectorGridApply();
        pInspectorOrientation.PInspectorOrientationApply(QInspector.QInspectorOrientationCapable);
        pInspectorApplyBox.IsEnabled = QInspector.QInspectorCropCapable;
        pInspectorApplyBox.ToolTip = lCrop.QInspectorApplyTip;
    }

    private ToggleButton PInspectorToolBuild()
    {
        ToggleButton pToolButton = PCropToolBuild();
        PCropToolAttach(pToolButton);
        return pToolButton;
    }

    private ToggleButton PCropToolBuild() => new()
    {
        Content = pInspectorToolIcon,
        ToolTip = QLocalizationCatalog.QLocalizationTextRead("Inspector.Crop.DrawTooltip"),
        Width = 32,
        Height = 32,
        VerticalAlignment = VerticalAlignment.Center,
        Style = PInspectorTool.PInspectorToolCreate()
    };

    private void PCropToolAttach(ToggleButton pToolButton)
    {
        pToolButton.Checked += (_, _) => PCropToolHandle();
        pToolButton.Unchecked += (_, _) => QInspector.QInspectorToolSet(false);
    }

    private void PCropToolHandle()
    {
        pInspectorCancelHandler();
        QInspector.QInspectorToolSet(true);
    }

    private void PInspectorToolUpdate()
    {
        PCropToolApply();
        PCropFaceApply();
    }

    private void PCropToolApply() =>
        pInspectorCropTool.IsChecked = PLook.PLookChecked[QInspector.QInspectorToolArmed];

    private void PCropFaceApply()
    {
        QInspectorCrop lCrop = QInspectorCrop;
        pInspectorCropTool.Background = pInspectorToolFaces[lCrop.QInspectorToolKey];
        pInspectorToolIcon.Source = QIcon.QIconRead(
            PCropIcon, PInspectorLook.PInspectorPickBrush[lCrop.QInspectorToolActive]);
    }
}
