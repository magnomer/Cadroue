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
        string lPresetName = LPresetNaming.LPresetNameCreate(QLocalizationCatalog.QLocalizationTextRead(
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

        if (LPresetTable.LPresetNativeCheck(lPresetName))
        {
            return;
        }

        if (!LPresetTable.LPresetDelete(lPresetName))
        {
            PExportFailureShow(lPresetName);
        }
    }

    private void PExportPresetSave(object sender, RoutedEventArgs e)
    {
        LPresetDocument lPresetValue = lPresetOwner.LPresetSelectionValue;
        string lPresetName = PExportNameResolve(lPresetValue);
        string pFileName = LPresetNaming.LPresetFileFormat(lPresetName);
        SaveFileDialog pDialog = PExportSaveBuild(PExportFileResolve(pFileName));

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

    private string PExportNameResolve(LPresetDocument lPresetValue) =>
        string.IsNullOrWhiteSpace(lPresetValue.LPresetName)
            ? QExport.QExportSelected ?? string.Empty
            : lPresetValue.LPresetName;

    private static string PExportFileResolve(string pFileName) =>
        string.IsNullOrWhiteSpace(pFileName)
            ? QLocalizationCatalog.QLocalizationTextRead("ExportPreset.Dialog.DefaultFile")
            : $"{pFileName}.json";

    private static SaveFileDialog PExportSaveBuild(string pFileName) => new()
    {
        Title = QLocalizationCatalog.QLocalizationTextRead("ExportPreset.Dialog.Export"),
        Filter = QLocalizationCatalog.QLocalizationTextRead("ExportPreset.Dialog.Filter"),
        DefaultExt = "json",
        AddExtension = true,
        FileName = pFileName
    };

    private static OpenFileDialog PExportOpenBuild() => new()
    {
        Title = QLocalizationCatalog.QLocalizationTextRead("ExportPreset.Dialog.Import"),
        Filter = QLocalizationCatalog.QLocalizationTextRead("ExportPreset.Dialog.Filter"),
        DefaultExt = "json",
        CheckFileExists = true
    };

    private void PExportPresetLoad(object sender, RoutedEventArgs e)
    {
        OpenFileDialog pDialog = PExportOpenBuild();

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

        string lImportedName = LPresetNaming.LPresetNameResolve(lImportedRecord.LPresetName, pDialog.FileName);

        string lPresetName = LPresetNaming.LPresetNameCreate(PExportImportResolve(lImportedName));

        PExportRecordSet(lImportedRecord, lPresetName);
        if (!LPresetTable.LPresetSave(lPresetName, LPreset.LPresetStateCreate(lImportedRecord)))
        {
            PExportFailureShow(lPresetName);
            return;
        }

        lPresetOwner.LPresetSelectionSelect(lPresetName);
    }

    private static string PExportImportResolve(string lImportedName) =>
        string.IsNullOrWhiteSpace(lImportedName)
            ? QLocalizationCatalog.QLocalizationTextRead("ExportPreset.ImportedName")
            : lImportedName;

    private static void PExportRecordSet(LPresetDocument lImportedRecord, string lPresetName) =>
        lImportedRecord.LPresetName = lPresetName;

    private void PExportModificationApply(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        PExportSelectedSave();
    }

    private void PExportSelectedSave()
    {
        if (QExport.QExportSelected is not string lPresetName)
        {
            return;
        }

        if (LPresetTable.LPresetNativeCheck(lPresetName))
        {
            return;
        }

        PExportPresetSave(lPresetName);
    }

    private void PExportModificationRestore(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        PExportSelectedRestore();
    }

    private void PExportSelectedRestore()
    {
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

        PExportBoxSet(null);
        QExport.QExportNameCommit(lOldPresetName, lNewPresetName);
    }

    private void PExportDialogShow(object sender, RoutedEventArgs e)
    {
        var pButton = (Button)sender;
        LPreset pWorking = PExportWorkingRead();
        PSEncoder psEncoder = PExportEncoderBuild(pWorking, pButton);

        if (psEncoder.ShowDialog() == true)
        {
            PExportWorkingSet(pWorking);
        }
    }

    private PSEncoder PExportEncoderBuild(LPreset pWorking, Button pButton) => new(
        QExport.QExportEncoderCreate(pWorking),
        () => PExportWorkingSet(pWorking))
    {
        Owner = Window.GetWindow(pButton)
    };

    private void PExportWorkingSet(LPreset pWorking) =>
        lPresetOwner.LPresetSelectionValue = pWorking.LPresetRecordCreate();
}
