using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Cadroue.UIDeportment;

// The neutral picker wiring: pulls the neutral surface, relays both targets and paints their completed state.
public sealed class QWhitebalanceNeutral
{
    private static readonly IReadOnlyDictionary<bool, Visibility> qWhitebalanceNeutralVisibility =
        new Dictionary<bool, Visibility>
        {
            [true] = Visibility.Visible,
            [false] = Visibility.Collapsed
        };

    private const string QWhitebalanceNeutralSheet = "PWhitebalanceNeutral";
    private const string QWhitebalanceNeutralLabel = "PWhitebalanceNeutralLabel";
    private const string QWhitebalanceNeutralTool = "PWhitebalanceNeutralTool";
    private const string QWhitebalanceWhiteTool = "PWhitebalanceWhiteTool";
    private const string QWhitebalanceNeutralIcon = "PWhitebalanceNeutralIcon";
    private const string QWhitebalanceWhiteIcon = "PWhitebalanceWhiteIcon";
    private const string QWhitebalanceNeutralGroup = "PWhitebalanceNeutralGroup";
    private const string QWhitebalanceNeutralSwatch = "PWhitebalanceNeutralSwatch";
    private const string QWhitebalanceNeutralReadout = "PWhitebalanceNeutralReadout";
    private const string QWhitebalanceNeutralStatus = "PWhitebalanceNeutralStatus";
    private const string QWhitebalanceNeutralInk = "PWhitebalanceNeutralInk";
    private const string QWhitebalanceNeutralAccent = "PWhitebalanceNeutralAccent";
    private const string QWhitebalancePickerIcon = "/PAsset/PPanel/PProcessingPicker.svg";

    private readonly ToggleButton qWhitebalanceNeutralTool;
    private readonly ToggleButton qWhitebalanceWhiteTool;
    private readonly Image qWhitebalanceNeutralIcon;
    private readonly Image qWhitebalanceWhiteIcon;
    private readonly StackPanel qWhitebalanceNeutralGroup;
    private readonly Rectangle qWhitebalanceNeutralSwatch;
    private readonly TextBlock qWhitebalanceNeutralReadout;
    private readonly TextBlock qWhitebalanceNeutralStatus;
    private readonly Brush qWhitebalanceNeutralInk;
    private readonly Brush qWhitebalanceNeutralAccent;

    public QWhitebalanceNeutral()
    {
        QWhitebalanceNeutralBody = QContract.QContractSheetFind<StackPanel>(QWhitebalanceNeutralSheet);
        qWhitebalanceNeutralTool = QContract.QContractFind<ToggleButton>(
            QWhitebalanceNeutralBody, QWhitebalanceNeutralTool);
        qWhitebalanceWhiteTool = QContract.QContractFind<ToggleButton>(
            QWhitebalanceNeutralBody, QWhitebalanceWhiteTool);
        var lTemplate = new ControlTemplate(typeof(ToggleButton));
        var lBorder = new FrameworkElementFactory(typeof(Border));
        lBorder.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
        lBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
        var lContent = new FrameworkElementFactory(typeof(ContentPresenter));
        lContent.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        lContent.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        lBorder.AppendChild(lContent);
        lTemplate.VisualTree = lBorder;
        qWhitebalanceNeutralTool.Template = lTemplate;
        qWhitebalanceWhiteTool.Template = lTemplate;
        qWhitebalanceNeutralIcon = QContract.QContractFind<Image>(
            QWhitebalanceNeutralBody, QWhitebalanceNeutralIcon);
        qWhitebalanceWhiteIcon = QContract.QContractFind<Image>(
            QWhitebalanceNeutralBody, QWhitebalanceWhiteIcon);
        qWhitebalanceNeutralGroup = QContract.QContractFind<StackPanel>(
            QWhitebalanceNeutralBody, QWhitebalanceNeutralGroup);
        qWhitebalanceNeutralSwatch = QContract.QContractFind<Rectangle>(
            QWhitebalanceNeutralBody, QWhitebalanceNeutralSwatch);
        qWhitebalanceNeutralReadout = QContract.QContractFind<TextBlock>(
            QWhitebalanceNeutralBody, QWhitebalanceNeutralReadout);
        qWhitebalanceNeutralStatus = QContract.QContractFind<TextBlock>(
            QWhitebalanceNeutralBody, QWhitebalanceNeutralStatus);
        qWhitebalanceNeutralInk = QContract.QContractSheetFind<Brush>(
            QWhitebalanceNeutralBody, QWhitebalanceNeutralInk);
        qWhitebalanceNeutralAccent = QContract.QContractSheetFind<Brush>(
            QWhitebalanceNeutralBody, QWhitebalanceNeutralAccent);

        QContract.QContractFind<TextBlock>(QWhitebalanceNeutralBody, QWhitebalanceNeutralLabel).Text =
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Video.WhitebalancePicker");
        qWhitebalanceNeutralTool.ToolTip =
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Video.WhitebalancePickTooltip");
        qWhitebalanceWhiteTool.ToolTip =
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Video.WhitebalancePickWhiteTooltip");
        qWhitebalanceNeutralIcon.Source = QIcon.QIconRead(QWhitebalancePickerIcon, qWhitebalanceNeutralInk);
        qWhitebalanceWhiteIcon.Source = QIcon.QIconRead(QWhitebalancePickerIcon, qWhitebalanceNeutralInk);
    }

