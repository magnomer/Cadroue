using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.Core;
using Cadroue.Application;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PWing;

public sealed class PClinicSalvage : StackPanel
{
    private static readonly FontFamily pClinicSalvageFont = new("Segoe UI");

    private readonly QClinic lClinic;
    private readonly CheckBox pClinicSalvageActive;
    private readonly CheckBox pClinicSalvagePersistent;
    private readonly RadioButton pClinicSalvageRejoin;
    private readonly RadioButton pClinicSalvageSeparate;
    private readonly RadioButton pClinicSalvageSource;
    private readonly RadioButton pClinicSalvageFixed;
    private readonly TextBlock pClinicSalvageDescription;

    public PClinicSalvage(QClinic lClinicOwner)
    {
        lClinic = lClinicOwner;
        Visibility = Visibility.Collapsed;

        pClinicSalvageActive = PClinic.PClinicSwitchBuild(
            LLocalization.LLocalizationTextRead("Clinic.Salvage.Apply"),
            LLocalization.LLocalizationTextRead("Clinic.Salvage.Apply.Tooltip"));
        pClinicSalvageActive.Checked += (_, _) => lClinic.QSalvageActiveSet(true);
        pClinicSalvageActive.Unchecked += (_, _) => lClinic.QSalvageActiveSet(false);

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
        pClinicSalvageRejoin.Checked += (_, _) => lClinic.QSalvageModeSet(LSalvageMode.LSalvageModeRejoin);
        pClinicSalvageSeparate.Checked += (_, _) => lClinic.QSalvageModeSet(LSalvageMode.LSalvageModeSeparate);
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
        pClinicSalvageSource.Checked += (_, _) => lClinic.QSalvageBasisSet(LSalvageBasis.LSalvageBasisSource);
        pClinicSalvageFixed.Checked += (_, _) => lClinic.QSalvageBasisSet(LSalvageBasis.LSalvageBasisFixed);
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
        pClinicSalvagePersistent.Checked += (_, _) => lClinic.QSalvagePersistentSet(true);
        pClinicSalvagePersistent.Unchecked += (_, _) => lClinic.QSalvagePersistentSet(false);

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
        Visibility = PLook.PLookVisible[lClinic.QClinicSalvageShown];
        pClinicSalvageActive.IsChecked = lClinic.QClinicSalvageActive;
        pClinicSalvagePersistent.IsChecked = lClinic.QClinicSalvagePersistent;
        pClinicSalvageSeparate.IsChecked = lClinic.QClinicSalvageSeparate;
        pClinicSalvageRejoin.IsChecked = lClinic.QClinicSalvageRejoin;
        pClinicSalvageFixed.IsEnabled = lClinic.QClinicRepairCheck();
        pClinicSalvageFixed.IsChecked = lClinic.QClinicSalvageFixed;
        pClinicSalvageSource.IsChecked = lClinic.QClinicSalvageSource;
        pClinicSalvageDescription.Text = lClinic.QClinicSalvageText;
    }

    private static RadioButton PClinicRadioBuild(string pKey) => new()
    {
        Content = LLocalization.LLocalizationTextRead(pKey),
        ToolTip = LLocalization.LLocalizationTextRead($"{pKey}.Tooltip"),
        FontSize = 12,
        FontFamily = pClinicSalvageFont
    };
}
