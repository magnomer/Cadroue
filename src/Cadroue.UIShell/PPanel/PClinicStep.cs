using System.Windows;
using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.UIShell.PPanel;

public sealed partial class PClinic
{
    public void PClinicSourceSet(string? pClinicSourcePath)
    {
        pClinicSource = string.IsNullOrWhiteSpace(pClinicSourcePath) ? null : pClinicSourcePath;
        PClinicResultApply();
    }

    public void PClinicStepShow(string? pStepName)
    {
        pClinicSalvageShown = pStepName == "Salvage";
        pClinicSalvage.PClinicSalvageShow(pClinicSalvageShown);
        if (pClinicSalvageShown)
        {
            pClinicSalvage.PClinicSalvageUpdate(
                pClinicStates.Values.Any(pState => pState.PClinicStateActive));
        }
        pClinicApplyBox.Visibility = pClinicSalvageShown ? Visibility.Collapsed : Visibility.Visible;
        pClinicPersistentBox.Visibility = pClinicSalvageShown ? Visibility.Collapsed : Visibility.Visible;
        pClinicDiagnosisButton.Visibility = pClinicSalvageShown ? Visibility.Collapsed : Visibility.Visible;
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
            PClinicProgressSet(false);
            pClinicTitleLabel.Text = LLocalization.LLocalizationTextRead("Processing.Step.Salvage");
            return;
        }

        pClinicToggleRow.Visibility = Visibility.Visible;
        LFlawKind? pKind = pClinicKinds
            .Where(pEntry => pEntry.PClinicKindName == pStepName)
            .Select(pEntry => (LFlawKind?)pEntry.PClinicKindValue)
            .FirstOrDefault();
        pClinicCurrentKind = pKind;
        bool pKnown = pKind is not null;
        pClinicEmptyNotice.Visibility = pKnown ? Visibility.Collapsed : Visibility.Visible;
        pClinicItemBody.Visibility = pKnown ? Visibility.Visible : Visibility.Collapsed;
        pClinicToggleRow.IsEnabled = pKnown;
        pClinicPersistentBox.IsEnabled = pKnown;

        pClinicSuppress = true;
        if (pKind is { } pShownKind && pClinicStates.TryGetValue(pShownKind, out PClinicState pState))
        {
            pClinicApplyBox.IsChecked = pState.PClinicStateActive;
            pClinicPersistentBox.IsChecked = pState.PClinicStatePersistent;
        }
        else
        {
            pClinicApplyBox.IsChecked = false;
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
            (bool pApply, bool pPersistent) =
                pClinicStates.TryGetValue(pKind, out PClinicState pState)
                    ? pState
                    : new PClinicState(false, false);
            pSteps.Add(new LWorkFixStep(pKind, pApply, pPersistent));
        }

        return new LWorkFix(pSteps) { LWorkFixSalvage = pClinicSalvage.PClinicSalvageRead() };
    }

    public void PClinicPlanApply(LWorkFix pClinicPlan)
    {
        pClinicSalvage.PClinicSalvageApply(pClinicPlan.LWorkFixSalvage);
        foreach (LWorkFixStep pStep in pClinicPlan.LWorkFixSteps)
        {
            pClinicStates[pStep.LWorkFixKind] =
                new PClinicState(pStep.LWorkFixRepair, pStep.LWorkFixPersistent);
        }

        if (pClinicSalvageShown)
        {
            pClinicSalvage.PClinicSalvageUpdate(
                pClinicStates.Values.Any(pState => pState.PClinicStateActive));
        }

        if (pClinicCurrentKind is { } pKind
            && pClinicStates.TryGetValue(pKind, out PClinicState pCurrent))
        {
            pClinicSuppress = true;
            pClinicApplyBox.IsChecked = pCurrent.PClinicStateActive;
            pClinicPersistentBox.IsChecked = pCurrent.PClinicStatePersistent;
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

        pClinicStates[pKind] = new PClinicState(
            pClinicApplyBox.IsChecked == true,
            pClinicPersistentBox.IsChecked == true);
        PClinicPlanChange?.Invoke();
    }

    private void PClinicPersistentHandle()
    {
        if (pClinicSuppress || pClinicCurrentKind is not { } pKind)
        {
            return;
        }

        pClinicStates[pKind] = new PClinicState(
            pClinicApplyBox.IsChecked == true,
            pClinicPersistentBox.IsChecked == true);
        PClinicPlanChange?.Invoke();
    }
}
