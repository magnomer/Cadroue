using System.Windows;
using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.UIVeneer.PWing;

public sealed partial class PClinic
{
    public void PClinicSourceSet(string? pClinicSourcePath) => LClinic.LClinicSourceSet(pClinicSourcePath);

    public void PClinicStepShow(string? pStepName) => LClinic.LClinicStepSet(pStepName);

    public LWorkFix PClinicPlanRead() => LClinic.LClinicPlanRead();

    public void PClinicPlanApply(LWorkFix pClinicPlan) => LClinic.LClinicPlanApply(pClinicPlan);

    private void PClinicUpdate()
    {
        bool pSalvage = LClinic.LClinicSalvageShown;
        Visibility pSalvageVisible = pSalvage ? Visibility.Visible : Visibility.Collapsed;
        Visibility pStepVisible = pSalvage ? Visibility.Collapsed : Visibility.Visible;
        pClinicSalvage.PClinicSalvageUpdate();
        pClinicApplyBox.Visibility = pStepVisible;
        pClinicPersistentBox.Visibility = pStepVisible;
        pClinicDiagnosisButton.Visibility = pStepVisible;
        pClinicSalvage.PClinicSalvageActive.Visibility = pSalvageVisible;
        pClinicSalvage.PClinicSalvagePersistent.Visibility = pSalvageVisible;
        pClinicToggleRow.Visibility = Visibility.Visible;

        string? pStepName = LClinic.LClinicStep;
        bool pKnown = pSalvage || LClinic.LClinicKind is not null;
        pClinicEmptyNotice.Visibility = pKnown ? Visibility.Collapsed : Visibility.Visible;
        pClinicItemBody.Visibility = pKnown ? Visibility.Visible : Visibility.Collapsed;
        pClinicToggleRow.IsEnabled = pKnown;
        pClinicPersistentBox.IsEnabled = pKnown && !pSalvage;

        LWorkFixStep lStep = LClinic.LClinicStepRead();
        bool pShown = LClinic.LClinicKind is not null;
        pClinicApplyBox.IsChecked = pShown && lStep.LWorkFixRepair;
        pClinicPersistentBox.IsChecked = pShown && lStep.LWorkFixPersistent;

        PClinicResultApply(pShown);
        pClinicTitleLabel.Text = LLocalization.LLocalizationTextRead(pKnown
            ? $"Processing.Step.{pStepName}"
            : "Clinic.Header.Title");
        if (pKnown)
        {
            pClinicItemSimple.Text = LLocalization.LLocalizationTextRead($"Clinic.Step.{pStepName}.Simple");
            pClinicItemTechnical.Text = LLocalization.LLocalizationTextRead($"Clinic.Step.{pStepName}.Technical");
        }
    }
}
