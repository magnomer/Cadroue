using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.Core;
using Cadroue.Application;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PPanel;

public sealed class PClinicSalvage : StackPanel
{
    private static readonly FontFamily pClinicSalvageFont = new("Segoe UI");

    private readonly LClinic lClinic;
    private readonly CheckBox pClinicSalvageActive;
    private readonly CheckBox pClinicSalvagePersistent;
    private readonly RadioButton pClinicSalvageRejoin;
    private readonly RadioButton pClinicSalvageSeparate;
    private readonly RadioButton pClinicSalvageSource;
    private readonly RadioButton pClinicSalvageFixed;
    private readonly TextBlock pClinicSalvageDescription;

    public PClinicSalvage(LClinic lClinicOwner)
    {
        lClinic = lClinicOwner;
        Visibility = Visibility.Collapsed;

        pClinicSalvageActive = PClinic.PClinicSwitchBuild(
            LLocalization.LLocalizationTextRead("Clinic.Salvage.Apply"),
            LLocalization.LLocalizationTextRead("Clinic.Salvage.Apply.Tooltip"));
        pClinicSalvageActive.Checked += (_, _) => PClinicSalvageSet(lClinic.LClinicSalvage with { LWorkSalvageActive = true });
        pClinicSalvageActive.Unchecked += (_, _) => PClinicSalvageSet(lClinic.LClinicSalvage with { LWorkSalvageActive = false });

        var pModeLabel = new TextBlock
        {
            Text = LLocalization.LLocalizationTextRead("Clinic.Salvage.Mode.Label"),
            FontSize = 12,
            FontFamily = pClinicSalvageFont,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(0x26, 0x36, 0x4A)),
            Margin = new Thickness(0, 12, 0, 6)
        };

        pClinicSalvageRejoin = PClinicRadioBuild("Clinic.Salvage.Mode.Rejoin");
        pClinicSalvageSeparate = PClinicRadioBuild("Clinic.Salvage.Mode.Separate");
        pClinicSalvageRejoin.Checked += (_, _) =>
            PClinicSalvageSet(lClinic.LClinicSalvage with { LWorkSalvageMode = LSalvageMode.LSalvageModeRejoin });
        pClinicSalvageSeparate.Checked += (_, _) =>
            PClinicSalvageSet(lClinic.LClinicSalvage with { LWorkSalvageMode = LSalvageMode.LSalvageModeSeparate });
        Border pModeSegment = PRadio.PRadioSegmentBuild(pClinicSalvageRejoin, pClinicSalvageSeparate);
        pModeSegment.HorizontalAlignment = HorizontalAlignment.Left;

        var pBasisHeading = new TextBlock
        {
            Text = LLocalization.LLocalizationTextRead("Clinic.Salvage.Basis.Label"),
            FontSize = 12,
            FontFamily = pClinicSalvageFont,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(0x26, 0x36, 0x4A)),
            Margin = new Thickness(0, 14, 0, 6)
        };

        pClinicSalvageSource = PClinicRadioBuild("Clinic.Salvage.Basis.Source");
        pClinicSalvageFixed = PClinicRadioBuild("Clinic.Salvage.Basis.Fixed");
        pClinicSalvageSource.Checked += (_, _) =>
            PClinicSalvageSet(lClinic.LClinicSalvage with { LWorkSalvageBasis = LSalvageBasis.LSalvageBasisSource });
        pClinicSalvageFixed.Checked += (_, _) =>
            PClinicSalvageSet(lClinic.LClinicSalvage with { LWorkSalvageBasis = LSalvageBasis.LSalvageBasisFixed });
        Border pBasisSegment = PRadio.PRadioSegmentBuild(pClinicSalvageSource, pClinicSalvageFixed);
        pBasisSegment.HorizontalAlignment = HorizontalAlignment.Left;

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
        pClinicSalvagePersistent.Checked += (_, _) =>
            PClinicSalvageSet(lClinic.LClinicSalvage with { LWorkSalvagePersistent = true });
        pClinicSalvagePersistent.Unchecked += (_, _) =>
            PClinicSalvageSet(lClinic.LClinicSalvage with { LWorkSalvagePersistent = false });

        Children.Add(pModeLabel);
        Children.Add(pModeSegment);
        Children.Add(pBasisHeading);
        Children.Add(pBasisSegment);
        Children.Add(pClinicSalvageDescription);
        PClinicSalvageUpdate();
    }

    public CheckBox PClinicSalvageActive => pClinicSalvageActive;

    public CheckBox PClinicSalvagePersistent => pClinicSalvagePersistent;

    public void PClinicSalvageUpdate()
    {
        LWorkFixSalvage lSalvage = lClinic.LClinicSalvage;
        bool pRepair = lClinic.LClinicRepairCheck();
        Visibility = lClinic.LClinicSalvageShown ? Visibility.Visible : Visibility.Collapsed;
        pClinicSalvageActive.IsChecked = lSalvage.LWorkSalvageActive;
        pClinicSalvagePersistent.IsChecked = lSalvage.LWorkSalvagePersistent;
        pClinicSalvageSeparate.IsChecked = lSalvage.LWorkSalvageMode == LSalvageMode.LSalvageModeSeparate;
        pClinicSalvageRejoin.IsChecked = lSalvage.LWorkSalvageMode != LSalvageMode.LSalvageModeSeparate;
        pClinicSalvageFixed.IsEnabled = pRepair;
        pClinicSalvageFixed.IsChecked = lSalvage.LWorkSalvageBasis == LSalvageBasis.LSalvageBasisFixed;
        pClinicSalvageSource.IsChecked = lSalvage.LWorkSalvageBasis != LSalvageBasis.LSalvageBasisFixed;
        pClinicSalvageDescription.Text = LLocalization.LLocalizationTextRead(!pRepair
            ? "Clinic.Salvage.Basis.None"
            : lSalvage.LWorkSalvageBasis == LSalvageBasis.LSalvageBasisFixed
                ? "Clinic.Salvage.Basis.Fixed.Description"
                : "Clinic.Salvage.Basis.Source.Description");
    }

    private void PClinicSalvageSet(LWorkFixSalvage lSalvage) => lClinic.LClinicSalvageSet(lSalvage);

    private static RadioButton PClinicRadioBuild(string pKey) => new()
    {
        Content = LLocalization.LLocalizationTextRead(pKey),
        ToolTip = LLocalization.LLocalizationTextRead($"{pKey}.Tooltip"),
        FontSize = 12,
        FontFamily = pClinicSalvageFont
    };
}
