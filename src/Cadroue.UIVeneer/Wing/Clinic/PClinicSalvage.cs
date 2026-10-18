using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

public sealed class PClinicSalvage : StackPanel
{
    private static readonly FontFamily pClinicSalvageFont = new("Segoe UI");

    private readonly QClinic lClinic;
    private CheckBox pClinicSalvageActive = null!;
    private CheckBox pClinicSalvagePersistent = null!;
    private RadioButton pClinicSalvageRejoin = null!;
    private RadioButton pClinicSalvageSeparate = null!;
    private RadioButton pClinicSalvageSource = null!;
    private RadioButton pClinicSalvageFixed = null!;
    private TextBlock pClinicSalvageDescription = null!;

    public PClinicSalvage(QClinic lClinicOwner)
    {
        lClinic = lClinicOwner;
        PClinicSalvageBuild();
        PClinicSalvageAttach();
        PClinicSalvageUpdate();
    }

    private void PClinicSalvageBuild()
    {
        Visibility = Visibility.Collapsed;

        pClinicSalvageActive = PClinic.PClinicSwitchBuild(
            QLocalizationCatalog.QLocalizationTextRead("Clinic.Salvage.Apply"),
            QLocalizationCatalog.QLocalizationTextRead("Clinic.Salvage.Apply.Tooltip"));

        var pModeLabel = new TextBlock
        {
            Text = QLocalizationCatalog.QLocalizationTextRead("Clinic.Salvage.Mode.Label"),
            FontSize = 12,
            FontFamily = pClinicSalvageFont,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(0x26, 0x36, 0x4A)),
            Margin = new Thickness(0, 12, 0, 6)
        };

        pClinicSalvageRejoin = PClinicRadioBuild("Clinic.Salvage.Mode.Rejoin");
        pClinicSalvageSeparate = PClinicRadioBuild("Clinic.Salvage.Mode.Separate");
        Border pModeSegment = QRadio.QRadioSegmentBuild(pClinicSalvageRejoin, pClinicSalvageSeparate);
        pModeSegment.HorizontalAlignment = HorizontalAlignment.Left;

        var pBasisHeading = new TextBlock
        {
            Text = QLocalizationCatalog.QLocalizationTextRead("Clinic.Salvage.Basis.Label"),
            FontSize = 12,
            FontFamily = pClinicSalvageFont,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(0x26, 0x36, 0x4A)),
            Margin = new Thickness(0, 14, 0, 6)
        };

        pClinicSalvageSource = PClinicRadioBuild("Clinic.Salvage.Basis.Source");
        pClinicSalvageFixed = PClinicRadioBuild("Clinic.Salvage.Basis.Fixed");
        Border pBasisSegment = QRadio.QRadioSegmentBuild(pClinicSalvageSource, pClinicSalvageFixed);
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
            QLocalizationCatalog.QLocalizationTextRead("Clinic.Salvage.Persistent"),
            QLocalizationCatalog.QLocalizationTextRead("Clinic.Salvage.Persistent.Tooltip"));

        Children.Add(pModeLabel);
        Children.Add(pModeSegment);
        Children.Add(pBasisHeading);
        Children.Add(pBasisSegment);
        Children.Add(pClinicSalvageDescription);
    }

    private void PClinicSalvageAttach() => lClinic.QClinicSalvage.QClinicSalvageAttach(
        pClinicSalvageActive,
        pClinicSalvagePersistent,
        pClinicSalvageRejoin,
        pClinicSalvageSeparate,
        pClinicSalvageSource,
        pClinicSalvageFixed);

    public CheckBox PClinicSalvageActive => pClinicSalvageActive;

    public CheckBox PClinicSalvagePersistent => pClinicSalvagePersistent;

    public void PClinicSalvageUpdate() => lClinic.QClinicSalvage.QClinicSalvageUpdate(
        this,
        pClinicSalvageActive,
        pClinicSalvagePersistent,
        pClinicSalvageRejoin,
        pClinicSalvageSeparate,
        pClinicSalvageSource,
        pClinicSalvageFixed,
        pClinicSalvageDescription);

    private static RadioButton PClinicRadioBuild(string pKey) => new()
    {
        Content = QLocalizationCatalog.QLocalizationTextRead(pKey),
        ToolTip = QLocalizationCatalog.QLocalizationTextRead($"{pKey}.Tooltip"),
        FontSize = 12,
        FontFamily = pClinicSalvageFont
    };
}
