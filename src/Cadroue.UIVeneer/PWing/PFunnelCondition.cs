using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PWing;

internal sealed class PFunnelCondition : Grid
{
    private static readonly FontFamily pFunnelFontFamily = new("Segoe UI");
    private static readonly FontFamily pFunnelMonoFamily = new("Consolas");
    private static readonly Brush pFunnelLineBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7));
    private static readonly Brush pFunnelTitleBrush = new SolidColorBrush(Color.FromRgb(0x26, 0x36, 0x4A));
    private static readonly Brush pFunnelMutedBrush = new SolidColorBrush(Color.FromRgb(0x8A, 0x93, 0x9E));
    private static readonly Brush pFunnelAccentBrush = new SolidColorBrush(Color.FromRgb(0x2C, 0x6C, 0xCE));
    private static readonly Brush pFunnelActiveBrush = new SolidColorBrush(Color.FromRgb(0xCE, 0xE1, 0xFB));

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

    private static readonly IReadOnlyDictionary<bool, FontWeight> pFunnelSegmentWeights =
        new Dictionary<bool, FontWeight>
        {
            [true] = FontWeights.SemiBold,
            [false] = FontWeights.Normal,
        };

    private static readonly IReadOnlyDictionary<bool, Brush> pFunnelSegmentForegrounds = new Dictionary<bool, Brush>
    {
        [true] = pFunnelTitleBrush,
        [false] = pFunnelMutedBrush,
    };

    private static readonly IReadOnlyDictionary<bool, Brush> pFunnelSegmentBackgrounds = new Dictionary<bool, Brush>
    {
        [true] = pFunnelActiveBrush,
        [false] = Brushes.Transparent,
    };

    private const double PFunnelFieldHeight = 30;
    private const double PFunnelJoinWidth = 78;
    private const double PFunnelCaseWidth = 38;

    private readonly LFunnel lFunnel;
    private readonly LFunnelRule lFunnelRule;
    private readonly LFunnelKind lFunnelKind;
    private readonly TextBox pFunnelField;
    private readonly TextBlock pFunnelCaseLabel;
    private readonly Border pFunnelCaseButton;
    private readonly Border pFunnelJoin;
    private readonly TextBlock pFunnelAndLabel;
    private readonly Border pFunnelAndSegment;
    private readonly TextBlock pFunnelOrLabel;
    private readonly Border pFunnelOrSegment;

    public PFunnelCondition(LFunnel lFunnelOwner, LFunnelRule lRule, LFunnelCondition lCondition)
    {
        lFunnel = lFunnelOwner;
        lFunnelRule = lRule;
        lFunnelKind = lCondition.LFunnelConditionKind;
        pFunnelField = PFunnelFieldBuild();
        pFunnelCaseLabel = PFunnelMonoBuild();
        pFunnelCaseButton = PFunnelCaseBuild(pFunnelCaseLabel);
        pFunnelAndLabel = PFunnelLabelBuild(LLocalization.LLocalizationTextRead("Inspector.Funnel.And"));
        pFunnelOrLabel = PFunnelLabelBuild(LLocalization.LLocalizationTextRead("Inspector.Funnel.Or"));
        pFunnelAndSegment = PFunnelSegmentBuild(pFunnelAndLabel, () => PFunnelModeSet(true));
        pFunnelOrSegment = PFunnelSegmentBuild(pFunnelOrLabel, () => PFunnelModeSet(false));
        pFunnelJoin = PFunnelJoinBuild();
        pFunnelJoin.Visibility = PLook.PLookVisible[lCondition.LFunnelConditionJoin];

        Margin = new Thickness(0, 0, 0, 8);
        RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var pLabel = new TextBlock
        {
            Text = LLocalization.LLocalizationTextRead(lCondition.LFunnelConditionLabel),
            FontSize = 11,
            FontFamily = pFunnelFontFamily,
            Foreground = pFunnelMutedBrush,
            Margin = new Thickness(2, 0, 0, 3)
        };
        SetRow(pLabel, 0);
        Children.Add(pLabel);

        var pLine = new Grid();
        pLine.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(PFunnelJoinWidth) });
        pLine.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pLine.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        SetColumn(pFunnelJoin, 0);
        pLine.Children.Add(pFunnelJoin);
        SetColumn(pFunnelField, 1);
        pLine.Children.Add(pFunnelField);
        SetColumn(pFunnelCaseButton, 2);
        pLine.Children.Add(pFunnelCaseButton);

        SetRow(pLine, 1);
        Children.Add(pLine);
        PFunnelConditionUpdate();
    }

    public void PFunnelConditionUpdate()
    {
        LSceneFunnelMatch lMatch = lFunnel.LFunnelMatchRead(lFunnelRule, lFunnelKind);
        pFunnelField.Text = lFunnel.LFunnelTextResolve(lFunnelRule, lFunnelKind, pFunnelField.Text);
        PFunnelCaseApply(lMatch.LSceneFunnelCase);
        PFunnelJoinApply(lMatch.LSceneFunnelJoin);
    }

    private TextBox PFunnelFieldBuild()
    {
        var pField = new TextBox
        {
            Height = PFunnelFieldHeight,
            FontSize = 12,
            FontFamily = pFunnelFontFamily
        };
        PTextbox.PTextboxApply(pField);
        pField.TextChanged += (_, _) => lFunnel.LFunnelTextSet(lFunnelRule, lFunnelKind, pField.Text);
        return pField;
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
        var pHost = new Border
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
        pHost.MouseLeftButtonUp += (_, _) => lFunnel.LFunnelCaseToggle(lFunnelRule, lFunnelKind);
        return pHost;
    }

    private void PFunnelCaseApply(bool pOn)
    {
        pFunnelCaseButton.BorderBrush = pFunnelCaseBorders[pOn];
        pFunnelCaseButton.ToolTip = LLocalization.LLocalizationTextRead(pFunnelCaseTooltips[pOn]);
        pFunnelCaseLabel.Text = pFunnelCaseTexts[pOn];
        pFunnelCaseLabel.Foreground = pFunnelCaseForegrounds[pOn];
    }

    private Border PFunnelJoinBuild()
    {
        var pGrid = new Grid();
        pGrid.ColumnDefinitions.Add(new ColumnDefinition());
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition());
        var pDivider = new Border { Width = 1, Background = pFunnelLineBrush };
        SetColumn(pFunnelAndSegment, 0);
        SetColumn(pDivider, 1);
        SetColumn(pFunnelOrSegment, 2);
        pGrid.Children.Add(pFunnelAndSegment);
        pGrid.Children.Add(pDivider);
        pGrid.Children.Add(pFunnelOrSegment);

        return new Border
        {
            Height = PFunnelFieldHeight,
            Margin = new Thickness(0, 0, 6, 0),
            BorderBrush = pFunnelLineBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Background = Brushes.White,
            SnapsToDevicePixels = true,
            Child = pGrid
        };
    }

    private void PFunnelJoinApply(bool pAnd)
    {
        PFunnelSegmentApply(pFunnelAndSegment, pFunnelAndLabel, pAnd);
        PFunnelSegmentApply(pFunnelOrSegment, pFunnelOrLabel, !pAnd);
    }

    private static TextBlock PFunnelLabelBuild(string pText) => new()
    {
        Text = pText,
        FontSize = 11,
        FontFamily = pFunnelFontFamily,
        VerticalAlignment = VerticalAlignment.Center,
        HorizontalAlignment = HorizontalAlignment.Center
    };

    private static Border PFunnelSegmentBuild(TextBlock pLabel, Action pClick)
    {
        var pSegment = new Border
        {
            Padding = new Thickness(4, 3, 4, 3),
            Cursor = Cursors.Hand,
            Child = pLabel
        };
        pSegment.MouseLeftButtonUp += (_, _) => pClick();
        return pSegment;
    }

    private static void PFunnelSegmentApply(Border pSegment, TextBlock pLabel, bool pActive)
    {
        pLabel.FontWeight = pFunnelSegmentWeights[pActive];
        pLabel.Foreground = pFunnelSegmentForegrounds[pActive];
        pSegment.Background = pFunnelSegmentBackgrounds[pActive];
    }

    private void PFunnelModeSet(bool pAndMode) => lFunnel.LFunnelJoinSet(lFunnelRule, lFunnelKind, pAndMode);
}
