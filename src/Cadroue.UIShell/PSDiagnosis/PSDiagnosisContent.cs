using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Cadroue.Infrastructure;
using Cadroue.UIShell.PSShared;

using static Cadroue.UIShell.PSShared.PSField;

namespace Cadroue.UIShell;

internal sealed partial class PSDiagnosis
{
    private enum PSDiagnosisState
    {
        PSDiagnosisStateChecking,
        PSDiagnosisStateReady,
        PSDiagnosisStateMissing
    }

    private enum PSDiagnosisMood
    {
        PSDiagnosisMoodChecking,
        PSDiagnosisMoodReady,
        PSDiagnosisMoodWarning,
        PSDiagnosisMoodMissing,
        PSDiagnosisMoodAbsent
    }

    private static readonly (string PSDiagnosisLabel, string[] PSDiagnosisFilters)[] PSDiagnosisVideoItems =
    {
        ("Diagnosis.Feature.Brightness", new[] { "eq" }),
        ("Diagnosis.Feature.Contrast", new[] { "eq" }),
        ("Diagnosis.Feature.Gamma", new[] { "eq" }),
        ("Diagnosis.Feature.Saturation", new[] { "eq" }),
        ("Diagnosis.Feature.Exposure", new[] { "exposure", "scale", "format" }),
        ("Diagnosis.Feature.Whitebalance", new[] { "colorcorrect", "scale", "format" }),
        ("Diagnosis.Feature.WhitebalanceManual", new[] { "colorchannelmixer", "eq", "scale", "format" }),
        ("Diagnosis.Feature.Crop", new[] { "crop" }),
        ("Diagnosis.Feature.Rotate", new[] { "transpose", "hflip", "vflip" }),
        ("Diagnosis.Feature.Resize", new[] { "scale" })
    };

    private static readonly (string PSDiagnosisLabel, string[] PSDiagnosisFilters)[] PSDiagnosisAudioItems =
    {
        ("Diagnosis.Feature.Volume", new[] { "volume" }),
        ("Diagnosis.Feature.Loudness", new[] { "loudnorm" }),
        ("Diagnosis.Feature.Dynamic", new[] { "dynaudnorm" }),
        ("Diagnosis.Feature.Equalizer", new[] { "equalizer" }),
        ("Diagnosis.Feature.Highpass", new[] { "highpass" }),
        ("Diagnosis.Feature.Lowpass", new[] { "lowpass" }),
        ("Diagnosis.Feature.Noise", new[] { "afftdn" })
    };

    private const double PSDiagnosisLabelWidth = 170;

    private readonly List<(string[] PSDiagnosisFilters, Border PSDiagnosisBadge, TextBlock PSDiagnosisText)> psDiagnosisChecks = new();

    private int psDiagnosisGeneration;

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

        var pLeft = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        pLeft.Children.Add(psDiagnosisSummaryDot);
        pLeft.Children.Add(psDiagnosisSummaryText);
        pBar.Children.Add(pLeft);

