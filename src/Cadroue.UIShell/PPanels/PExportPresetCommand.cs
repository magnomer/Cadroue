using Cadroue.Infrastructure;
using Cadroue.Core;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Cadroue.Application;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PExport
{
    private void PExportPresetSync()
    {
        if (pExportPresetBusy || pPresetNameEditing is not null || pPresetDragActive)
        {
            return;
        }

        string lSelectedName = lPresetOwner.LPresetSelectionName;
        if (!string.IsNullOrEmpty(lSelectedName)
            && LPreset.LPresetNames.Any(lName => string.Equals(lName, lSelectedName, StringComparison.OrdinalIgnoreCase)))
        {
            if (pExportPresetClean)
            {
                lPresetOwner.LPresetSelectionSelect(lSelectedName);
                return;
            }

            PExportSummaryUpdate();
            return;
        }

        if (LPreset.LPresetFirstName is string lFirstName)
        {
            lPresetOwner.LPresetSelectionSelect(lFirstName);
            return;
        }

        PExportSummaryUpdate();
    }

    private void PExportPresetAdd(object sender, RoutedEventArgs e)
    {
        string lPresetName = LPreset.LPresetNameCreate(LLocalization.LLocalizationTextRead("ExportPreset.DefaultName"));
        lPresetOwner.LPresetSelectionSave(lPresetName);
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

        if (LPreset.LPresetFirstName is string lNextPresetName)
        {
            lPresetOwner.LPresetSelectionSelect(lNextPresetName);
        }
        else
        {
            PExportSummaryUpdate();
        }
    }

    private void PExportPresetSave(object sender, RoutedEventArgs e)
    {
        LPresetRecord lPresetValue = lPresetOwner.LPresetSelectionValue;
        string lPresetName = string.IsNullOrWhiteSpace(lPresetValue.LPresetName)
            ? pPresetNameSelected ?? string.Empty
            : lPresetValue.LPresetName;

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
            LPresetStore.LPresetFileSave(lPresetValue, pDialog.FileName);
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

        LPresetRecord? lImportedRecord;
        try
        {
            lImportedRecord = LPresetStore.LPresetFileLoad(pDialog.FileName);
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

        if (lImportedRecord is null)
        {
            MessageBox.Show(
                LLocalization.LLocalizationTextRead("ExportPreset.Error.Invalid"),
                LLocalization.LLocalizationTextRead("ExportPreset.Dialog.Import"),
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        string lImportedName = lImportedRecord.LPresetName.Trim();
        if (string.IsNullOrWhiteSpace(lImportedName))
        {
            lImportedName = Path.GetFileNameWithoutExtension(pDialog.FileName).Trim();
        }

        string lPresetName = LPreset.LPresetNameCreate(
            string.IsNullOrWhiteSpace(lImportedName)
                ? LLocalization.LLocalizationTextRead("ExportPreset.ImportedName")
                : lImportedName);

        lImportedRecord.LPresetName = lPresetName;
        lPresetOwner.LPresetSelectionValue = lImportedRecord;
        lPresetOwner.LPresetSelectionSave(lPresetName);
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

        lPresetOwner.LPresetSelectionSave(lPresetName);
    }

    private void PExportModificationRestore(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (pPresetNameSelected is not string)
        {
            return;
        }

        lPresetOwner.LPresetSelectionRestore();
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
        if (lCurrentPresetRename)
        {
            if (!lPresetOwner.LPresetSelectionSet(lName))
            {
                PExportPresetRebuild();
            }

            return;
        }

        var lPresetState = new LPreset();
        if (!LPreset.LPresetTryLoad(lOldPresetName, lPresetState))
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

        PExportPresetRebuild();
    }

    private void PExportDialogShow(object sender, RoutedEventArgs e)
    {
        var pButton = (Button)sender;
        LPreset pWorking = PExportWorkingRead();
        var psEncoder = new PSEncoder(pWorking, () => lPresetOwner.LPresetSelectionValue = pWorking.LPresetRecordCreate())
        {
            Owner = Window.GetWindow(pButton)
        };

        if (psEncoder.ShowDialog() == true)
        {
            lPresetOwner.LPresetSelectionValue = pWorking.LPresetRecordCreate();
        }
    }
}
