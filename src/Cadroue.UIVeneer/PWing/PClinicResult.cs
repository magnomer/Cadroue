using System.Windows;
using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.UIVeneer.PWing;

public sealed partial class PClinic
{
    public void PClinicResultsRemove(IReadOnlyList<string> pClinicPaths) => LClinic.LClinicResultsRemove(pClinicPaths);

    public void PClinicResultShow(
        string pClinicResultPath,
        LFlawKind pClinicResultKind,
        LCheckupResult pClinicResult) =>
        LClinic.LClinicResultSet(pClinicResultPath, pClinicResultKind, pClinicResult);

    public void PClinicProgressShow(string pClinicProgressPath, double pClinicProgressValue) =>
        LClinic.LClinicProgressSet(pClinicProgressPath, pClinicProgressValue);

    private void PClinicResultApply(bool pKnown)
    {
        bool pVisible = !LClinic.LClinicSalvageShown && pKnown;
        pClinicResultBody.Visibility = pVisible ? Visibility.Visible : Visibility.Collapsed;
        bool pScanning = pVisible && LClinic.LClinicScanCheck();
        pClinicDiagnosisProgress.Value = pScanning ? LClinic.LClinicProgressRead() : 0;
        pClinicDiagnosisProgress.Visibility = pScanning ? Visibility.Visible : Visibility.Collapsed;
        if (pVisible)
        {
            pClinicResultText.Text = LCheckupFormat.LCheckupBodyFormat(
                LClinic.LClinicResultRead(), PClinicStringsRead());
        }
    }

    private static LCheckupStrings PClinicStringsRead() => new(
        LLocalization.LLocalizationTextRead("Clinic.Result.Empty"),
        LLocalization.LLocalizationTextRead("Clinic.Result.Scanning"),
        LLocalization.LLocalizationTextRead("Clinic.Result.Clean"),
        LLocalization.LLocalizationTextRead("Clinic.Result.Failed"),
        LLocalization.LLocalizationTextRead("Clinic.Result.Defect"),
        LLocalization.LLocalizationTextRead("Clinic.Result.Evidence"),
        LLocalization.LLocalizationTextRead("Clinic.Result.Repair"));
}