        Button pRecheck = PSInlineButtonBuild(LLocalization.LLocalizationTextRead("Diagnosis.Recheck"), 110, new Thickness(0));
        pRecheck.HorizontalAlignment = HorizontalAlignment.Right;
        pRecheck.Click += (_, _) =>
        {
            LInventory.LInventoryReset();
            PSDiagnosisProbeStart();
        };
        Grid.SetColumn(pRecheck, 1);
        pBar.Children.Add(pRecheck);
        return pBar;
    }

    private UIElement PSDiagnosisContentBuild()
    {
        var pBody = new StackPanel { Margin = new Thickness(0, 2, 0, 0) };
        pBody.Children.Add(PSDiagnosisProgramBuild());
        pBody.Children.Add(PSDiagnosisGroupBuild("Diagnosis.Group.Video", PSDiagnosisVideoItems));
        pBody.Children.Add(PSDiagnosisGroupBuild("Diagnosis.Group.Audio", PSDiagnosisAudioItems));
        return pBody;
    }

    private UIElement PSDiagnosisProgramBuild()
    {
        psDiagnosisVersionValue = PSDiagnosisValueBuild();
        psDiagnosisLocationValue = PSDiagnosisValueBuild();
        (psDiagnosisProgramBadge, psDiagnosisProgramText) = PSDiagnosisBadgeBuild();

        UIElement pVersion = PSDiagnosisLineBuild("Diagnosis.Version", psDiagnosisVersionValue, psDiagnosisProgramBadge);
        UIElement pLocation = PSDiagnosisLineBuild("Options.System.Location", psDiagnosisLocationValue, null);
        return PSPlateBuild(LLocalization.LLocalizationTextRead("Diagnosis.Group.Program"), pVersion, pLocation);
    }

    private UIElement PSDiagnosisGroupBuild(string pTitleKey, (string PSDiagnosisLabel, string[] PSDiagnosisFilters)[] pItems)
    {
        UIElement[] pRows = pItems.Select(pItem => PSDiagnosisFeatureBuild(pItem.PSDiagnosisLabel, pItem.PSDiagnosisFilters)).ToArray();
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
        PSDiagnosisBadgeApply(pBadge, pText, PSDiagnosisState.PSDiagnosisStateChecking);
        return (pBadge, pText);
    }

    private static void PSDiagnosisBadgeApply(Border pBadge, TextBlock pText, PSDiagnosisState pState)
    {
        switch (pState)
        {
            case PSDiagnosisState.PSDiagnosisStateReady:
                pBadge.Background = PSDiagnosisReadyFill;
                pText.Foreground = PSDiagnosisReadyInk;
                pText.Text = LLocalization.LLocalizationTextRead("Encoder.Verification.Available");
                break;
            case PSDiagnosisState.PSDiagnosisStateMissing:
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

    private void PSDiagnosisProbeStart()
    {
        PSDiagnosisSummaryApply(PSDiagnosisMood.PSDiagnosisMoodChecking, 0);
        PSDiagnosisBadgeApply(psDiagnosisProgramBadge, psDiagnosisProgramText, PSDiagnosisState.PSDiagnosisStateChecking);
        psDiagnosisVersionValue.Text = "…";
        psDiagnosisLocationValue.Text = "…";
        foreach ((_, Border pBadge, TextBlock pText) in psDiagnosisChecks)
        {
            PSDiagnosisBadgeApply(pBadge, pText, PSDiagnosisState.PSDiagnosisStateChecking);
        }

        int pGeneration = ++psDiagnosisGeneration;
        string[] pFilters = psDiagnosisChecks
            .SelectMany(pCheck => pCheck.PSDiagnosisFilters)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        Task.Run(() =>
        {
            string pVersion = LInventory.LInventoryVersionRead();
            string pLocation = PSDiagnosisLocationResolve();
            var pMap = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(pVersion))
            {
                foreach (string pFilter in pFilters)
                {
                    pMap[pFilter] = LInventory.LInventoryFilterConfirm(pFilter);
                }
            }

            Dispatcher.BeginInvoke(() =>
            {
                if (pGeneration == psDiagnosisGeneration)
                {
                    PSDiagnosisResultApply(pVersion, pLocation, pMap);
                }
            });
        });
    }

    private void PSDiagnosisResultApply(string pVersion, string pLocation, Dictionary<string, bool> pMap)
    {
        bool pReady = !string.IsNullOrWhiteSpace(pVersion);
        psDiagnosisVersionValue.Text = pReady ? pVersion : "—";
        psDiagnosisLocationValue.Text = pLocation;
        PSDiagnosisBadgeApply(psDiagnosisProgramBadge, psDiagnosisProgramText,
            pReady ? PSDiagnosisState.PSDiagnosisStateReady : PSDiagnosisState.PSDiagnosisStateMissing);

        int pMissing = 0;
        foreach ((string[] pFilters, Border pBadge, TextBlock pText) in psDiagnosisChecks)
        {
            bool pOk = pReady && PSDiagnosisFiltersCheck(pFilters, pMap);
            PSDiagnosisBadgeApply(pBadge, pText,
                pOk ? PSDiagnosisState.PSDiagnosisStateReady : PSDiagnosisState.PSDiagnosisStateMissing);
            if (!pOk)
            {
                pMissing++;
            }
        }

        if (!pReady)
        {
            PSDiagnosisSummaryApply(PSDiagnosisMood.PSDiagnosisMoodAbsent, 0);
            return;
        }

        if (pMissing == 0)
        {
            PSDiagnosisSummaryApply(PSDiagnosisMood.PSDiagnosisMoodReady, 0);
            return;
        }

        bool pVideoAny = PSDiagnosisGroupCheck(PSDiagnosisVideoItems, pMap);
        bool pAudioAny = PSDiagnosisGroupCheck(PSDiagnosisAudioItems, pMap);
        PSDiagnosisSummaryApply(
            pVideoAny && pAudioAny ? PSDiagnosisMood.PSDiagnosisMoodWarning : PSDiagnosisMood.PSDiagnosisMoodMissing,
            pMissing);
    }

    private static bool PSDiagnosisGroupCheck(
        (string PSDiagnosisLabel, string[] PSDiagnosisFilters)[] pItems,
        Dictionary<string, bool> pMap) =>
        pItems.Any(pItem => PSDiagnosisFiltersCheck(pItem.PSDiagnosisFilters, pMap));

    private static bool PSDiagnosisFiltersCheck(string[] pFilters, Dictionary<string, bool> pMap) =>
        pFilters.All(pFilter => pMap.TryGetValue(pFilter, out bool pValue) && pValue);

    private void PSDiagnosisSummaryApply(PSDiagnosisMood pMood, int pMissing)
    {
        switch (pMood)
        {
            case PSDiagnosisMood.PSDiagnosisMoodReady:
                psDiagnosisSummaryDot.Fill = PSDiagnosisReadyDot;
                psDiagnosisSummaryText.Text = LLocalization.LLocalizationTextRead("Diagnosis.Summary.Ready");
                break;
            case PSDiagnosisMood.PSDiagnosisMoodWarning:
                psDiagnosisSummaryDot.Fill = PSDiagnosisWarnDot;
                psDiagnosisSummaryText.Text = LLocalization.LLocalizationFormat("Diagnosis.Summary.Missing", pMissing);
                break;
            case PSDiagnosisMood.PSDiagnosisMoodMissing:
                psDiagnosisSummaryDot.Fill = PSDiagnosisMissingDot;
                psDiagnosisSummaryText.Text = LLocalization.LLocalizationFormat("Diagnosis.Summary.Missing", pMissing);
                break;
            case PSDiagnosisMood.PSDiagnosisMoodAbsent:
                psDiagnosisSummaryDot.Fill = PSDiagnosisMissingDot;
                psDiagnosisSummaryText.Text = LLocalization.LLocalizationTextRead("Diagnosis.Summary.NoProgram");
                break;
            default:
                psDiagnosisSummaryDot.Fill = PSDiagnosisNeutralFill;
                psDiagnosisSummaryText.Text = LLocalization.LLocalizationTextRead("Diagnosis.Summary.Checking");
                break;
        }
    }

    private static string PSDiagnosisLocationResolve()
    {
        string pExe = Cadroue.Media.LTool.LToolFfmpegRead();
        if (!System.IO.Path.IsPathRooted(pExe))
        {
            return LLocalization.LLocalizationTextRead("Diagnosis.LocationPath");
        }

        string? pFolder = System.IO.Path.GetDirectoryName(pExe);
        return string.IsNullOrEmpty(pFolder) ? pExe : pFolder;
    }
}
