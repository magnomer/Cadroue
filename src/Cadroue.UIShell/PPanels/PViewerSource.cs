using System;
using System.Windows;
using Cadroue.Media;
using Cadroue.Core;
using Cadroue.Application;
using Cadroue.Infrastructure;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PViewer
{
    public void PViewerSourceOpen(string sourcePath)
    {
        LTraceLog.LTraceInfoRecord(
            $"Viewer source open requested '{System.IO.Path.GetFileName(sourcePath)}'",
            $"command={(pViewerCommandActive ? "active" : "INACTIVE")}, unloaded={pViewerUnloaded}, "
            + $"engine={PViewerEngineCurrent}, path={sourcePath}");

        if (!pViewerCommandActive || string.IsNullOrWhiteSpace(sourcePath))
        {
            LTraceLog.LTraceWarningRecord(
                $"Viewer source open refused: {(pViewerCommandActive ? "empty path" : "viewer command inactive (tab not the front workspace)")}");
            return;
        }

        if (LLibrarian.LLibrarianFileCheck(sourcePath))
        {
            if (PViewerSidecarResolve(sourcePath) is not { } pResolvedPath)
            {
                LTraceLog.LTraceWarningRecord("Viewer source open refused: sidecar (.cad) source could not be resolved");
                return;
            }

            sourcePath = pResolvedPath;
        }

        LPreference.LPreferenceMediaSet(sourcePath);
        if (!PCropPersistent)
        {
            LPreviewStateCurrent = LPreviewStateCurrent.LRotateFlipChange(LRotateFlip.LRotateDefaultCreate());
        }

        PPlayerVideoLoad(sourcePath);
    }

    private string? PViewerSidecarResolve(string pSidecarPath)
    {
        LSidecarSourceResult? pResult = LLibrarian.LLibrarianSourceResolve(pSidecarPath);
        if (pResult is null)
        {
            MessageBox.Show(
                LLocalization.LLocalizationTextRead("Viewer.Sidecar.ReadError"),
                LLocalization.LLocalizationTextRead("Viewer.Dialog.OpenTitle"),
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return null;
        }

        if (pResult.LSidecarResultVerified)
        {
            return pResult.LSidecarResultPath;
        }

        if (pResult.LSidecarResultKind != LSidecarSourceKind.LSidecarSourceMissing
            && MessageBox.Show(
                LLocalization.LLocalizationFormat("Viewer.Sidecar.MismatchFound", pResult.LSidecarResultPath),
                LLocalization.LLocalizationTextRead("Viewer.Dialog.OpenTitle"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            return pResult.LSidecarResultPath;
        }

        return PViewerSidecarFind(pSidecarPath, pResult.LSidecarResultName);
    }

    private string? PViewerSidecarFind(string pSidecarPath, string pSidecarFileName)
    {
        var pDialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = LLocalization.LLocalizationFormat("Viewer.Locate.Title", pSidecarFileName),
            FileName = pSidecarFileName,
            Filter = LLocalization.LLocalizationTextRead("Viewer.Dialog.MediaFilter")
        };

        if (pDialog.ShowDialog() != true)
        {
            return null;
        }

        if (LLibrarian.LLibrarianSourceMatch(pDialog.FileName, pSidecarPath))
        {
            return pDialog.FileName;
        }

        return MessageBox.Show(
            LLocalization.LLocalizationTextRead("Viewer.Sidecar.MismatchSelected"),
            LLocalization.LLocalizationTextRead("Viewer.Dialog.OpenTitle"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Question) == MessageBoxResult.Yes
            ? pDialog.FileName
            : null;
    }
}
