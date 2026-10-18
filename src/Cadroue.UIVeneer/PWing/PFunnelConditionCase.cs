using Cadroue.UIDeportment;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Cadroue.UIVeneer.PWing;

internal sealed class PFunnelConditionCase
{
    private static readonly FontFamily pFunnelMonoFamily = new("Consolas");
    private static readonly Brush pFunnelLineBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7));
    private static readonly Brush pFunnelMutedBrush = new SolidColorBrush(Color.FromRgb(0x8A, 0x93, 0x9E));
    private static readonly Brush pFunnelAccentBrush = new SolidColorBrush(Color.FromRgb(0x2C, 0x6C, 0xCE));

    private static readonly IReadOnlyDictionary<bool, Brush> pFunnelCaseBorders = new Dictionary<bool, Brush>
    {
        [true] = pFunnelAccentBrush,
        [false] = pFunnelLineBrush,
    };

    private static readonly IReadOnlyDictionary<bool, Brush> pFunnelCaseForegrounds = new Dictionary<bool, Brush>
    {
        [true] = pFunnelAccentBrush,
        [false] = pFunnelMutedBrush,
    };

    private static readonly IReadOnlyDictionary<bool, string> pFunnelCaseTexts = new Dictionary<bool, string>
    {
        [true] = "ABC",
        [false] = "abc",
    };

    private static readonly IReadOnlyDictionary<bool, string> pFunnelCaseTooltips = new Dictionary<bool, string>
    {
        [true] = "Inspector.Funnel.CaseOn",
        [false] = "Inspector.Funnel.CaseOff",
    };

    private const double PFunnelFieldHeight = 30;
    private const double PFunnelCaseWidth = 38;

    private readonly QFunnelCriterion lFunnelCriterion;
    private readonly QFunnelRule lFunnelRule;
    private readonly QFunnelKind lFunnelKind;
    private readonly TextBlock pFunnelCaseLabel;

    public PFunnelConditionCase(QFunnelCriterion lCriterion, QFunnelRule lRule, QFunnelKind lKind)
    {
        lFunnelCriterion = lCriterion;
        lFunnelRule = lRule;
        lFunnelKind = lKind;
        pFunnelCaseLabel = PFunnelMonoBuild();
        PFunnelCaseBorder = PFunnelCaseBuild(pFunnelCaseLabel);
    }

    public Border PFunnelCaseBorder { get; }

    public void PFunnelCaseApply(bool pOn)
    {
        PFunnelCaseBorder.BorderBrush = pFunnelCaseBorders[pOn];
        pFunnelCaseLabel.Foreground = pFunnelCaseForegrounds[pOn];
    }

    public void PFunnelCaseShow(bool pOn)
    {
        PFunnelCaseBorder.ToolTip = QLocalizationCatalog.QLocalizationTextRead(pFunnelCaseTooltips[pOn]);
        pFunnelCaseLabel.Text = pFunnelCaseTexts[pOn];
    }

    private static TextBlock PFunnelMonoBuild() => new()
    {
        FontFamily = pFunnelMonoFamily,
        FontSize = 12,
        FontWeight = FontWeights.SemiBold,
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center
    };

    private Border PFunnelCaseBuild(TextBlock pLabel)
    {
        Border pHost = PFunnelCaseCreate(pLabel);
        PFunnelCaseAttach(pHost);
        return pHost;
    }

    private static Border PFunnelCaseCreate(TextBlock pLabel) => new()
    {
        Width = PFunnelCaseWidth,
        Height = PFunnelFieldHeight,
        Margin = new Thickness(6, 0, 0, 0),
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(6),
        Background = Brushes.White,
        Cursor = Cursors.Hand,
        SnapsToDevicePixels = true,
        Child = pLabel
    };

    private void PFunnelCaseAttach(Border pHost) =>
        pHost.MouseLeftButtonUp += (_, _) => lFunnelCriterion.QFunnelCaseToggle(lFunnelRule, lFunnelKind);
}
