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
    private bool pClinicSalvageSuppress;

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

        pClinicSalvagePersistent = PClinic.PClinicSwitchBuild(
            LLocalization.LLocalizationTextRead("Clinic.Salvage.Persistent"),
            LLocalization.LLocalizationTextRead("Clinic.Salvage.Persistent.Tooltip"));
        pClinicSalvagePersistent.Margin = new Thickness(0, 14, 0, 0);
        pClinicSalvagePersistent.Checked += (_, _) => PClinicSalvageHandle();
        pClinicSalvagePersistent.Unchecked += (_, _) => PClinicSalvageHandle();

        Children.Add(pClinicSalvageActive);
        Children.Add(pModeLabel);
        Children.Add(pModeSegment);
        Children.Add(pClinicSalvagePersistent);
    }

    public void PClinicSalvageShow(bool pClinicSalvageVisible) =>
        Visibility = pClinicSalvageVisible ? Visibility.Visible : Visibility.Collapsed;

    public LWorkFixSalvage PClinicSalvageRead() => new(
        pClinicSalvageActive.IsChecked == true,
        pClinicSalvageSeparate.IsChecked == true
            ? LSalvageMode.LSalvageModeSeparate
            : LSalvageMode.LSalvageModeRejoin,
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
        pClinicSalvageSuppress = false;
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
