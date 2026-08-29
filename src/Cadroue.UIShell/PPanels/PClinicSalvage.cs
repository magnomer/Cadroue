using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.Core;
using Cadroue.UIShell.PMainWindow;

namespace Cadroue.UIShell.PPanels;

public sealed class PClinicSalvage : StackPanel
{
    private static readonly FontFamily pClinicSalvageFont = new("Segoe UI");

    public event Action? PClinicSalvageChange;

    private readonly CheckBox pClinicSalvageActive;
    private readonly CheckBox pClinicSalvagePersistent;
    private readonly RadioButton pClinicSalvageRejoin;
    private readonly RadioButton pClinicSalvageSeparate;
    private readonly RadioButton pClinicSalvageSource;
    private readonly RadioButton pClinicSalvageFixed;
    private readonly TextBlock pClinicSalvageHeading;
    private readonly Border pClinicSalvageBasis;
    private readonly TextBlock pClinicSalvageDescription;
    private bool pClinicSalvageSuppress;
    private bool pClinicSalvageRepair;

    public PClinicSalvage()
    {
        Visibility = Visibility.Collapsed;

        pClinicSalvageActive = PClinic.PClinicSwitchBuild(
            LLocalization.LLocalizationTextRead("Clinic.Salvage.Apply"),
            LLocalization.LLocalizationTextRead("Clinic.Salvage.Apply.Tooltip"));
        pClinicSalvageActive.Checked += (_, _) => PClinicSalvageHandle();
        pClinicSalvageActive.Unchecked += (_, _) => PClinicSalvageHandle();

        var pModeLabel = new TextBlock
        {
            Text = LLocalization.LLocalizationTextRead("Clinic.Salvage.Mode.Label"),
            FontSize = 12,
            FontFamily = pClinicSalvageFont,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(0x26, 0x36, 0x4A)),
            Margin = new Thickness(0, 12, 0, 6)
        };

        pClinicSalvageRejoin = new RadioButton
        {
            Content = LLocalization.LLocalizationTextRead("Clinic.Salvage.Mode.Rejoin"),
            ToolTip = LLocalization.LLocalizationTextRead("Clinic.Salvage.Mode.Rejoin.Tooltip"),
            FontSize = 12,
            FontFamily = pClinicSalvageFont
        };
        pClinicSalvageSeparate = new RadioButton
        {
            Content = LLocalization.LLocalizationTextRead("Clinic.Salvage.Mode.Separate"),
            ToolTip = LLocalization.LLocalizationTextRead("Clinic.Salvage.Mode.Separate.Tooltip"),
            FontSize = 12,
            FontFamily = pClinicSalvageFont
        };
        pClinicSalvageRejoin.Checked += (_, _) => PClinicSalvageHandle();
        pClinicSalvageSeparate.Checked += (_, _) => PClinicSalvageHandle();
        Border pModeSegment = PRadio.PRadioSegmentBuild(pClinicSalvageRejoin, pClinicSalvageSeparate);
        pModeSegment.HorizontalAlignment = HorizontalAlignment.Left;

