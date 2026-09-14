using Cadroue.Infrastructure;
using Cadroue.UIShell.PSCasement;
using Cadroue.Core;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using Cadroue.Application;

namespace Cadroue.UIShell.PPanel;

public sealed partial class PExport
{
    private void PExportPresetSync()
    {
        if (pExportPresetBusy || pPresetNameEditing is not null || pPresetDragActive)
        {
            return;
        }

        PExportSummaryUpdate();
    }

    private void PExportPresetAdd(object sender, RoutedEventArgs e)
    {
        string lPresetName = LPreset.LPresetNameCreate(LLocalization.LLocalizationTextRead("ExportPreset.DefaultName"));
        PExportPresetSave(lPresetName);
    }

    private void PExportPresetSave(string lPresetName)
    {
        if (!lPresetOwner.LPresetSelectionSave(lPresetName))
        {
            PExportFailureShow(lPresetName);
        }
    }

    private void PExportFailureShow(string lPresetName) =>
        PSWarning.PSWarningShow(
            Window.GetWindow(this),
            LLocalization.LLocalizationTextRead("ExportPreset.Dialog.Export"),
            LLocalization.LLocalizationFormat("ExportPreset.Error.Write", lPresetName));

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
            PExportFailureShow(lPresetName);
        }
    }

    private void PExportPresetSave(object sender, RoutedEventArgs e)
    {
        LPresetRecord lPresetValue = lPresetOwner.LPresetSelectionValue;
        string lPresetName = string.IsNullOrWhiteSpace(lPresetValue.LPresetName)
            ? pPresetNameSelected ?? string.Empty
            : lPresetValue.LPresetName;

        string pFileName = LPreset.LPresetFileFormat(lPresetName);

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

        if (LPresetStore.LPresetCatalogCheck(pDialog.FileName))
        {
            PSWarning.PSWarningShow(
                Window.GetWindow(this),
                LLocalization.LLocalizationTextRead("ExportPreset.Dialog.Export"),
                LLocalization.LLocalizationTextRead("ExportPreset.Error.Catalogue"));
            return;
        }

        if (!LPresetStore.LPresetFileSave(lPresetValue, pDialog.FileName))
        {
            PExportFailureShow(pDialog.FileName);
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
            PSWarning.PSWarningShow(
                Window.GetWindow(this),
                LLocalization.LLocalizationTextRead("ExportPreset.Dialog.Import"),
                LLocalization.LLocalizationFormat("ExportPreset.Error.Read", pError.Message));
            return;
        }

        if (lImportedRecord is null)
        {
            PSWarning.PSWarningShow(
                Window.GetWindow(this),
                LLocalization.LLocalizationTextRead("ExportPreset.Dialog.Import"),
                LLocalization.LLocalizationTextRead("ExportPreset.Error.Invalid"));
            return;
        }

        string lImportedName = LPreset.LPresetNameResolve(lImportedRecord.LPresetName, pDialog.FileName);

        string lPresetName = LPreset.LPresetNameCreate(
            string.IsNullOrWhiteSpace(lImportedName)
                ? LLocalization.LLocalizationTextRead("ExportPreset.ImportedName")
                : lImportedName);

        lImportedRecord.LPresetName = lPresetName;
        if (!LPreset.LPresetSave(lPresetName, LPreset.LPresetStateCreate(lImportedRecord)))
        {
            PExportFailureShow(lPresetName);
            return;
        }

        lPresetOwner.LPresetSelectionSelect(lPresetName);
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

        PExportPresetSave(lPresetName);
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
        lPresetOwner.LPresetSelectionCommit(lOldPresetName, lNewPresetName);
        PExportPresetSync();
    }

    private void PExportDialogShow(object sender, RoutedEventArgs e)
    {
        var pButton = (Button)sender;
        LPreset pWorking = PExportWorkingRead();
        var psEncoder = new PSEncoder(
            pWorking,
            () => lPresetOwner.LPresetSelectionValue = pWorking.LPresetRecordCreate(),
            pExportSmartAllowed)
        {
            Owner = Window.GetWindow(pButton)
        };

        if (psEncoder.ShowDialog() == true)
        {
            lPresetOwner.LPresetSelectionValue = pWorking.LPresetRecordCreate();
        }
    }
}
