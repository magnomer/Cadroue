using Cadroue.Infrastructure;
using Cadroue.Core;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PExport
{
    private void PExportPresetApply()
    {
        if (pExportPresetBusy || pPresetNameSelected is not string lPresetName)
        {
            return;
        }

        if (LPreset.LPresetTryLoad(lPresetName, lExportSpecificState))
        {
            PExportSummaryUpdate();
        }
    }

    private void PExportPresetSync()
    {
        if (pExportPresetBusy || pPresetNameEditing is not null)
        {
            return;
        }

        if (pPresetNameSelected is string lPresetName
            && LPreset.LPresetNames.Any(lName => string.Equals(lName, lPresetName, StringComparison.OrdinalIgnoreCase)))
        {
            if (pExportPresetClean)
            {
                pExportPresetBusy = true;
                LPreset.LPresetTryLoad(lPresetName, lExportSpecificState);
                pExportPresetBusy = false;
            }

            PExportSummaryUpdate();
            return;
        }

        pExportPresetBusy = true;
        if (LPreset.LPresetFirstName is string lFirstName && LPreset.LPresetTryLoad(lFirstName, lExportSpecificState))
        {
            pPresetNameSelected = lFirstName;
        }

        pExportPresetBusy = false;
        PExportSummaryUpdate();
    }

    private void PExportPresetAdd(object sender, RoutedEventArgs e)
    {
        string lPresetName = PExportNameCreate(LLocalization.LLocalizationTextRead("ExportPreset.DefaultName"));
        lExportSpecificState.LPresetName = lPresetName;
        LPreset.LPresetSave(lPresetName, lExportSpecificState);
        pExportPresetBusy = true;
        pPresetNameSelected = lPresetName;
        pExportPresetBusy = false;
        PExportSummaryUpdate();
    }

    private void PExportPresetDelete(object sender, RoutedEventArgs e)
    {
        if (pPresetNameSelected is not string lPresetName)
        {
            return;
        }

        if (LPreset.LPresetNativeCheck(lPresetName))
        {
            return;
        }

        if (!LPreset.LPresetDelete(lPresetName))
        {
            return;
        }

        string? lNextPresetName = LPreset.LPresetFirstName;
        pExportPresetBusy = true;
        if (lNextPresetName is not null && LPreset.LPresetTryLoad(lNextPresetName, lExportSpecificState))
        {
            pPresetNameSelected = lNextPresetName;
        }
        else
        {
            lExportSpecificState.LPresetName = string.Empty;
            pPresetNameSelected = null;
        }

        pExportPresetBusy = false;
        PExportSummaryUpdate();
    }

    private void PExportPresetSave(object sender, RoutedEventArgs e)
    {
        string lPresetName = string.IsNullOrWhiteSpace(lExportSpecificState.LPresetName)
            ? pPresetNameSelected ?? string.Empty
            : lExportSpecificState.LPresetName;

        char[] pInvalidCharacters = Path.GetInvalidFileNameChars();
        string pFileName = new string(lPresetName
            .Trim()
            .Select(pCharacter => pInvalidCharacters.Contains(pCharacter) ? '_' : pCharacter)
            .ToArray());

        var pDialog = new SaveFileDialog
        {
            Title = LLocalization.LLocalizationTextRead("ExportPreset.Dialog.Export"),
            Filter = LLocalization.LLocalizationTextRead("ExportPreset.Dialog.Filter"),
            DefaultExt = "json",
            AddExtension = true,
            FileName = string.IsNullOrWhiteSpace(pFileName)
                ? LLocalization.LLocalizationTextRead("ExportPreset.Dialog.DefaultFile")
                : $"{pFileName}.json"
        };

        if (pDialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            LPresetStore.LPresetFileSave(lExportSpecificState.LPresetRecordCreate(), pDialog.FileName);
        }
        catch (Exception pError)
        {
            MessageBox.Show(
                LLocalization.LLocalizationFormat("ExportPreset.Error.Write", pError.Message),
                LLocalization.LLocalizationTextRead("ExportPreset.Dialog.Export"),
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void PExportPresetLoad(object sender, RoutedEventArgs e)
    {
        var pDialog = new OpenFileDialog
        {
            Title = LLocalization.LLocalizationTextRead("ExportPreset.Dialog.Import"),
            Filter = LLocalization.LLocalizationTextRead("ExportPreset.Dialog.Filter"),
            DefaultExt = "json",
            CheckFileExists = true
        };

        if (pDialog.ShowDialog() != true)
        {
            return;
        }

        LPreset? lImportedPreset;
        try
        {
            LPresetRecord? lImportedRecord = LPresetStore.LPresetFileLoad(pDialog.FileName);
            lImportedPreset = lImportedRecord is null ? null : LPreset.LPresetStateCreate(lImportedRecord);
        }
        catch (Exception pError)
        {
            MessageBox.Show(
                LLocalization.LLocalizationFormat("ExportPreset.Error.Read", pError.Message),
                LLocalization.LLocalizationTextRead("ExportPreset.Dialog.Import"),
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        if (lImportedPreset is null)
        {
            MessageBox.Show(
                LLocalization.LLocalizationTextRead("ExportPreset.Error.Invalid"),
                LLocalization.LLocalizationTextRead("ExportPreset.Dialog.Import"),
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        string lImportedName = lImportedPreset.LPresetName.Trim();
        if (string.IsNullOrWhiteSpace(lImportedName))
        {
            lImportedName = Path.GetFileNameWithoutExtension(pDialog.FileName).Trim();
        }

        string lPresetName = PExportNameCreate(
            string.IsNullOrWhiteSpace(lImportedName)
                ? LLocalization.LLocalizationTextRead("ExportPreset.ImportedName")
                : lImportedName);

        lExportSpecificState.LPresetCopy(lImportedPreset);
        lExportSpecificState.LPresetName = lPresetName;
        LPreset.LPresetSave(lPresetName, lExportSpecificState);
        pExportPresetBusy = true;
        pPresetNameSelected = lPresetName;
        pExportPresetBusy = false;
        PExportSummaryUpdate();
    }

    private void PExportModificationApply(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (pPresetNameSelected is not string lPresetName)
        {
            return;
        }

        if (LPreset.LPresetNativeCheck(lPresetName))
        {
            return;
        }

        lExportSpecificState.LPresetName = lPresetName;
        LPreset.LPresetSave(lPresetName, lExportSpecificState);
        PExportSummaryUpdate();
    }

    private void PExportModificationRestore(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (pPresetNameSelected is not string lPresetName)
        {
            return;
        }

        if (LPreset.LPresetTryLoad(lPresetName, lExportSpecificState))
        {
            PExportSummaryUpdate();
        }
    }

    private void PExportNameCommit(string lOldPresetName, string lNewPresetName)
    {
        if (!string.Equals(pPresetNameEditing, lOldPresetName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        pPresetNameEditing = null;
        pExportBoxCurrent = null;
        if (LPreset.LPresetNativeCheck(lOldPresetName))
        {
            PExportPresetRebuild();
            return;
        }

        string lName = lNewPresetName.Trim();
        if (string.IsNullOrWhiteSpace(lName) || string.Equals(lOldPresetName, lName, StringComparison.OrdinalIgnoreCase))
        {
            PExportPresetRebuild();
            return;
        }

        if (LPreset.LPresetNames.Any(lExisting => string.Equals(lExisting, lName, StringComparison.OrdinalIgnoreCase)))
        {
            PExportPresetRebuild();
            return;
        }

        bool lCurrentPresetRename = string.Equals(pPresetNameSelected, lOldPresetName, StringComparison.OrdinalIgnoreCase);
        var lPresetState = new LPreset();
        if (lCurrentPresetRename)
        {
            lPresetState.LPresetCopy(lExportSpecificState);
        }
        else if (!LPreset.LPresetTryLoad(lOldPresetName, lPresetState))
        {
            PExportPresetRebuild();
            return;
        }

        lPresetState.LPresetName = lName;
        if (!LPreset.LPresetNameSet(lOldPresetName, lName, lPresetState))
        {
            PExportPresetRebuild();
            return;
        }

        if (lCurrentPresetRename)
        {
            lExportSpecificState.LPresetName = lName;
            pPresetNameSelected = lName;
            PExportSummaryUpdate();
        }
        else
        {
            PExportPresetRebuild();
        }
    }

    private void PExportDialogShow(object sender, RoutedEventArgs e)
    {
        var pButton = (Button)sender;
        var psEncoder = new PSEncoder(lExportSpecificState, PExportSummaryUpdate)
        {
            Owner = Window.GetWindow(pButton)
        };

        if (psEncoder.ShowDialog() == true)
        {
            PExportSummaryUpdate();
        }
    }

    private static string PExportNameCreate(string pBaseName)
    {
        if (!LPreset.LPresetNames.Any(lName => string.Equals(lName, pBaseName, StringComparison.OrdinalIgnoreCase)))
        {
            return pBaseName;
        }

        for (int lIndex = 2; ; lIndex++)
        {
            string lCandidate = $"{pBaseName} {lIndex}";
            if (!LPreset.LPresetNames.Any(lName => string.Equals(lName, lCandidate, StringComparison.OrdinalIgnoreCase)))
            {
                return lCandidate;
            }
        }
    }
}
