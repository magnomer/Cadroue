using Cadroue.Infrastructure;
using Cadroue.Core;
using Microsoft.Win32;
using System.Windows;
using Cadroue.Application;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed class PExportPresetFile
{
    private readonly QExport lExport;
    private readonly LPresetSelection lPresetOwner;
    private readonly DependencyObject pExportOwner;

    public PExportPresetFile(QExport lExport, LPresetSelection lPresetOwner, DependencyObject pExportOwner)
    {
        this.lExport = lExport;
        this.lPresetOwner = lPresetOwner;
        this.pExportOwner = pExportOwner;
    }

    internal void PExportPresetSave(object sender, RoutedEventArgs e)
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
                Window.GetWindow(pExportOwner),
                QLocalizationCatalog.QLocalizationTextRead("ExportPreset.Dialog.Export"),
                QLocalizationCatalog.QLocalizationTextRead("ExportPreset.Error.Catalogue"));
            return;
        }

        if (!LPresetStore.LPresetFileSave(lPresetValue, pDialog.FileName))
        {
            PExportNotice.PExportFailureShow(pExportOwner, pDialog.FileName);
        }
    }

    private string PExportNameResolve(LPresetDocument lPresetValue) =>
        string.IsNullOrWhiteSpace(lPresetValue.LPresetName)
            ? lExport.QExportSelected ?? string.Empty
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

    internal void PExportPresetLoad(object sender, RoutedEventArgs e)
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
                Window.GetWindow(pExportOwner),
                QLocalizationCatalog.QLocalizationTextRead("ExportPreset.Dialog.Import"),
                QLocalizationCatalog.QLocalizationFormat("ExportPreset.Error.Read", pError.Message));
            return;
        }

        if (lImportedRecord is null)
        {
            PSWarning.PSWarningShow(
                Window.GetWindow(pExportOwner),
                QLocalizationCatalog.QLocalizationTextRead("ExportPreset.Dialog.Import"),
                QLocalizationCatalog.QLocalizationTextRead("ExportPreset.Error.Invalid"));
            return;
        }

        string lImportedName = LPresetNaming.LPresetNameResolve(lImportedRecord.LPresetName, pDialog.FileName);

        string lPresetName = LPresetNaming.LPresetNameCreate(PExportImportResolve(lImportedName));

        PExportRecordSet(lImportedRecord, lPresetName);
        if (!LPresetTable.LPresetSave(lPresetName, LPreset.LPresetStateCreate(lImportedRecord)))
        {
            PExportNotice.PExportFailureShow(pExportOwner, lPresetName);
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
}
