using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.ShellEngine;

namespace Cadroue.UIDeportment;

// The crop section's wiring: shows the crop gate's answers and hands every input to it. The record stays here for the
// owners that still read it; edits that carry a crop record go to its outlet, which the gate also reaches.
public sealed class QInspectorCrop
{
    private const string QInspectorCropIcon = "/PAsset/PPanel/PProcessingCrop.svg";

    private static readonly IReadOnlyDictionary<string, Brush> qInspectorCropFaces =
        new Dictionary<string, Brush>
        {
            ["Active"] = new SolidColorBrush(Color.FromRgb(0xD3, 0xE3, 0xFA)),
            ["Armed"] = new SolidColorBrush(Color.FromRgb(0xDD, 0xE3, 0xEC)),
            ["Idle"] = Brushes.Transparent
        };

    private readonly QInspector qInspector;
    private readonly CCrop qInspectorCropGate;
    private readonly LCropOutlet qInspectorCropOutlet;
    private CheckBox qInspectorApplyBox = null!;
    private CheckBox qInspectorCropPersistent = null!;
    private StackPanel qInspectorCropStack = null!;
    private ToggleButton qInspectorCropTool = null!;
    private Image qInspectorToolIcon = null!;

    public QInspectorCrop(QInspector lOwner)
    {
        qInspector = lOwner;
        qInspectorCropGate = lOwner.QInspectorGate.CInspectorCrop;
        qInspectorCropOutlet = new LCropOutlet(
            QInspectorCropbox,
            () => (lOwner.QInspectorSourcePresent, lOwner.QInspectorSourceWidth, lOwner.QInspectorSourceHeight));
        QInspectorGrid = new QInspectorGrid();
        qInspectorCropGate.CCropAccept(qInspectorCropOutlet);
    }

    public LCropboxState QInspectorCropbox { get; } = new();

    public QInspectorGrid QInspectorGrid { get; }

    public StackPanel PInspectorCropBody { get; private set; } = null!;

    public CheckBox PInspectorPersistentBox { get; private set; } = null!;

    public bool QInspectorActive => qInspectorCropGate.CCropActive;

    public bool QInspectorPersistent => qInspectorCropGate.CCropPersistent;

    public bool QInspectorRatioFixed => qInspectorCropGate.CCropRatioFixed;

    public bool QInspectorRatioLenient => qInspectorCropGate.CCropRatioLenient;

    public int QInspectorPresetIndex => qInspectorCropGate.CCropPresetIndex;

    public bool QInspectorCustomShown => qInspectorCropGate.CCropCustomShown;

    public bool QInspectorCropEnabled => QInspectorActive && qInspector.QInspectorCropCapable;

    public string QInspectorApplyTip => QLocalizationCatalog.QLocalizationTextRead(
        qInspector.QInspectorCropCapable ? "Inspector.Crop.ApplyTooltip" : "Inspector.Crop.RequiresCrop");

    public bool QInspectorToolActive => qInspector.QInspectorToolArmed && qInspectorCropGate.CCropRectShown;

    public string QInspectorToolKey =>
        QInspectorToolActive ? "Active" : qInspector.QInspectorToolArmed ? "Armed" : "Idle";

    public bool QInspectorNoticeShown => QInspectorNoticeResolve() is not null;

    public string QInspectorNoticeRead() => QInspectorNoticeResolve() ?? string.Empty;

