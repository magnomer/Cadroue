using System.Text;
using System.Windows;
using System.Windows.Controls;
using Cadroue.Core;
using Cadroue.Infrastructure;

using static Cadroue.UIShell.PSShared.PSField;
using static Cadroue.UIShell.PSShared.PSCombo;

namespace Cadroue.UIShell.PPanels;

internal sealed partial class PSEncoder
{
    private static HashSet<string>? psCodecAvailable;
    private static Task? psCodecProbeTask;

    internal static void PSCodecProbeStart() => psCodecProbeTask = Task.Run(PSCodecProbeRun);

    private static async Task PSCodecProbeRun()
    {
        var pAvailable = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pCandidate in LRepertoireCatalog.LRepertoireEncodersRead())
        {
            foreach (string pEncoder in pCandidate.LRepertoireTokens)
            {
                if ((await LTrial.LTrialRun(pEncoder, LTrialKind.LTrialKindVideo)).LTrialSuccess)
                {
                    pAvailable.Add(pEncoder);
                }
            }
        }

        if (pAvailable.Count > 0)
        {
            psCodecAvailable = pAvailable;
        }
    }

    private void PSCodecProbeDefer()
    {
        if (psCodecProbeTask is { IsCompleted: false } pTask)
        {
            pTask.ContinueWith(
                _ => Dispatcher.BeginInvoke(() => { if (IsLoaded) PSCodecContainerHandle(); }),
                TaskScheduler.Default);
        }
    }

    private static string[] PSCodecItemsRead() =>
        LRepertoireCatalog.LRepertoireEncodersRead()
            .Where(PSCodecAvailableCheck)
            .Select(pCandidate => pCandidate.LRepertoireText)
            .ToArray();

    private static string[] PSCodecItemsRead(string pContainer)
    {
        if (!LRepertoireCatalog.LRepertoireContainerNames.Contains(pContainer))
        {
            return PSCodecItemsRead();
        }

        return LRepertoireCatalog.LRepertoireEncodersRead()
            .Where(pCandidate => PSCodecContainerCheck(pCandidate.LRepertoireText, pContainer) && PSCodecAvailableCheck(pCandidate))
            .Select(pCandidate => pCandidate.LRepertoireText)
            .ToArray();
    }

    private static bool PSCodecAvailableCheck(LRepertoireEncoder pCandidate) =>
        psCodecAvailable is not { } pSet || pCandidate.LRepertoireTokens.Any(pSet.Contains);

    private static bool PSCodecAvailableCheck(string pText)
    {
        foreach (var pCandidate in LRepertoireCatalog.LRepertoireEncodersRead())
        {
            if (string.Equals(pCandidate.LRepertoireText, pText, StringComparison.Ordinal))
            {
                return PSCodecAvailableCheck(pCandidate);
            }
        }

        return true;
    }

    private static string[] PSCodecItemsRead(string pContainer, string pKeep)
    {
        string[] pItems = PSCodecItemsRead(pContainer);
        if (string.IsNullOrEmpty(pKeep) || pItems.Contains(pKeep))
        {
            return pItems;
        }

        bool pFits = LRepertoireCatalog.LRepertoireEncodersRead().Any(pCandidate => string.Equals(pCandidate.LRepertoireText, pKeep, StringComparison.Ordinal))
                     && (!LRepertoireCatalog.LRepertoireContainerNames.Contains(pContainer) || PSCodecContainerCheck(pKeep, pContainer));
        return pFits ? [pKeep, .. pItems] : pItems;
    }

    private void PSVideoEncoderUpdate()
    {
        bool pAvailable = PSCodecAvailableCheck(PSComboTextRead(psVideoEncoderCombo));
        psVideoEncoderNotice.Visibility = pAvailable ? Visibility.Collapsed : Visibility.Visible;
        if (!pAvailable)
        {
            psVideoEncoderNotice.Text = LLocalization.LLocalizationTextRead("Encoder.Video.Notice.Unavailable");
        }
    }

    private static bool PSCodecContainerCheck(string pText, string pContainer) =>
        LRepertoireCatalog.LRepertoireContainerCheck(pText, pContainer);

    private void PSCodecContainerHandle()
    {
        string pContainer = PSComboTextRead(psOutputContainerCombo);
        string pCurrent = psVideoEncoderCombo.SelectedItem as string ?? string.Empty;
        string[] pItems = PSCodecItemsRead(pContainer, pCurrent);
        psVideoEncoderCombo.ItemsSource = pItems;
        psVideoEncoderCombo.SelectedItem = pItems.Contains(pCurrent) ? pCurrent : pItems.FirstOrDefault();
        PSVideoEncoderUpdate();
    }

    private static string PSCodecValueRead(string pText)
    {
        foreach (var pCandidate in LRepertoireCatalog.LRepertoireEncodersRead())
        {
            if (string.Equals(pCandidate.LRepertoireText, pText, StringComparison.Ordinal))
            {
                return pCandidate.LRepertoireTokens.FirstOrDefault() ?? string.Empty;
            }
        }

        return string.Empty;
    }

    private async Task PSCodecVerifyHandle(ComboBox pCombo, Button pButton, IProgress<double> pFeed)
    {
        string pSelected = pCombo.SelectedItem as string ?? string.Empty;
        pButton.IsEnabled = false;
        pButton.Content = LLocalization.LLocalizationTextRead("Encoder.Verification.Checking");
        var pAvailable = new List<string>();
        var pAvailableNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pRows = new List<PSVerdictRow>();
        int pTotal = LRepertoireCatalog.LRepertoireEncodersRead().Sum(pCandidate => pCandidate.LRepertoireTokens.Count);
        int pDone = 0;
        foreach (var pCandidate in LRepertoireCatalog.LRepertoireEncodersRead())
        {
            bool pCandidateAvailable = false;
            foreach (string pEncoder in pCandidate.LRepertoireTokens)
            {
                LTrialResult pResult = await LTrial.LTrialRun(pEncoder, LTrialKind.LTrialKindVideo);
                pCandidateAvailable |= pResult.LTrialSuccess;
                if (pResult.LTrialSuccess)
                {
                    pAvailableNames.Add(pEncoder);
                }

                pRows.Add(new PSVerdictRow(pCandidate.LRepertoireText, pEncoder, pResult.LTrialSuccess, pResult.LTrialMessage));
                pDone++;
                pFeed.Report(pTotal == 0 ? 1 : (double)pDone / pTotal);
            }

            if (pCandidateAvailable)
            {
                pAvailable.Add(pCandidate.LRepertoireText);
            }
        }

        psCodecAvailable = pAvailableNames.Count > 0 ? pAvailableNames : psCodecAvailable;
        if (!pAvailable.Contains(pSelected)
            && LRepertoireCatalog.LRepertoireEncodersRead().Any(pCandidate => string.Equals(pCandidate.LRepertoireText, pSelected, StringComparison.Ordinal)))
        {
            pAvailable.Insert(0, pSelected);
        }

        pCombo.ItemsSource = pAvailable;
        pCombo.SelectedItem = pAvailable.Contains(pSelected) ? pSelected : pAvailable.FirstOrDefault();
        PSVideoEncoderUpdate();
        psCodecResults = pRows;
        PSVerdictLogRecord("video", pRows);
        pButton.Content = LLocalization.LLocalizationTextRead("Encoder.Button.Verify");
        pButton.IsEnabled = true;
    }

    private static void PSVerdictLogRecord(string pKind, IReadOnlyList<PSVerdictRow> pRows)
    {
        int pPassed = pRows.Count(pRow => pRow.PSVerdictSuccess);
        var pDetail = new StringBuilder();
        foreach (PSVerdictRow pRow in pRows)
        {
            pDetail.AppendLine($"{pRow.PSVerdictFamily} / {pRow.PSVerdictEncoder}: {(pRow.PSVerdictSuccess ? "OK" : "FAIL")} - {pRow.PSVerdictMessage}");
        }

        LTraceLog.LTraceInfoRecord(
            $"Encoder verification ({pKind}): {pPassed} of {pRows.Count} encoder(s) available",
            pDetail.ToString().TrimEnd());
    }
}