        pClinicSalvageHeading = new TextBlock
        {
            Text = LLocalization.LLocalizationTextRead("Clinic.Salvage.Basis.Label"),
            FontSize = 12,
            FontFamily = pClinicSalvageFont,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(0x26, 0x36, 0x4A)),
            Margin = new Thickness(0, 14, 0, 6)
        };

        pClinicSalvageSource = new RadioButton
        {
            Content = LLocalization.LLocalizationTextRead("Clinic.Salvage.Basis.Source"),
            ToolTip = LLocalization.LLocalizationTextRead("Clinic.Salvage.Basis.Source.Tooltip"),
            FontSize = 12,
            FontFamily = pClinicSalvageFont
        };
        pClinicSalvageFixed = new RadioButton
        {
            Content = LLocalization.LLocalizationTextRead("Clinic.Salvage.Basis.Fixed"),
            ToolTip = LLocalization.LLocalizationTextRead("Clinic.Salvage.Basis.Fixed.Tooltip"),
            FontSize = 12,
            FontFamily = pClinicSalvageFont
        };
        pClinicSalvageSource.Checked += (_, _) => PClinicSalvageDescribe(true);
        pClinicSalvageFixed.Checked += (_, _) => PClinicSalvageDescribe(true);
        pClinicSalvageBasis = PRadio.PRadioSegmentBuild(pClinicSalvageSource, pClinicSalvageFixed);
        pClinicSalvageBasis.HorizontalAlignment = HorizontalAlignment.Left;

        pClinicSalvageDescription = new TextBlock
        {
            FontSize = 11,
            FontFamily = pClinicSalvageFont,
            Foreground = new SolidColorBrush(Color.FromRgb(0x5A, 0x67, 0x78)),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 8, 0, 0)
        };

        pClinicSalvagePersistent = PClinic.PClinicSwitchBuild(
            LLocalization.LLocalizationTextRead("Clinic.Salvage.Persistent"),
            LLocalization.LLocalizationTextRead("Clinic.Salvage.Persistent.Tooltip"));
        pClinicSalvagePersistent.Margin = new Thickness(0, 14, 0, 0);
        pClinicSalvagePersistent.Checked += (_, _) => PClinicSalvageHandle();
        pClinicSalvagePersistent.Unchecked += (_, _) => PClinicSalvageHandle();

        Children.Add(pClinicSalvageActive);
        Children.Add(pModeLabel);
        Children.Add(pModeSegment);
        Children.Add(pClinicSalvageHeading);
        Children.Add(pClinicSalvageBasis);
        Children.Add(pClinicSalvageDescription);
        Children.Add(pClinicSalvagePersistent);
    }

    public void PClinicSalvageShow(bool pClinicSalvageVisible) =>
        Visibility = pClinicSalvageVisible ? Visibility.Visible : Visibility.Collapsed;

    // The From-source/From-fixed choice only has meaning when at least one repair step is
    // selected; with none, salvage runs from the source alone, so the segment is hidden and
    // the description states the source-only behaviour.
    public void PClinicSalvageUpdate(bool pClinicSalvageHasRepair)
    {
        pClinicSalvageRepair = pClinicSalvageHasRepair;
        Visibility pVisible = pClinicSalvageHasRepair ? Visibility.Visible : Visibility.Collapsed;
        pClinicSalvageHeading.Visibility = pVisible;
        pClinicSalvageBasis.Visibility = pVisible;
        PClinicSalvageDescribe(false);
    }

    public LWorkFixSalvage PClinicSalvageRead() => new(
        pClinicSalvageActive.IsChecked == true,
        pClinicSalvageSeparate.IsChecked == true
            ? LSalvageMode.LSalvageModeSeparate
            : LSalvageMode.LSalvageModeRejoin,
        pClinicSalvageFixed.IsChecked == true
            ? LSalvageBasis.LSalvageBasisFixed
            : LSalvageBasis.LSalvageBasisSource,
        pClinicSalvagePersistent.IsChecked == true);

    public void PClinicSalvageApply(LWorkFixSalvage pClinicSalvagePlan)
    {
        pClinicSalvageSuppress = true;
        pClinicSalvageActive.IsChecked = pClinicSalvagePlan.LWorkSalvageActive;
        pClinicSalvagePersistent.IsChecked = pClinicSalvagePlan.LWorkSalvagePersistent;
        pClinicSalvageSeparate.IsChecked =
            pClinicSalvagePlan.LWorkSalvageMode == LSalvageMode.LSalvageModeSeparate;
        pClinicSalvageRejoin.IsChecked =
            pClinicSalvagePlan.LWorkSalvageMode != LSalvageMode.LSalvageModeSeparate;
        pClinicSalvageFixed.IsChecked =
            pClinicSalvagePlan.LWorkSalvageBasis == LSalvageBasis.LSalvageBasisFixed;
        pClinicSalvageSource.IsChecked =
            pClinicSalvagePlan.LWorkSalvageBasis != LSalvageBasis.LSalvageBasisFixed;
        pClinicSalvageSuppress = false;
        PClinicSalvageDescribe(false);
    }

    private void PClinicSalvageDescribe(bool pClinicSalvageNotify)
    {
        string pClinicSalvageKey = !pClinicSalvageRepair
            ? "Clinic.Salvage.Basis.None"
            : pClinicSalvageFixed.IsChecked == true
                ? "Clinic.Salvage.Basis.Fixed.Description"
                : "Clinic.Salvage.Basis.Source.Description";
        pClinicSalvageDescription.Text = LLocalization.LLocalizationTextRead(pClinicSalvageKey);
        if (pClinicSalvageNotify)
        {
            PClinicSalvageHandle();
        }
    }

    private void PClinicSalvageHandle()
    {
        if (pClinicSalvageSuppress)
        {
            return;
        }

        PClinicSalvageChange?.Invoke();
    }
}
