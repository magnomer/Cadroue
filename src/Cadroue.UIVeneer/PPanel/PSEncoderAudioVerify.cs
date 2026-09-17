using System.Windows.Controls;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.Application;

namespace Cadroue.UIVeneer.PPanel;

internal sealed partial class PSEncoder
{
    private async Task PSAudioVerifyHandle(ComboBox pCombo, Button pButton, IProgress<double> pFeed)
    {
        string pSelected = pCombo.SelectedItem as string ?? string.Empty;
        pButton.IsEnabled = false;
        pButton.Content = LLocalization.LLocalizationTextRead("Encoder.Verification.Checking");
        LInventory.LInventoryReset();
        var pAvailable = new List<string>();
        var pRows = new List<PSVerdictRow>();
        int pTotal = PSAudioCandidates.Length;
        int pDone = 0;
        foreach (var pCandidate in PSAudioCandidates)
        {
            LTrialResult pResult = await LTrial.LTrialRun(pCandidate.PSAudioName, LTrialKind.LTrialKindAudio);
            pRows.Add(new PSVerdictRow(
                pCandidate.PSAudioText,
                pCandidate.PSAudioName,
                pResult.LTrialSuccess,
                pResult.LTrialMessage));
            if (pResult.LTrialSuccess)
            {
                pAvailable.Add(pCandidate.PSAudioText);
            }

            pDone++;
            pFeed.Report(pTotal == 0 ? 1 : (double)pDone / pTotal);
        }

        if (!pAvailable.Contains(pSelected)
            && PSAudioCandidates.Any(pCandidate => string.Equals(
                pCandidate.PSAudioText,
                pSelected,
                StringComparison.Ordinal)))
        {
            pAvailable.Insert(0, pSelected);
        }

        pCombo.ItemsSource = pAvailable;
        pCombo.SelectedItem = pAvailable.Contains(pSelected) ? pSelected : pAvailable.FirstOrDefault();
        PSAudioEncoderUpdate();
        psAudioResults = pRows;
        PSVerdictLogRecord("audio", pRows);
        pButton.Content = LLocalization.LLocalizationTextRead("Encoder.Button.Verify");
        pButton.IsEnabled = true;
    }
}
