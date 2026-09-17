using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Cadroue.Application;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PSCasement;

using static Cadroue.UIVeneer.PSCasement.PSField;
using static Cadroue.UIVeneer.PSCasement.PSPlate;
using static Cadroue.UIVeneer.PSCasement.PSInline;

namespace Cadroue.UIVeneer;

internal sealed partial class PSDiagnosis
{
    private const double PSDiagnosisLabelWidth = 170;

    private readonly List<(string[] PSDiagnosisFilters, Border PSDiagnosisBadge, TextBlock PSDiagnosisText)>
        psDiagnosisChecks = new();

    private Ellipse psDiagnosisSummaryDot = null!;
    private TextBlock psDiagnosisSummaryText = null!;
    private Border psDiagnosisProgramBadge = null!;
    private TextBlock psDiagnosisProgramText = null!;
    private TextBlock psDiagnosisVersionValue = null!;
    private TextBlock psDiagnosisLocationValue = null!;

    private UIElement PSDiagnosisBannerBuild()
    {
        var pBar = new Grid { Margin = new Thickness(PSDiagnosisInset, 12, PSDiagnosisInset, 10) };
        pBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        psDiagnosisSummaryDot = new Ellipse
        {
            Width = 10,
            Height = 10,
            Fill = PSDiagnosisNeutralFill,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        };
        psDiagnosisSummaryText = new TextBlock
        {
            Text = LLocalization.LLocalizationTextRead("Diagnosis.Summary.Checking"),
            FontWeight = FontWeights.SemiBold,
            Foreground = PSFieldText,
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

        Button pRecheck = PSInlineButtonBuild(
            LLocalization.LLocalizationTextRead("Diagnosis.Recheck"),
            110,
            new Thickness(0));
        pRecheck.HorizontalAlignment = HorizontalAlignment.Right;
        pRecheck.Click += (_, _) => _ = lsDiagnosis.LSDiagnosisReset();
        Grid.SetColumn(pRecheck, 1);
        pBar.Children.Add(pRecheck);
        return pBar;
    }

    private UIElement PSDiagnosisContentBuild()
    {
        var pBody = new StackPanel { Margin = new Thickness(0, 2, 0, 0) };
        pBody.Children.Add(PSDiagnosisProgramBuild());
        pBody.Children.Add(PSDiagnosisGroupBuild("Diagnosis.Group.Video", LSDiagnosis.LSDiagnosisVideoItems));
        pBody.Children.Add(PSDiagnosisGroupBuild("Diagnosis.Group.Audio", LSDiagnosis.LSDiagnosisAudioItems));
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
        return PSPlateBuild(LLocalization.LLocalizationTextRead("Diagnosis.Group.Program"), pVersion, pLocation);
    }

    private UIElement PSDiagnosisGroupBuild(string pTitleKey, IReadOnlyList<LSDiagnosisItem> pItems)
    {
        UIElement[] pRows = pItems
            .Select(pItem => PSDiagnosisFeatureBuild(pItem.LSDiagnosisLabel, pItem.LSDiagnosisFilters))
            .ToArray();
        return PSPlateBuild(LLocalization.LLocalizationTextRead(pTitleKey), pRows);
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
        var pRow = new Grid { Margin = new Thickness(0, 0, 0, 6), MinHeight = PSFieldChipHeight };
        pRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(PSDiagnosisLabelWidth) });
        pRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        TextBlock pLabel = PSFieldLabelBuild(LLocalization.LLocalizationTextRead(pLabelKey));
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
        Foreground = PSFieldMuted,
        TextWrapping = TextWrapping.Wrap,
        VerticalAlignment = VerticalAlignment.Center,
        Margin = new Thickness(0, 0, 10, 0),
        Text = "…"
    };

    private static Border PSDiagnosisChipBuild(string pFilter) => new()
    {
        Background = PSDiagnosisChipFill,
        CornerRadius = new CornerRadius(4),
        Padding = new Thickness(8, 1, 8, 1),
        HorizontalAlignment = HorizontalAlignment.Left,
        VerticalAlignment = VerticalAlignment.Center,
        Margin = new Thickness(0, 0, 10, 0),
        Child = new TextBlock
        {
            Text = pFilter,
            FontFamily = PSDiagnosisChipFont,
            FontSize = 11,
            Foreground = PSFieldMuted
        }
    };

    private (Border PSDiagnosisBadge, TextBlock PSDiagnosisText) PSDiagnosisBadgeBuild()
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
        PSDiagnosisBadgeApply(pBadge, pText, LSDiagnosisState.LSDiagnosisStateChecking);
        return (pBadge, pText);
    }

    private static void PSDiagnosisBadgeApply(Border pBadge, TextBlock pText, LSDiagnosisState pState)
    {
        switch (pState)
        {
            case LSDiagnosisState.LSDiagnosisStateReady:
                pBadge.Background = PSDiagnosisReadyFill;
                pText.Foreground = PSDiagnosisReadyInk;
                pText.Text = LLocalization.LLocalizationTextRead("Encoder.Verification.Available");
                break;
            case LSDiagnosisState.LSDiagnosisStateMissing:
                pBadge.Background = PSDiagnosisMissingFill;
                pText.Foreground = PSDiagnosisMissingInk;
                pText.Text = LLocalization.LLocalizationTextRead("Encoder.Verification.Unavailable");
                break;
            default:
                pBadge.Background = PSDiagnosisNeutralFill;
                pText.Foreground = PSFieldMuted;
                pText.Text = "…";
                break;
        }
    }

    private void PSDiagnosisResultApply()
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(PSDiagnosisResultApply);
            return;
        }

        bool pChecking = lsDiagnosis.LSDiagnosisChecking;
        bool pReady = lsDiagnosis.LSDiagnosisReady;
        psDiagnosisVersionValue.Text = pChecking ? "…" : pReady ? lsDiagnosis.LSDiagnosisVersion : "—";
        psDiagnosisLocationValue.Text = pChecking
            ? "…"
            : lsDiagnosis.LSDiagnosisLocation.Length == 0
                ? LLocalization.LLocalizationTextRead("Diagnosis.LocationPath")
                : lsDiagnosis.LSDiagnosisLocation;
        PSDiagnosisBadgeApply(psDiagnosisProgramBadge, psDiagnosisProgramText, lsDiagnosis.LSDiagnosisProgramState);
        foreach ((string[] pFilters, Border pBadge, TextBlock pText) in psDiagnosisChecks)
        {
            PSDiagnosisBadgeApply(pBadge, pText, lsDiagnosis.LSDiagnosisStateRead(pFilters));
        }

        PSDiagnosisSummaryApply(lsDiagnosis.LSDiagnosisMood, lsDiagnosis.LSDiagnosisMissing);
    }

    private void PSDiagnosisSummaryApply(LSDiagnosisMood pMood, int pMissing)
    {
        switch (pMood)
        {
            case LSDiagnosisMood.LSDiagnosisMoodReady:
                psDiagnosisSummaryDot.Fill = PSDiagnosisReadyDot;
                psDiagnosisSummaryText.Text = LLocalization.LLocalizationTextRead("Diagnosis.Summary.Ready");
                break;
            case LSDiagnosisMood.LSDiagnosisMoodWarning:
                psDiagnosisSummaryDot.Fill = PSDiagnosisWarnDot;
                psDiagnosisSummaryText.Text = LLocalization.LLocalizationFormat("Diagnosis.Summary.Missing", pMissing);
                break;
            case LSDiagnosisMood.LSDiagnosisMoodMissing:
                psDiagnosisSummaryDot.Fill = PSDiagnosisMissingDot;
                psDiagnosisSummaryText.Text = LLocalization.LLocalizationFormat("Diagnosis.Summary.Missing", pMissing);
                break;
            case LSDiagnosisMood.LSDiagnosisMoodAbsent:
                psDiagnosisSummaryDot.Fill = PSDiagnosisMissingDot;
                psDiagnosisSummaryText.Text = LLocalization.LLocalizationTextRead("Diagnosis.Summary.NoProgram");
                break;
            default:
                psDiagnosisSummaryDot.Fill = PSDiagnosisNeutralFill;
                psDiagnosisSummaryText.Text = LLocalization.LLocalizationTextRead("Diagnosis.Summary.Checking");
                break;
        }
    }
}
