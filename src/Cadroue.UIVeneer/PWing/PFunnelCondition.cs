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

    private readonly QFunnel lFunnel;
    private readonly QFunnelRule lFunnelRule;
    private readonly QFunnelKind lFunnelKind;
    private readonly TextBox pFunnelField;
    private readonly TextBlock pFunnelCaseLabel;
    private readonly Border pFunnelCaseButton;
    private readonly Border pFunnelJoin;
    private readonly TextBlock pFunnelAndLabel;
    private readonly Border pFunnelAndSegment;
    private readonly TextBlock pFunnelOrLabel;
    private readonly Border pFunnelOrSegment;

    public PFunnelCondition(QFunnel lFunnelOwner, QFunnelRule lRule, QFunnelCondition lCondition)
    {
        lFunnel = lFunnelOwner;
        lFunnelRule = lRule;
        lFunnelKind = lCondition.QFunnelConditionKind;
        pFunnelField = PFunnelFieldBuild();
        pFunnelCaseLabel = PFunnelMonoBuild();
        pFunnelCaseButton = PFunnelCaseBuild(pFunnelCaseLabel);
        pFunnelAndLabel = PFunnelLabelBuild(QLocalizationCatalog.QLocalizationTextRead("Inspector.Funnel.And"));
        pFunnelOrLabel = PFunnelLabelBuild(QLocalizationCatalog.QLocalizationTextRead("Inspector.Funnel.Or"));
        pFunnelAndSegment = PFunnelSegmentBuild(pFunnelAndLabel, () => PFunnelModeSet(true));
        pFunnelOrSegment = PFunnelSegmentBuild(pFunnelOrLabel, () => PFunnelModeSet(false));
        pFunnelJoin = PFunnelJoinBuild(lCondition.QFunnelConditionJoin);
        PFunnelLayoutBuild(lCondition.QFunnelConditionLabel);
        PFunnelConditionUpdate();
    }

    public void PFunnelConditionUpdate()
    {
        LSceneFunnelCondition lMatch = lFunnel.QFunnelCriterion.QFunnelMatchRead(lFunnelRule, lFunnelKind);
        PFunnelFieldShow();
        PFunnelCaseApply(lMatch.LSceneFunnelCase);
        PFunnelCaseShow(lMatch.LSceneFunnelCase);
        PFunnelJoinApply(lMatch.LSceneFunnelJoin);
    }

    private void PFunnelLayoutBuild(string pLabelKey)
    {
        Margin = new Thickness(0, 0, 0, 8);
        RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var pLabel = new TextBlock
        {
            Text = QLocalizationCatalog.QLocalizationTextRead(pLabelKey),
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
    }

    private void PFunnelFieldShow() =>
        pFunnelField.Text = lFunnel.QFunnelCriterion.QFunnelTextResolve(lFunnelRule, lFunnelKind, pFunnelField.Text);

    private TextBox PFunnelFieldBuild()
    {
        TextBox pField = PFunnelFieldCreate();
        QTextbox.QTextboxAttach(pField);
        PFunnelFieldAttach(pField);
        return pField;
    }

    private static TextBox PFunnelFieldCreate() => new()
    {
        Height = PFunnelFieldHeight,
        FontSize = 12,
        FontFamily = pFunnelFontFamily
    };

    private void PFunnelFieldAttach(TextBox pField) =>
        pField.TextChanged += (_, _) => lFunnel.QFunnelCriterion.QFunnelTextSet(lFunnelRule, lFunnelKind, pField.Text);

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
        pHost.MouseLeftButtonUp += (_, _) => lFunnel.QFunnelCriterion.QFunnelCaseToggle(lFunnelRule, lFunnelKind);

    private void PFunnelCaseApply(bool pOn)
    {
        pFunnelCaseButton.BorderBrush = pFunnelCaseBorders[pOn];
        pFunnelCaseLabel.Foreground = pFunnelCaseForegrounds[pOn];
    }

    private void PFunnelCaseShow(bool pOn)
    {
        pFunnelCaseButton.ToolTip = QLocalizationCatalog.QLocalizationTextRead(pFunnelCaseTooltips[pOn]);
        pFunnelCaseLabel.Text = pFunnelCaseTexts[pOn];
    }

    private Border PFunnelJoinBuild(bool pJoined)
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
            Visibility = PLook.PLookVisible[pJoined],
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
        Border pSegment = PFunnelSegmentCreate(pLabel);
        PFunnelSegmentAttach(pSegment, pClick);
        return pSegment;
    }

    private static Border PFunnelSegmentCreate(TextBlock pLabel) => new()
    {
        Padding = new Thickness(4, 3, 4, 3),
        Cursor = Cursors.Hand,
        Child = pLabel
    };

    private static void PFunnelSegmentAttach(Border pSegment, Action pClick) =>
        pSegment.MouseLeftButtonUp += (_, _) => pClick();

    private static void PFunnelSegmentApply(Border pSegment, TextBlock pLabel, bool pActive)
    {
        pLabel.FontWeight = pFunnelSegmentWeights[pActive];
        pLabel.Foreground = pFunnelSegmentForegrounds[pActive];
        pSegment.Background = pFunnelSegmentBackgrounds[pActive];
    }

    private void PFunnelModeSet(bool pAndMode) =>
        lFunnel.QFunnelCriterion.QFunnelJoinSet(lFunnelRule, lFunnelKind, pAndMode);
}
