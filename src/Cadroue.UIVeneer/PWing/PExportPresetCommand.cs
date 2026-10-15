using Cadroue.Infrastructure;
using Cadroue.Core;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using Cadroue.Application;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PWing;

public sealed partial class PExport
{
    private void PExportPresetAdd(object sender, RoutedEventArgs e)
    {
        string lPresetName = LPreset.LPresetNameCreate(QLocalizationCatalog.QLocalizationTextRead(
            "ExportPreset.DefaultName"));
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
            QLocalizationCatalog.QLocalizationTextRead("ExportPreset.Dialog.Export"),
            QLocalizationCatalog.QLocalizationFormat("ExportPreset.Error.Write", lPresetName));

    private void PExportPresetDelete(object sender, RoutedEventArgs e)
    {
        if (QExport.QExportSelected is not string lPresetName)
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
        LPresetDocument lPresetValue = lPresetOwner.LPresetSelectionValue;
        string lPresetName = string.IsNullOrWhiteSpace(lPresetValue.LPresetName)
            ? QExport.QExportSelected ?? string.Empty
            : lPresetValue.LPresetName;

        string pFileName = LPreset.LPresetFileFormat(lPresetName);

        var pDialog = new SaveFileDialog
        {
            Title = QLocalizationCatalog.QLocalizationTextRead("ExportPreset.Dialog.Export"),
            Filter = QLocalizationCatalog.QLocalizationTextRead("ExportPreset.Dialog.Filter"),
            DefaultExt = "json",
            AddExtension = true,
            FileName = string.IsNullOrWhiteSpace(pFileName)
                ? QLocalizationCatalog.QLocalizationTextRead("ExportPreset.Dialog.DefaultFile")
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
                QLocalizationCatalog.QLocalizationTextRead("ExportPreset.Dialog.Export"),
                QLocalizationCatalog.QLocalizationTextRead("ExportPreset.Error.Catalogue"));
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
            Title = QLocalizationCatalog.QLocalizationTextRead("ExportPreset.Dialog.Import"),
            Filter = QLocalizationCatalog.QLocalizationTextRead("ExportPreset.Dialog.Filter"),
            DefaultExt = "json",
            CheckFileExists = true
        };

        if (pDialog.ShowDialog() != true)
        {
            return;
        }

        LPresetDocument? lImportedRecord;
        try
        {
            lImportedRecord = LPresetStore.LPresetFileLoad(pDialog.FileName);
        }
        catch (Exception pError)
        {
            PSWarning.PSWarningShow(
                Window.GetWindow(this),
                QLocalizationCatalog.QLocalizationTextRead("ExportPreset.Dialog.Import"),
                QLocalizationCatalog.QLocalizationFormat("ExportPreset.Error.Read", pError.Message));
            return;
        }

        if (lImportedRecord is null)
        {
            PSWarning.PSWarningShow(
                Window.GetWindow(this),
                QLocalizationCatalog.QLocalizationTextRead("ExportPreset.Dialog.Import"),
                QLocalizationCatalog.QLocalizationTextRead("ExportPreset.Error.Invalid"));
            return;
        }

        string lImportedName = LPreset.LPresetNameResolve(lImportedRecord.LPresetName, pDialog.FileName);

        string lPresetName = LPreset.LPresetNameCreate(
            string.IsNullOrWhiteSpace(lImportedName)
                ? QLocalizationCatalog.QLocalizationTextRead("ExportPreset.ImportedName")
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
        if (QExport.QExportSelected is not string lPresetName)
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
        if (QExport.QExportSelected is not string)
        {
            return;
        }

        lPresetOwner.LPresetSelectionRestore();
    }

    private void PExportNameCommit(string lOldPresetName, string lNewPresetName)
    {
        if (!QExport.QExportEditingCheck(lOldPresetName))
        {
            return;
        }

        pExportBoxCurrent = null;
        QExport.QExportNameCommit(lOldPresetName, lNewPresetName);
    }

    private void PExportDialogShow(object sender, RoutedEventArgs e)
    {
        var pButton = (Button)sender;
        LPreset pWorking = PExportWorkingRead();
        var psEncoder = new PSEncoder(
            QExport.QExportEncoderCreate(pWorking),
            () => lPresetOwner.LPresetSelectionValue = pWorking.LPresetRecordCreate())
        {
            Owner = Window.GetWindow(pButton)
        };

        if (psEncoder.ShowDialog() == true)
        {
            lPresetOwner.LPresetSelectionValue = pWorking.LPresetRecordCreate();
        }
    }
}