    public void QInspectorCropBuild()
    {
        PInspectorCropBody = QContract.QContractSheetFind<StackPanel>("PInspectorCropBody");
        PInspectorPersistentBox = QContract.QContractSheetFind<CheckBox>("PInspectorCropPersistent");
        qInspectorApplyBox =
            QContract.QContractFind<CheckBox>(PInspectorCropBody, "PInspectorApplyBox");
        qInspectorCropPersistent = PInspectorPersistentBox;
        qInspectorCropStack =
            QContract.QContractFind<StackPanel>(PInspectorCropBody, "PInspectorCropStack");
        qInspectorCropTool =
            QContract.QContractFind<ToggleButton>(PInspectorCropBody, "PInspectorCropTool");
        qInspectorToolIcon =
            QContract.QContractFind<Image>(PInspectorCropBody, "PInspectorToolIcon");

        qInspector.QInspectorOrientation.QInspectorOrientationBuild();
        QInspectorGrid.QInspectorGridBuild(this);
        qInspector.QInspectorRatio.QInspectorRatioBuild();
        QContract.QContractFind<ContentPresenter>(PInspectorCropBody, "PInspectorCropOrientation").Content =
            qInspector.QInspectorOrientation.QInspectorOrientationBody;
        QContract.QContractFind<ContentPresenter>(PInspectorCropBody, "PInspectorCropGrid").Content =
            QInspectorGrid.PInspectorGridBody;
        QContract.QContractFind<ContentPresenter>(PInspectorCropBody, "PInspectorCropRatio").Content =
            qInspector.QInspectorRatio.QInspectorRatioBody;

        qInspectorApplyBox.Content = QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Apply");
        qInspectorCropPersistent.Content =
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Persistent");
        qInspectorCropPersistent.ToolTip =
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Crop.PersistentTooltip");
        QContract.QContractFind<TextBlock>(PInspectorCropBody, "PInspectorToolLabel").Text =
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Crop.Tool");
        qInspectorCropTool.ToolTip = QLocalizationCatalog.QLocalizationTextRead("Inspector.Crop.DrawTooltip");

        var lTemplate = new ControlTemplate(typeof(ToggleButton));
        var lBorder = new FrameworkElementFactory(typeof(Border));
        lBorder.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
        lBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
        var lContent = new FrameworkElementFactory(typeof(ContentPresenter));
        lContent.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        lContent.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        lBorder.AppendChild(lContent);
        lTemplate.VisualTree = lBorder;
        var lStyle = new Style(typeof(ToggleButton));
        lStyle.Setters.Add(new Setter(FrameworkElement.FocusVisualStyleProperty, null));
        lStyle.Setters.Add(new Setter(FrameworkElement.CursorProperty, System.Windows.Input.Cursors.Hand));
        lStyle.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
        lStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
        lStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(0)));
        lStyle.Setters.Add(new Setter(Control.TemplateProperty, lTemplate));
        qInspectorCropTool.Style = lStyle;
    }

    public void QInspectorCropAttach()
    {
        if (!qInspectorCropStack.Resources.Contains(typeof(QInspectorCrop)))
        {
            RoutedEventHandler lTool = (_, _) => QInspectorCropHandle();
            Action lAttach = () =>
            {
                qInspectorCropTool.Click -= lTool;
                qInspectorCropTool.Click += lTool;
            };
            Action lDetach = () => qInspectorCropTool.Click -= lTool;
            lAttach();
            qInspectorCropStack.Resources[typeof(QInspectorCrop)] = new List<(Action, Action)>
            {
                QTool.QToolSwitchAttach(qInspectorApplyBox, QInspectorApplySet),
                QTool.QToolSwitchAttach(qInspectorCropPersistent, QInspectorPersistentSet),
                (lAttach, lDetach),
            };
        }

        foreach ((Action lAttach, _) in
                 (List<(Action, Action)>)qInspectorCropStack.Resources[typeof(QInspectorCrop)])
        {
            lAttach();
        }

        if (qInspector.QInspectorOrientation.QInspectorOrientationBody is not null)
            qInspector.QInspectorOrientation.QInspectorOrientationBuild(true);
        if (QInspectorGrid.PInspectorGridBody is not null)
            QInspectorGrid.QInspectorGridBuild(true);
        if (qInspector.QInspectorRatio.QInspectorRatioBody is not null)
            qInspector.QInspectorRatio.QInspectorRatioBuild(true);

        QInspectorCropbox.LCropboxStateChanged -= QInspectorCropUpdate;
        QInspectorCropbox.LCropboxStateChanged += QInspectorCropUpdate;
        QInspectorCropUpdate();
    }

    public void QInspectorCropDetach()
    {
        QInspectorCropbox.LCropboxStateChanged -= QInspectorCropUpdate;
        if (qInspector.QInspectorOrientation.QInspectorOrientationBody is not null)
            qInspector.QInspectorOrientation.QInspectorOrientationBuild(false);
        if (QInspectorGrid.PInspectorGridBody is not null)
            QInspectorGrid.QInspectorGridBuild(false);
        if (qInspector.QInspectorRatio.QInspectorRatioBody is not null)
            qInspector.QInspectorRatio.QInspectorRatioBuild(false);
        foreach ((_, Action lDetach) in
                 (List<(Action, Action)>)qInspectorCropStack.Resources[typeof(QInspectorCrop)])
        {
            lDetach();
        }
    }

    public void QInspectorCropUpdate()
    {
        QTool.QToolSwitchUpdate(qInspectorApplyBox, QInspectorActive);
        QTool.QToolSwitchUpdate(qInspectorCropPersistent, QInspectorPersistent);
        QTool.QToolStackUpdate(qInspectorCropStack, QInspectorCropEnabled);
        qInspector.QInspectorOrientation.QInspectorOrientationUpdate(qInspector.QInspectorOrientationCapable);
        qInspectorApplyBox.IsEnabled = qInspector.QInspectorCropCapable;
        qInspectorApplyBox.ToolTip = QInspectorApplyTip;
        qInspector.QInspectorRatio.QInspectorRatioUpdate();
        QInspectorGrid.QInspectorGridUpdate(this);
        qInspectorCropTool.IsChecked = qInspector.QInspectorToolArmed;
        qInspectorCropTool.Background = qInspectorCropFaces[QInspectorToolKey];
        qInspectorToolIcon.Source = QIcon.QIconRead(
            QInspectorCropIcon, QTool.QToolPickRead(QInspectorToolActive));
    }

    public LWorkCrop QInspectorCropRead() => qInspectorCropOutlet.LCropWorkRead();

    public LCropbox? QInspectorRectRead() => qInspectorCropOutlet.LCropRectRead();

    public string QInspectorResolutionRead() => qInspectorCropGate.CCropResolution is { } lSize
        ? $"{lSize.CCropWidth.ToString(CultureInfo.InvariantCulture)} × "
            + $"{lSize.CCropHeight.ToString(CultureInfo.InvariantCulture)}"
        : "—";

    public string QInspectorEdgeFormat(int lSide, string lText) =>
        QInspectorField.QInspectorWholeFormat(lText, qInspectorCropGate.CCropEdgeRead(lSide));

    public void QInspectorEdgeCommit(int lSide, string lText, bool lTyped) =>
        qInspectorCropGate.CCropEdgeCommit(lSide, lText, lTyped);

    public void QInspectorEdgeReset() => qInspectorCropGate.CCropEdgeReset();

    public void QInspectorApplySet(bool lApply) => qInspectorCropGate.CCropApplySet(lApply);

    public void QInspectorPersistentSet(bool lPersistent) => qInspectorCropGate.CCropPersistentSet(lPersistent);

    public void QInspectorCropSet(LCropbox? lDrawn, int lDriveAxis, int lAnchorX, int lAnchorY) =>
        qInspectorCropOutlet.LCropDrawnSet(lDrawn, lDriveAxis, lAnchorX, lAnchorY);

    public void QInspectorCropApply(LWorkCrop lCrop, bool lApply) => qInspectorCropOutlet.LCropWorkApply(lCrop, lApply);

    // A reset crop disarms the crop tool; a persistent one is left as it is.
    public void QInspectorCropReset() => qInspector.QInspectorGate.CInspectorFrame.CInspectorCropReset();

    private void QInspectorCropHandle()
    {
        qInspector.QInspectorToolSet(qInspectorCropTool.IsChecked == true);
    }

    private string? QInspectorNoticeResolve() => QInspectorNotice.QInspectorNoticeResolve(
        CInspectorNotice.CInspectorNoticeResolve(qInspectorCropGate.CCropNotice));
}
