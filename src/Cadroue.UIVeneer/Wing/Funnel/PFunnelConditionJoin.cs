using Cadroue.UIDeportment;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Cadroue.UIVeneer;

internal sealed class PFunnelConditionJoin
{
    private static readonly FontFamily pFunnelFontFamily = new("Segoe UI");
    private static readonly Brush pFunnelLineBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7));
    private static readonly Brush pFunnelTitleBrush = new SolidColorBrush(Color.FromRgb(0x26, 0x36, 0x4A));
    private static readonly Brush pFunnelMutedBrush = new SolidColorBrush(Color.FromRgb(0x8A, 0x93, 0x9E));
    private static readonly Brush pFunnelActiveBrush = new SolidColorBrush(Color.FromRgb(0xCE, 0xE1, 0xFB));

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

    private readonly QFunnelCriterion lFunnelCriterion;
    private readonly QFunnelRule lFunnelRule;
    private readonly QFunnelKind lFunnelKind;
    private readonly TextBlock pFunnelAndLabel;
    private readonly Border pFunnelAndSegment;
    private readonly TextBlock pFunnelOrLabel;
    private readonly Border pFunnelOrSegment;

    public PFunnelConditionJoin(QFunnelCriterion lCriterion, QFunnelRule lRule, QFunnelKind lKind, bool pJoined)
    {
        lFunnelCriterion = lCriterion;
        lFunnelRule = lRule;
        lFunnelKind = lKind;
        pFunnelAndLabel = PFunnelLabelBuild(QLocalizationCatalog.QLocalizationTextRead("Inspector.Funnel.And"));
        pFunnelOrLabel = PFunnelLabelBuild(QLocalizationCatalog.QLocalizationTextRead("Inspector.Funnel.Or"));
        pFunnelAndSegment = PFunnelSegmentBuild(pFunnelAndLabel, () => PFunnelModeSet(true));
        pFunnelOrSegment = PFunnelSegmentBuild(pFunnelOrLabel, () => PFunnelModeSet(false));
        PFunnelJoinBorder = PFunnelJoinBuild(pJoined);
    }

    public Border PFunnelJoinBorder { get; }

    public void PFunnelJoinApply(bool pAnd)
    {
        PFunnelSegmentApply(pFunnelAndSegment, pFunnelAndLabel, pAnd);
        PFunnelSegmentApply(pFunnelOrSegment, pFunnelOrLabel, !pAnd);
    }

    private Border PFunnelJoinBuild(bool pJoined)
    {
        var pGrid = new Grid();
        pGrid.ColumnDefinitions.Add(new ColumnDefinition());
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition());
        var pDivider = new Border { Width = 1, Background = pFunnelLineBrush };
        Grid.SetColumn(pFunnelAndSegment, 0);
        Grid.SetColumn(pDivider, 1);
        Grid.SetColumn(pFunnelOrSegment, 2);
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
        lFunnelCriterion.QFunnelJoinSet(lFunnelRule, lFunnelKind, pAndMode);
}
