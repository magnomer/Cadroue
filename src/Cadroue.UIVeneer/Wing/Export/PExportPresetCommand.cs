using System.Windows;
using System.Windows.Controls;
using Cadroue.Application;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed class PExportPresetCommand
{
    private readonly QExport lExport;
    private readonly LPresetSelection lPresetOwner;
    private readonly DependencyObject pExportOwner;

    public PExportPresetCommand(QExport lExport, LPresetSelection lPresetOwner, DependencyObject pExportOwner)
    {
        this.lExport = lExport;
        this.lPresetOwner = lPresetOwner;
        this.pExportOwner = pExportOwner;
    }

    internal LPreset PExportWorkingRead() => LPreset.LPresetStateCreate(lPresetOwner.LPresetSelectionValue);

    internal void PExportPresetAdd(object sender, RoutedEventArgs e)
    {
        string lPresetName = LPresetNaming.LPresetNameCreate(QLocalizationCatalog.QLocalizationTextRead(
            "ExportPreset.DefaultName"));
        PExportPresetSave(lPresetName);
    }

    private void PExportPresetSave(string lPresetName)
    {
        if (!lPresetOwner.LPresetSelectionSave(lPresetName))
        {
            PExportNotice.PExportFailureShow(pExportOwner, lPresetName);
        }
    }

    internal void PExportPresetDelete(object sender, RoutedEventArgs e)
    {
        if (lExport.QExportSelected is not string lPresetName)
        {
            return;
        }

        if (LPresetTable.LPresetNativeCheck(lPresetName))
        {
            return;
        }

        if (!LPresetTable.LPresetDelete(lPresetName))
        {
            PExportNotice.PExportFailureShow(pExportOwner, lPresetName);
        }
    }

    internal void PExportModificationApply(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        PExportSelectedSave();
    }

    private void PExportSelectedSave()
    {
        if (lExport.QExportSelected is not string lPresetName)
        {
            return;
        }

        if (LPresetTable.LPresetNativeCheck(lPresetName))
        {
            return;
        }

        PExportPresetSave(lPresetName);
    }

    internal void PExportModificationRestore(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        PExportSelectedRestore();
    }

    private void PExportSelectedRestore()
    {
        if (lExport.QExportSelected is not string)
        {
            return;
        }

        lPresetOwner.LPresetSelectionRestore();
    }

    internal void PExportDialogShow(object sender, RoutedEventArgs e)
    {
        var pButton = (Button)sender;
        QSEncoder qsEncoder = lExport.QExportEncoderCreate();
        PSEncoder psEncoder = PExportEncoderBuild(qsEncoder, pButton);

        psEncoder.ShowDialog();
    }

    private static PSEncoder PExportEncoderBuild(QSEncoder qsEncoder, Button pButton)
    {
        var pEncoder = new PSEncoder { Owner = Window.GetWindow(pButton) };
        _ = new QSEncoderWindow(
            pEncoder,
            qsEncoder,
            qsEncoder.QSEncoderSave,
            pVerdict => PSVerdict.PSVerdictShow(pEncoder, pVerdict));
        return pEncoder;
    }
}
