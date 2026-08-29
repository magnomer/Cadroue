using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIShell.PAssets;
using Cadroue.UIShell.PMainWindow;

namespace Cadroue.UIShell.PPanels;

public sealed class PClinic : PPanel
{
    private static readonly FontFamily pClinicFontFamily = new("Segoe UI");
    private static readonly Brush pClinicTitleBrush = new SolidColorBrush(Color.FromRgb(0x26, 0x36, 0x4A));
    private static readonly Brush pClinicMutedBrush = new SolidColorBrush(Color.FromRgb(0x8A, 0x93, 0x9E));
    private static readonly Brush pClinicIconBrush = new SolidColorBrush(Color.FromRgb(0x56, 0x62, 0x73));

    public const double PClinicStripWidth = 48;

    private static readonly IReadOnlyList<(LFlawKind Kind, string Name)> pClinicKinds = new[]
    {
        (LFlawKind.LFlawKindContainer, "Container"),
        (LFlawKind.LFlawKindTruncation, "Truncation"),
        (LFlawKind.LFlawKindTransport, "Transport"),
        (LFlawKind.LFlawKindMetadata, "Metadata"),
        (LFlawKind.LFlawKindIndex, "Index"),
        (LFlawKind.LFlawKindFraming, "Framing"),
        (LFlawKind.LFlawKindConfig, "Config"),
        (LFlawKind.LFlawKindTiming, "Timing"),
        (LFlawKind.LFlawKindSecondary, "Secondary"),
        (LFlawKind.LFlawKindCoded, "Coded"),
        (LFlawKind.LFlawKindFfvone, "Ffvone")
    };

    public event Action<bool>? PClinicMinimizeChange;
    public event Action? PClinicPlanChange;
    public event Action<LFlawKind>? PClinicDiagnosisRequest;

    private readonly UIElement pClinicFullBody;
    private readonly UIElement pClinicStripBody;
    private readonly TextBlock pClinicTitleLabel;
    private readonly TextBlock pClinicEmptyNotice;
    private readonly UIElement pClinicItemBody;
    private readonly StackPanel pClinicToggleRow;
    private readonly TextBlock pClinicItemSimple;
    private readonly TextBlock pClinicItemTechnical;
    private readonly UIElement pClinicResultBody;
    private readonly TextBlock pClinicResultText;
    private readonly CheckBox pClinicApplyBox;
    private readonly CheckBox pClinicDiagnosisBox;
    private readonly CheckBox pClinicPersistentBox;
    private readonly PClinicSalvage pClinicSalvage = new();
    private readonly Border pClinicPersistentRow;
    private bool pClinicSalvageShown;
    private readonly Dictionary<LFlawKind, (bool Apply, bool Diagnosis, bool Persistent)> pClinicStates = new();
    private readonly Dictionary<(string Path, LFlawKind Kind), LCheckupResult> pClinicResults = new();
    private string? pClinicSource;
    private LFlawKind? pClinicCurrentKind;
    private bool pClinicSuppress;
    private bool pClinicMinimized;

