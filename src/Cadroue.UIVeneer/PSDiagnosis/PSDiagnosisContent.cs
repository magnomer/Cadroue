using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed class PSDiagnosisContent
{
    internal const double PSDiagnosisInset = 18;

    private const double PSDiagnosisLabelWidth = 170;

    private readonly List<(string[] PSDiagnosisFilters, Border PSDiagnosisBadge, TextBlock PSDiagnosisText)>
        psDiagnosisChecks = new();

    private Ellipse psDiagnosisSummaryDot = null!;
    private TextBlock psDiagnosisSummaryText = null!;
    private Border psDiagnosisProgramBadge = null!;
    private TextBlock psDiagnosisProgramText = null!;
    private TextBlock psDiagnosisVersionValue = null!;
    private TextBlock psDiagnosisLocationValue = null!;

    private static Brush PSDiagnosisBrushCreate(byte pRed, byte pGreen, byte pBlue)
    {
        var pBrush = new SolidColorBrush(Color.FromRgb(pRed, pGreen, pBlue));
        pBrush.Freeze();
        return pBrush;
    }

    internal UIElement PSDiagnosisBannerBuild(QSDiagnosis lsDiagnosis)
    {
        Button pRecheck = PSDiagnosisRecheckBuild();
        PSDiagnosisRecheckAttach(pRecheck, lsDiagnosis);
        return PSDiagnosisBarBuild(pRecheck);
    }

    private static Button PSDiagnosisRecheckBuild()
    {
        Button pRecheck = PSInline.PSInlineButtonBuild(
            QLocalizationCatalog.QLocalizationTextRead("Diagnosis.Recheck"),
            110,
            new Thickness(0));
        pRecheck.HorizontalAlignment = HorizontalAlignment.Right;
        Grid.SetColumn(pRecheck, 1);
        return pRecheck;
    }

    private static void PSDiagnosisRecheckAttach(Button pRecheck, QSDiagnosis lsDiagnosis) =>
        pRecheck.Click += (_, _) => _ = lsDiagnosis.QSDiagnosisReset();

    private Grid PSDiagnosisBarBuild(Button pRecheck)
    {
        var pBar = new Grid { Margin = new Thickness(PSDiagnosisInset, 12, PSDiagnosisInset, 10) };
        pBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        psDiagnosisSummaryDot = new Ellipse
        {
            Width = 10,
            Height = 10,
            Fill = PSDiagnosisBrushCreate(0xEC, 0xEF, 0xF4),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        };
        psDiagnosisSummaryText = new TextBlock
        {
            Text = QLocalizationCatalog.QLocalizationTextRead("Diagnosis.Summary.Checking"),
            FontWeight = FontWeights.SemiBold,
            Foreground = PSField.PSFieldText,
            VerticalAlignment = VerticalAlignment.Center
        };

        var pLeft = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };
        pLeft.Children.Add(psDiagnosisSummaryDot);
        pLeft.Children.Add(psDiagnosisSummaryText);
        pBar.Children.Add(pLeft);
        pBar.Children.Add(pRecheck);
        return pBar;
    }

    internal UIElement PSDiagnosisContentBuild()
    {
        var pBody = new StackPanel { Margin = new Thickness(0, 2, 0, 0) };
        pBody.Children.Add(PSDiagnosisProgramBuild());
        pBody.Children.Add(PSDiagnosisGroupBuild("Diagnosis.Group.Video", QSDiagnosis.QSDiagnosisVideoItems));
        pBody.Children.Add(PSDiagnosisGroupBuild("Diagnosis.Group.Audio", QSDiagnosis.QSDiagnosisAudioItems));
        return pBody;
    }

    private UIElement PSDiagnosisProgramBuild()
    {
        psDiagnosisVersionValue = PSDiagnosisValueBuild();
        psDiagnosisLocationValue = PSDiagnosisValueBuild();
        (psDiagnosisProgramBadge, psDiagnosisProgramText) = PSDiagnosisBadgeBuild();

        UIElement pVersion = PSDiagnosisLineBuild(
            "Diagnosis.Version",
            psDiagnosisVersionValue,
            psDiagnosisProgramBadge);
        UIElement pLocation = PSDiagnosisLineBuild("Options.System.Location", psDiagnosisLocationValue, null);
        return PSPlate.PSPlateBuild(QLocalizationCatalog.QLocalizationTextRead("Diagnosis.Group.Program"), pVersion,
            pLocation);
    }

    private UIElement PSDiagnosisGroupBuild(string pTitleKey, IReadOnlyList<QSDiagnosisItem> pItems)
    {
        UIElement[] pRows = pItems
            .Select(pItem => PSDiagnosisFeatureBuild(pItem.QSDiagnosisLabel, pItem.QSDiagnosisFilters))
            .ToArray();
        return PSPlate.PSPlateBuild(QLocalizationCatalog.QLocalizationTextRead(pTitleKey), pRows);
    }

    private UIElement PSDiagnosisFeatureBuild(string pLabelKey, string[] pFilters)
    {
        Border pChip = PSDiagnosisChipBuild(string.Join(", ", pFilters));
        (Border pBadge, TextBlock pText) = PSDiagnosisBadgeBuild();
        psDiagnosisChecks.Add((pFilters, pBadge, pText));
        return PSDiagnosisLineBuild(pLabelKey, pChip, pBadge);
    }

    private static Grid PSDiagnosisLineBuild(string pLabelKey, UIElement pMiddle, UIElement? pBadge)
    {
        var pRow = new Grid { Margin = new Thickness(0, 0, 0, 6), MinHeight = PSField.PSFieldChipHeight };
        pRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(PSDiagnosisLabelWidth) });
        pRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        TextBlock pLabel = PSField.PSFieldLabelBuild(QLocalizationCatalog.QLocalizationTextRead(pLabelKey));
        pLabel.TextWrapping = TextWrapping.Wrap;
        pLabel.Margin = new Thickness(0, 0, 10, 0);

        pRow.Children.Add(pLabel);
        Grid.SetColumn(pMiddle, 1);
        pRow.Children.Add(pMiddle);
        if (pBadge is not null)
        {
            Grid.SetColumn(pBadge, 2);
            pRow.Children.Add(pBadge);
        }

        return pRow;
    }

    private static TextBlock PSDiagnosisValueBuild() => new()
    {
        Foreground = PSField.PSFieldMuted,
        TextWrapping = TextWrapping.Wrap,
        VerticalAlignment = VerticalAlignment.Center,
        Margin = new Thickness(0, 0, 10, 0),
        Text = "…"
    };

    private static Border PSDiagnosisChipBuild(string pFilter) => new()
    {
        Background = PSDiagnosisBrushCreate(0xF1, 0xF5, 0xFA),
        CornerRadius = new CornerRadius(4),
        Padding = new Thickness(8, 1, 8, 1),
        HorizontalAlignment = HorizontalAlignment.Left,
        VerticalAlignment = VerticalAlignment.Center,
        Margin = new Thickness(0, 0, 10, 0),
        Child = new TextBlock
        {
            Text = pFilter,
            FontFamily = new FontFamily("Consolas, Cascadia Mono, monospace"),
            FontSize = 11,
            Foreground = PSField.PSFieldMuted
        }
    };

    private (Border PSDiagnosisBadge, TextBlock PSDiagnosisText) PSDiagnosisBadgeBuild()
    {
        (Border pBadge, TextBlock pText) = PSDiagnosisBadgeCreate();
        PSDiagnosisBadgeApply(pBadge, pText, QSDiagnosisState.QSDiagnosisStateChecking);
        return (pBadge, pText);
    }

    private static (Border PSDiagnosisBadge, TextBlock PSDiagnosisText) PSDiagnosisBadgeCreate()
    {
        var pText = new TextBlock { FontWeight = FontWeights.SemiBold, FontSize = 11 };
        var pBadge = new Border
        {
            CornerRadius = new CornerRadius(3),
            Padding = new Thickness(8, 1, 8, 1),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            Child = pText
        };
        return (pBadge, pText);
    }

    private static void PSDiagnosisBadgeApply(Border pBadge, TextBlock pText, QSDiagnosisState pState)
    {
        PSDiagnosisTintApply(pBadge, pText, pState);
        PSDiagnosisBadgeShow(pText, PSDiagnosisBadgeFormat(pState));
    }

    private static void PSDiagnosisTintApply(Border pBadge, TextBlock pText, QSDiagnosisState pState)
    {
        switch (pState)
        {
            case QSDiagnosisState.QSDiagnosisStateReady:
                pBadge.Background = PSDiagnosisBrushCreate(0xE8, 0xF1, 0xE7);
                pText.Foreground = PSDiagnosisBrushCreate(0x2E, 0x5B, 0x2B);
                break;
            case QSDiagnosisState.QSDiagnosisStateMissing:
                pBadge.Background = PSDiagnosisBrushCreate(0xFB, 0xE3, 0xE3);
                pText.Foreground = PSDiagnosisBrushCreate(0x8C, 0x1D, 0x1D);
                break;
            default:
                pBadge.Background = PSDiagnosisBrushCreate(0xEC, 0xEF, 0xF4);
                pText.Foreground = PSField.PSFieldMuted;
                break;
        }
    }

    private static string PSDiagnosisBadgeFormat(QSDiagnosisState pState) => pState switch
    {
        QSDiagnosisState.QSDiagnosisStateReady =>
            QLocalizationCatalog.QLocalizationTextRead("Encoder.Verification.Available"),
        QSDiagnosisState.QSDiagnosisStateMissing =>
            QLocalizationCatalog.QLocalizationTextRead("Encoder.Verification.Unavailable"),
        _ => "…"
    };

    private static void PSDiagnosisBadgeShow(TextBlock pText, string pLabel) => pText.Text = pLabel;

    internal void PSDiagnosisResultApply(QSDiagnosis lsDiagnosis)
    {
        bool pChecking = lsDiagnosis.QSDiagnosisChecking;
        bool pReady = lsDiagnosis.QSDiagnosisReady;
        PSDiagnosisValueShow(
            PSDiagnosisVersionFormat(lsDiagnosis, pChecking, pReady),
            PSDiagnosisLocationFormat(lsDiagnosis, pChecking));
        PSDiagnosisBadgeApply(psDiagnosisProgramBadge, psDiagnosisProgramText, lsDiagnosis.QSDiagnosisProgramState);
        PSDiagnosisCheckApply(lsDiagnosis);
        PSDiagnosisSummaryApply(lsDiagnosis.QSDiagnosisMood, lsDiagnosis.QSDiagnosisMissing);
    }

    private static string PSDiagnosisVersionFormat(QSDiagnosis lsDiagnosis, bool pChecking, bool pReady) =>
        pChecking ? "…" : pReady ? lsDiagnosis.QSDiagnosisVersion : "—";

    private static string PSDiagnosisLocationFormat(QSDiagnosis lsDiagnosis, bool pChecking) =>
        pChecking
            ? "…"
            : lsDiagnosis.QSDiagnosisLocation.Length == 0
                ? QLocalizationCatalog.QLocalizationTextRead("Diagnosis.LocationPath")
                : lsDiagnosis.QSDiagnosisLocation;

    private void PSDiagnosisValueShow(string pVersion, string pLocation)
    {
        psDiagnosisVersionValue.Text = pVersion;
        psDiagnosisLocationValue.Text = pLocation;
    }

    private void PSDiagnosisCheckApply(QSDiagnosis lsDiagnosis)
    {
        foreach ((string[] pFilters, Border pBadge, TextBlock pText) in psDiagnosisChecks)
        {
            PSDiagnosisBadgeApply(pBadge, pText, lsDiagnosis.QSDiagnosisStateRead(pFilters));
        }
    }

    private void PSDiagnosisSummaryApply(QSDiagnosisMood pMood, int pMissing)
    {
        PSSummaryLookApply(pMood);
        PSDiagnosisSummaryShow(PSDiagnosisSummaryFormat(pMood, pMissing));
    }

    private void PSSummaryLookApply(QSDiagnosisMood pMood)
    {
        psDiagnosisSummaryDot.Fill = pMood switch
        {
            QSDiagnosisMood.QSDiagnosisMoodReady => PSDiagnosisBrushCreate(0x2E, 0xA0, 0x43),
            QSDiagnosisMood.QSDiagnosisMoodWarning => PSDiagnosisBrushCreate(0xE0, 0xA0, 0x11),
            QSDiagnosisMood.QSDiagnosisMoodMissing => PSDiagnosisBrushCreate(0xD0, 0x33, 0x33),
            QSDiagnosisMood.QSDiagnosisMoodAbsent => PSDiagnosisBrushCreate(0xD0, 0x33, 0x33),
            _ => PSDiagnosisBrushCreate(0xEC, 0xEF, 0xF4)
        };
    }

    private static string PSDiagnosisSummaryFormat(QSDiagnosisMood pMood, int pMissing) => pMood switch
    {
        QSDiagnosisMood.QSDiagnosisMoodReady => QLocalizationCatalog.QLocalizationTextRead("Diagnosis.Summary.Ready"),
        QSDiagnosisMood.QSDiagnosisMoodWarning => QLocalizationCatalog.QLocalizationFormat(
            "Diagnosis.Summary.Missing",
            pMissing),
        QSDiagnosisMood.QSDiagnosisMoodMissing => QLocalizationCatalog.QLocalizationFormat(
            "Diagnosis.Summary.Missing",
            pMissing),
        QSDiagnosisMood.QSDiagnosisMoodAbsent => QLocalizationCatalog.QLocalizationTextRead(
            "Diagnosis.Summary.NoProgram"),
        _ => QLocalizationCatalog.QLocalizationTextRead("Diagnosis.Summary.Checking")
    };

    private void PSDiagnosisSummaryShow(string pSummary) => psDiagnosisSummaryText.Text = pSummary;
}