    public StackPanel QWhitebalanceNeutralBody { get; }

    public void QWhitebalanceNeutralAttach(QWhitebalance lWhitebalance)
    {
        QWhitebalanceNeutralAttach(qWhitebalanceNeutralTool, false, lWhitebalance.QWhitebalanceToolToggle);
        QWhitebalanceNeutralAttach(qWhitebalanceWhiteTool, true, lWhitebalance.QWhitebalanceToolToggle);
    }

    public void QWhitebalanceNeutralUpdate(QWhitebalance lWhitebalance)
    {
        QInspectorTip lTip = lWhitebalance.QWhitebalanceTip;
        qWhitebalanceNeutralTool.IsEnabled = lWhitebalance.QWhitebalanceCapable;
        qWhitebalanceNeutralTool.ToolTip =
            lTip.QInspectorNoticeRead("Inspector.Video.WhitebalancePickTooltip");
        qWhitebalanceWhiteTool.IsEnabled = lWhitebalance.QWhitebalanceCapable;
        qWhitebalanceWhiteTool.ToolTip =
            lTip.QInspectorNoticeRead("Inspector.Video.WhitebalancePickWhiteTooltip");
        ToolTipService.SetShowOnDisabled(qWhitebalanceNeutralTool, true);
        ToolTipService.SetShowOnDisabled(qWhitebalanceWhiteTool, true);

        qWhitebalanceNeutralTool.IsChecked = lWhitebalance.QWhitebalanceGreyArmed;
        qWhitebalanceWhiteTool.IsChecked = lWhitebalance.QWhitebalanceWhiteArmed;
        qWhitebalanceNeutralIcon.Source = QIcon.QIconRead(
            QWhitebalancePickerIcon,
            lWhitebalance.QWhitebalanceGreyArmed ? qWhitebalanceNeutralAccent : qWhitebalanceNeutralInk);
        qWhitebalanceWhiteIcon.Source = QIcon.QIconRead(
            QWhitebalancePickerIcon,
            lWhitebalance.QWhitebalanceWhiteArmed ? qWhitebalanceNeutralAccent : qWhitebalanceNeutralInk);
        qWhitebalanceNeutralStatus.Text = lWhitebalance.QWhitebalanceGuideRead();
        qWhitebalanceNeutralStatus.Visibility = lWhitebalance.QWhitebalanceGuideShown
            ? Visibility.Visible
            : Visibility.Collapsed;

        QWhitebalanceReadout lReadout = lWhitebalance.QWhitebalanceReadoutRead();
        qWhitebalanceNeutralGroup.Visibility =
            qWhitebalanceNeutralVisibility[lReadout.QWhitebalanceReadoutSampled];
        qWhitebalanceNeutralSwatch.Fill = new SolidColorBrush(Color.FromRgb(
            lReadout.QWhitebalanceReadoutRed,
            lReadout.QWhitebalanceReadoutGreen,
            lReadout.QWhitebalanceReadoutBlue));
        qWhitebalanceNeutralReadout.Text = lReadout.QWhitebalanceReadoutText;
    }

    private static void QWhitebalanceNeutralAttach(
        ToggleButton lTool,
        bool lWhite,
        Action<bool, bool> lSet)
    {
        lTool.Checked += (_, _) => lSet(lWhite, true);
        lTool.Unchecked += (_, _) => lSet(lWhite, false);
    }

}