    public PClinic() : base("")
    {
        pClinicTitleLabel = new TextBlock
        {
            Text = LLocalization.LLocalizationTextRead("Clinic.Header.Title"),
            FontSize = 12,
            FontFamily = pClinicFontFamily,
            FontWeight = FontWeights.SemiBold,
            Foreground = pClinicTitleBrush,
            VerticalAlignment = VerticalAlignment.Center
        };

        Button pMinimizeButton = PClinicButtonBuild(
            "/PAssets/PPanels/PListMinimize.svg",
            LLocalization.LLocalizationTextRead("Inspector.Panel.HideTooltip"),
            () => PClinicMinimizeSet(true));
        pMinimizeButton.HorizontalAlignment = HorizontalAlignment.Right;

        var pHeaderGrid = new Grid();
        pHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(pMinimizeButton, 1);
        pHeaderGrid.Children.Add(pClinicTitleLabel);
        pHeaderGrid.Children.Add(pMinimizeButton);

        var pHeader = new Border
        {
            Padding = new Thickness(12, 5, 6, 5),
            BorderBrush = PPanelLineBrush,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Background = Brushes.White,
            Child = pHeaderGrid
        };

        foreach ((LFlawKind pKind, string _) in pClinicKinds)
        {
            pClinicStates[pKind] = (false, false, false);
        }

        pClinicApplyBox = PClinicSwitchBuild(
            LLocalization.LLocalizationTextRead("Clinic.Apply"),
            LLocalization.LLocalizationTextRead("Clinic.Apply.Tooltip"));
        pClinicApplyBox.Checked += (_, _) => PClinicToggleHandle();
        pClinicApplyBox.Unchecked += (_, _) => PClinicToggleHandle();
        pClinicDiagnosisBox = PClinicSwitchBuild(
            LLocalization.LLocalizationTextRead("Clinic.Diagnosis"),
            LLocalization.LLocalizationTextRead("Clinic.Diagnosis.Tooltip"));
        pClinicDiagnosisBox.Margin = new Thickness(18, 0, 0, 0);
        pClinicDiagnosisBox.Checked += (_, _) => PClinicToggleHandle();
        pClinicDiagnosisBox.Unchecked += (_, _) => PClinicToggleHandle();

        pClinicToggleRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 4),
            IsEnabled = false
        };
        pClinicSalvage.PClinicSalvageActive.Visibility = Visibility.Collapsed;
        pClinicToggleRow.Children.Add(pClinicSalvage.PClinicSalvageActive);
        pClinicToggleRow.Children.Add(pClinicApplyBox);
        pClinicToggleRow.Children.Add(pClinicDiagnosisBox);

        pClinicEmptyNotice = new TextBlock
        {
            Text = LLocalization.LLocalizationTextRead("Clinic.Empty.Notice"),
            FontSize = 12,
            FontFamily = pClinicFontFamily,
            Foreground = pClinicMutedBrush,
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(16, 24, 16, 16)
        };

        pClinicItemSimple = new TextBlock
        {
            FontSize = 12,
            FontFamily = pClinicFontFamily,
            Foreground = PPanelTextBrush,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8)
        };
        pClinicItemTechnical = new TextBlock
        {
            FontSize = 11,
            FontFamily = pClinicFontFamily,
            Foreground = pClinicMutedBrush,
            TextWrapping = TextWrapping.Wrap
        };

        var pItemStack = new StackPanel();
        pItemStack.Children.Add(pClinicItemSimple);
        pItemStack.Children.Add(pClinicItemTechnical);
        pClinicItemBody = pItemStack;
        pClinicItemBody.Visibility = Visibility.Collapsed;

        var pResultHeader = new TextBlock
        {
            Text = LLocalization.LLocalizationTextRead("Clinic.Result.Header"),
            FontSize = 12,
            FontFamily = pClinicFontFamily,
            FontWeight = FontWeights.SemiBold,
            Foreground = pClinicTitleBrush,
            Margin = new Thickness(0, 0, 0, 6)
        };
        pClinicResultText = new TextBlock
        {
            Text = LLocalization.LLocalizationTextRead("Clinic.Result.Empty"),
            FontSize = 12,
            FontFamily = pClinicFontFamily,
            Foreground = pClinicMutedBrush,
            TextWrapping = TextWrapping.Wrap
        };

        var pResultStack = new StackPanel();
        pResultStack.Children.Add(PClinicSeparatorBuild());
        pResultStack.Children.Add(pResultHeader);
        pResultStack.Children.Add(pClinicResultText);
        pClinicResultBody = pResultStack;
        pClinicResultBody.Visibility = Visibility.Collapsed;

        var pBody = new StackPanel { Margin = new Thickness(12, 10, 12, 10) };
        pBody.Children.Add(pClinicToggleRow);
        pBody.Children.Add(PClinicSeparatorBuild());
        pBody.Children.Add(pClinicEmptyNotice);
        pBody.Children.Add(pClinicItemBody);
        pBody.Children.Add(pClinicResultBody);
        pBody.Children.Add(pClinicSalvage);
        pClinicSalvage.PClinicSalvageChange += () => PClinicPlanChange?.Invoke();

        var pScroll = new ScrollViewer
        {
            Content = pBody,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };

        pClinicPersistentBox = PClinicSwitchBuild(
            LLocalization.LLocalizationTextRead("Clinic.Persistent"),
            LLocalization.LLocalizationTextRead("Clinic.Persistent.Tooltip"));
        pClinicPersistentBox.IsEnabled = false;
        pClinicPersistentBox.Checked += (_, _) => PClinicPersistentHandle();
        pClinicPersistentBox.Unchecked += (_, _) => PClinicPersistentHandle();
        pClinicSalvage.PClinicSalvagePersistent.Visibility = Visibility.Collapsed;
        var pPersistentStack = new Grid();
        pPersistentStack.Children.Add(pClinicPersistentBox);
        pPersistentStack.Children.Add(pClinicSalvage.PClinicSalvagePersistent);
        pClinicPersistentRow = new Border
        {
            Padding = new Thickness(12, 8, 12, 8),
            BorderBrush = PPanelLineBrush,
            BorderThickness = new Thickness(0, 1, 0, 0),
            Background = Brushes.White,
            Child = pPersistentStack
        };

        var pRoot = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(pHeader, Dock.Top);
        pRoot.Children.Add(pHeader);
        DockPanel.SetDock(pClinicPersistentRow, Dock.Bottom);
        pRoot.Children.Add(pClinicPersistentRow);
        pRoot.Children.Add(pScroll);

        pClinicFullBody = pRoot;
        pClinicStripBody = PClinicStripBuild();
        pClinicStripBody.Visibility = Visibility.Collapsed;

        var pBodyHost = new Grid();
        pBodyHost.Children.Add(pClinicFullBody);
        pBodyHost.Children.Add(pClinicStripBody);

        FocusVisualStyle = null;
        Content = PPanelBorderBuild(pBodyHost);
    }

    public bool PClinicMinimizedCheck() => pClinicMinimized;

    public void PClinicMinimizeSet(bool pClinicMinimizeRequest)
    {
        if (pClinicMinimized == pClinicMinimizeRequest)
        {
            return;
        }

        pClinicMinimized = pClinicMinimizeRequest;
        pClinicFullBody.Visibility = pClinicMinimized ? Visibility.Collapsed : Visibility.Visible;
        pClinicStripBody.Visibility = pClinicMinimized ? Visibility.Visible : Visibility.Collapsed;
        PClinicMinimizeChange?.Invoke(pClinicMinimized);
    }

    public void PClinicSourceSet(string? pClinicSourcePath)
    {
        pClinicSource = string.IsNullOrWhiteSpace(pClinicSourcePath) ? null : pClinicSourcePath;
        PClinicResultApply();
    }

    public void PClinicResultsRemove(IReadOnlyList<string> pClinicPaths)
    {
        foreach (string pClinicPath in pClinicPaths)
        {
            foreach ((LFlawKind pKind, string _) in pClinicKinds)
            {
                pClinicResults.Remove((pClinicPath, pKind));
            }
        }
    }

    public void PClinicResultShow(string pClinicResultPath, LFlawKind pClinicResultKind, LCheckupResult pClinicResult)
    {
        pClinicResults[(pClinicResultPath, pClinicResultKind)] = pClinicResult;
        if (pClinicResultPath == pClinicSource && pClinicCurrentKind == pClinicResultKind)
        {
            PClinicResultApply();
        }
    }

    public void PClinicStepShow(string? pStepName)
    {
        pClinicSalvageShown = pStepName == "Salvage";
        pClinicSalvage.PClinicSalvageShow(pClinicSalvageShown);
        if (pClinicSalvageShown)
        {
            pClinicSalvage.PClinicSalvageUpdate(
                pClinicStates.Values.Any(pState => pState.Apply));
        }
        pClinicApplyBox.Visibility = pClinicSalvageShown ? Visibility.Collapsed : Visibility.Visible;
        pClinicDiagnosisBox.Visibility = pClinicSalvageShown ? Visibility.Collapsed : Visibility.Visible;
        pClinicPersistentBox.Visibility = pClinicSalvageShown ? Visibility.Collapsed : Visibility.Visible;
        pClinicSalvage.PClinicSalvageActive.Visibility = pClinicSalvageShown ? Visibility.Visible : Visibility.Collapsed;
        pClinicSalvage.PClinicSalvagePersistent.Visibility = pClinicSalvageShown ? Visibility.Visible : Visibility.Collapsed;
        if (pClinicSalvageShown)
        {
            pClinicCurrentKind = null;
            pClinicToggleRow.IsEnabled = true;
            pClinicToggleRow.Visibility = Visibility.Visible;
            pClinicEmptyNotice.Visibility = Visibility.Collapsed;
            pClinicItemSimple.Text = LLocalization.LLocalizationTextRead("Clinic.Step.Salvage.Simple");
            pClinicItemTechnical.Text = LLocalization.LLocalizationTextRead("Clinic.Step.Salvage.Technical");
            pClinicItemBody.Visibility = Visibility.Visible;
            pClinicResultBody.Visibility = Visibility.Collapsed;
            pClinicTitleLabel.Text = LLocalization.LLocalizationTextRead("Processing.Step.Salvage");
            return;
        }

        pClinicToggleRow.Visibility = Visibility.Visible;
        LFlawKind? pKind = pClinicKinds
            .Where(pEntry => pEntry.Name == pStepName)
            .Select(pEntry => (LFlawKind?)pEntry.Kind)
            .FirstOrDefault();
        pClinicCurrentKind = pKind;
        bool pKnown = pKind is not null;
        pClinicEmptyNotice.Visibility = pKnown ? Visibility.Collapsed : Visibility.Visible;
        pClinicItemBody.Visibility = pKnown ? Visibility.Visible : Visibility.Collapsed;
        pClinicToggleRow.IsEnabled = pKnown;
        pClinicPersistentBox.IsEnabled = pKnown;

        pClinicSuppress = true;
        if (pKind is { } pShownKind && pClinicStates.TryGetValue(pShownKind, out (bool Apply, bool Diagnosis, bool Persistent) pState))
        {
            pClinicApplyBox.IsChecked = pState.Apply;
            pClinicDiagnosisBox.IsChecked = pState.Diagnosis;
            pClinicPersistentBox.IsChecked = pState.Persistent;
        }
        else
        {
            pClinicApplyBox.IsChecked = false;
            pClinicDiagnosisBox.IsChecked = false;
            pClinicPersistentBox.IsChecked = false;
        }

        pClinicSuppress = false;
        PClinicResultApply();
        if (!pKnown)
        {
            pClinicTitleLabel.Text = LLocalization.LLocalizationTextRead("Clinic.Header.Title");
            return;
        }

        pClinicTitleLabel.Text = LLocalization.LLocalizationTextRead($"Processing.Step.{pStepName}");
        pClinicItemSimple.Text = LLocalization.LLocalizationTextRead($"Clinic.Step.{pStepName}.Simple");
        pClinicItemTechnical.Text = LLocalization.LLocalizationTextRead($"Clinic.Step.{pStepName}.Technical");
    }

    public LWorkFix PClinicPlanRead()
    {
        var pSteps = new List<LWorkFixStep>();
        foreach ((LFlawKind pKind, string _) in pClinicKinds)
        {
            (bool pApply, bool pDiagnosis, bool pPersistent) =
                pClinicStates.TryGetValue(pKind, out (bool Apply, bool Diagnosis, bool Persistent) pState)
                    ? pState
                    : (false, false, false);
            pSteps.Add(new LWorkFixStep(pKind, pApply, pDiagnosis, pPersistent));
        }

        return new LWorkFix(pSteps) { LWorkFixSalvage = pClinicSalvage.PClinicSalvageRead() };
    }

    public void PClinicPlanApply(LWorkFix pClinicPlan)
    {
        pClinicSalvage.PClinicSalvageApply(pClinicPlan.LWorkFixSalvage);
        foreach (LWorkFixStep pStep in pClinicPlan.LWorkFixSteps)
        {
            pClinicStates[pStep.LWorkFixKind] =
                (pStep.LWorkFixRepair, pStep.LWorkFixDiagnosis, pStep.LWorkFixPersistent);
        }

        if (pClinicSalvageShown)
        {
            pClinicSalvage.PClinicSalvageUpdate(
                pClinicStates.Values.Any(pState => pState.Apply));
        }

        if (pClinicCurrentKind is { } pKind
            && pClinicStates.TryGetValue(pKind, out (bool Apply, bool Diagnosis, bool Persistent) pCurrent))
        {
            pClinicSuppress = true;
            pClinicApplyBox.IsChecked = pCurrent.Apply;
            pClinicDiagnosisBox.IsChecked = pCurrent.Diagnosis;
            pClinicPersistentBox.IsChecked = pCurrent.Persistent;
            pClinicSuppress = false;
            PClinicResultApply();
        }
    }

    private void PClinicToggleHandle()
    {
        if (pClinicSuppress || pClinicCurrentKind is not { } pKind)
        {
            return;
        }

        bool pWasDiagnosis = pClinicStates.TryGetValue(pKind, out (bool Apply, bool Diagnosis, bool Persistent) pPrior)
            && pPrior.Diagnosis;
        bool pNowDiagnosis = pClinicDiagnosisBox.IsChecked == true;
        pClinicStates[pKind] = (
            pClinicApplyBox.IsChecked == true,
            pNowDiagnosis,
            pClinicPersistentBox.IsChecked == true);
        PClinicResultApply();
        PClinicPlanChange?.Invoke();
        if (!pWasDiagnosis && pNowDiagnosis)
        {
            PClinicDiagnosisRequest?.Invoke(pKind);
        }
    }

    private void PClinicPersistentHandle()
    {
        if (pClinicSuppress || pClinicCurrentKind is not { } pKind)
        {
            return;
        }

        pClinicStates[pKind] = (
            pClinicApplyBox.IsChecked == true,
            pClinicDiagnosisBox.IsChecked == true,
            pClinicPersistentBox.IsChecked == true);
        PClinicPlanChange?.Invoke();
    }

    private void PClinicResultApply()
    {
        bool pVisible = pClinicDiagnosisBox.IsChecked == true && pClinicItemBody.Visibility == Visibility.Visible;
        pClinicResultBody.Visibility = pVisible ? Visibility.Visible : Visibility.Collapsed;
        if (pVisible)
        {
            pClinicResultText.Text = PClinicResultResolve();
        }
    }

    private string PClinicResultResolve()
    {
        if (pClinicCurrentKind is not { } pKind)
        {
            return LLocalization.LLocalizationTextRead("Clinic.Result.Empty");
        }

        LCheckupResult pResult = pClinicSource is { } pSource
            && pClinicResults.TryGetValue((pSource, pKind), out LCheckupResult pStored)
            ? pStored
            : new LCheckupResult(pClinicSource ?? string.Empty, pKind, LCheckupOutcome.LCheckupOutcomeUntested);
        return LCheckupFormat.LCheckupBodyFormat(pResult, PClinicStringsRead());
    }

    private static LCheckupStrings PClinicStringsRead() => new(
        LLocalization.LLocalizationTextRead("Clinic.Result.Empty"),
        LLocalization.LLocalizationTextRead("Clinic.Result.Scanning"),
        LLocalization.LLocalizationTextRead("Clinic.Result.Clean"),
        LLocalization.LLocalizationTextRead("Clinic.Result.Failed"),
        LLocalization.LLocalizationTextRead("Clinic.Result.Defect"),
        LLocalization.LLocalizationTextRead("Clinic.Result.Evidence"),
        LLocalization.LLocalizationTextRead("Clinic.Result.Repair"));

    private UIElement PClinicStripBuild()
    {
        Button pMaximizeButton = PClinicButtonBuild(
            "/PAssets/PPanels/PListMaximize.svg",
            LLocalization.LLocalizationTextRead("Inspector.Panel.ShowTooltip"),
            () => PClinicMinimizeSet(false));
        pMaximizeButton.Margin = new Thickness(0, 6, 0, 0);
        pMaximizeButton.HorizontalAlignment = HorizontalAlignment.Center;

        var pStrip = new StackPanel { Background = Brushes.White };
        pStrip.Children.Add(pMaximizeButton);
        return pStrip;
    }

    internal static CheckBox PClinicSwitchBuild(string pSwitchLabel, string pSwitchTip)
    {
        var pSwitch = new CheckBox
        {
            Content = pSwitchLabel,
            ToolTip = pSwitchTip,
            FontSize = 12,
            FontFamily = pClinicFontFamily,
            Foreground = PPanelTextBrush,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        PCheckbox.PCheckboxApply(pSwitch);
        return pSwitch;
    }

    private static UIElement PClinicSeparatorBuild() => new Border
    {
        Height = 1,
        Background = PPanelLineBrush,
        Margin = new Thickness(0, 10, 0, 10)
    };

    private static Button PClinicButtonBuild(string pIconPath, string pTooltip, Action pClick)
    {
        var pButton = new Button
        {
            Content = new Image
            {
                Width = 14,
                Height = 14,
                Source = PIcon.PIconRead(pIconPath, pClinicIconBrush),
                Stretch = Stretch.Uniform
            },
            ToolTip = pTooltip,
            Width = 28,
            Height = 26,
            Style = PButton.PButtonPanelCreate()
        };
        pButton.Click += (_, _) => pClick();
        return pButton;
    }
}
