using System.Windows;
using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.UIVeneer.PPanel;

public sealed partial class PClinic
{
    public void PClinicResultsRemove(IReadOnlyList<string> pClinicPaths)
    {
        foreach (string pClinicPath in pClinicPaths)
        {
            foreach ((LFlawKind pKind, string _) in pClinicKinds)
            {
                pClinicResults.Remove(new PClinicKey(pClinicPath, pKind));
            }
        }
    }

    public void PClinicResultShow(string pClinicResultPath, LFlawKind pClinicResultKind, LCheckupResult pClinicResult)
    {
        pClinicResults[new PClinicKey(pClinicResultPath, pClinicResultKind)] = pClinicResult;
        if (pClinicResult.LCheckupOutcome == LCheckupOutcome.LCheckupOutcomeScanning)
        {
            pClinicProgress[pClinicResultPath] = 0;
        }
        else
        {
            pClinicProgress.Remove(pClinicResultPath);
        }

        if (pClinicResultPath == pClinicSource && pClinicCurrentKind == pClinicResultKind)
        {
            PClinicResultApply();
        }
    }

    public void PClinicProgressShow(string pClinicProgressPath, double pClinicProgressValue)
    {
        double pClinicValue = Math.Clamp(pClinicProgressValue, 0, 1);
        pClinicProgress[pClinicProgressPath] = pClinicValue;
        if (string.Equals(pClinicProgressPath, pClinicSource, StringComparison.OrdinalIgnoreCase)
            && pClinicDiagnosisProgress.Visibility == Visibility.Visible)
        {
            pClinicDiagnosisProgress.Value = pClinicValue;
        }
    }

    private void PClinicResultApply()
    {
        bool pVisible = !pClinicSalvageShown
            && pClinicCurrentKind is not null
            && pClinicItemBody.Visibility == Visibility.Visible;
        pClinicResultBody.Visibility = pVisible ? Visibility.Visible : Visibility.Collapsed;
        bool pScanning = pVisible
            && pClinicSource is { } pSource
            && pClinicCurrentKind is { } pKind
            && pClinicResults.TryGetValue(new PClinicKey(pSource, pKind), out LCheckupResult pResult)
            && pResult.LCheckupOutcome == LCheckupOutcome.LCheckupOutcomeScanning;
        double pProgress = pScanning
            && pClinicSource is { } pProgressSource
            && pClinicProgress.TryGetValue(pProgressSource, out double pStoredProgress)
                ? pStoredProgress
                : 0;
        PClinicProgressSet(pScanning, pProgress);
        if (pVisible)
        {
            pClinicResultText.Text = PClinicResultResolve();
        }
    }

    private void PClinicProgressSet(bool pClinicDiagnosisRunning, double pClinicProgressValue = 0)
    {
        pClinicDiagnosisProgress.Value = Math.Clamp(pClinicProgressValue, 0, 1);
        pClinicDiagnosisProgress.Visibility = pClinicDiagnosisRunning
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private string PClinicResultResolve()
    {
        if (pClinicCurrentKind is not { } pKind)
        {
            return LLocalization.LLocalizationTextRead("Clinic.Result.Empty");
        }

        LCheckupResult pResult = pClinicSource is { } pSource
            && pClinicResults.TryGetValue(new PClinicKey(pSource, pKind), out LCheckupResult pStored)
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
}
