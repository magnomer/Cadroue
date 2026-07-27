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

        if (LExportSpecificState.LPresetTryLoad(lPresetName, lExportSpecificState))
        {
            PExportSummaryUpdate();
        }
    }

    private void PExportPresetAdd(object sender, RoutedEventArgs e)
    {
        string lPresetName = PExportPresetNameCreate("New Preset");
        lExportSpecificState.PresetName = lPresetName;
        LExportSpecificState.LPresetSave(lPresetName, lExportSpecificState);
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

        if (!LExportSpecificState.LPresetDelete(lPresetName))
        {
            return;
        }

        string? lNextPresetName = LExportSpecificState.LPresetFirstName;
        pExportPresetBusy = true;
        if (lNextPresetName is not null && LExportSpecificState.LPresetTryLoad(lNextPresetName, lExportSpecificState))
        {
            pPresetNameSelected = lNextPresetName;
        }
        else
        {
            lExportSpecificState.PresetName = string.Empty;
            pPresetNameSelected = null;
        }

        pExportPresetBusy = false;
        PExportSummaryUpdate();
    }

    private void PExportPresetSave(object sender, RoutedEventArgs e)
    {
        string lPresetName = string.IsNullOrWhiteSpace(lExportSpecificState.PresetName)
            ? pPresetNameSelected ?? string.Empty
            : lExportSpecificState.PresetName;

        char[] pInvalidCharacters = Path.GetInvalidFileNameChars();
        string pFileName = new string(lPresetName
            .Trim()
            .Select(pCharacter => pInvalidCharacters.Contains(pCharacter) ? '_' : pCharacter)
            .ToArray());

        var pDialog = new SaveFileDialog
        {
            Title = "Export preset",
            Filter = "Cadroue preset (*.json)|*.json|All files|*.*",
            DefaultExt = "json",
            AddExtension = true,
            FileName = string.IsNullOrWhiteSpace(pFileName) ? "Preset.json" : $"{pFileName}.json"
        };

        if (pDialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            LExportSpecificPresetStore.LPresetFileSave(lExportSpecificState, pDialog.FileName);
        }
        catch (Exception pError)
        {
            MessageBox.Show(
                $"Could not write the preset file.\n\n{pError.Message}",
                "Export preset",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void PExportPresetLoad(object sender, RoutedEventArgs e)
    {
        var pDialog = new OpenFileDialog
        {
            Title = "Import preset",
            Filter = "Cadroue preset (*.json)|*.json|All files|*.*",
            DefaultExt = "json",
            CheckFileExists = true
        };

        if (pDialog.ShowDialog() != true)
        {
            return;
        }

        LExportSpecificState? lImportedPreset;
        try
        {
            lImportedPreset = LExportSpecificPresetStore.LPresetFileLoad(pDialog.FileName);
        }
        catch (Exception pError)
        {
            MessageBox.Show(
                $"Could not read the preset file.\n\n{pError.Message}",
                "Import preset",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        if (lImportedPreset is null)
        {
            MessageBox.Show(
                "That file does not hold a Cadroue preset.",
                "Import preset",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        string lImportedName = lImportedPreset.PresetName.Trim();
        if (string.IsNullOrWhiteSpace(lImportedName))
        {
            lImportedName = Path.GetFileNameWithoutExtension(pDialog.FileName).Trim();
        }

        string lPresetName = PExportPresetNameCreate(
            string.IsNullOrWhiteSpace(lImportedName) ? "Imported Preset" : lImportedName);

        lExportSpecificState.LPresetCopy(lImportedPreset);
        lExportSpecificState.PresetName = lPresetName;
        LExportSpecificState.LPresetSave(lPresetName, lExportSpecificState);
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

        lExportSpecificState.PresetName = lPresetName;
        LExportSpecificState.LPresetSave(lPresetName, lExportSpecificState);
        PExportSummaryUpdate();
    }

    private void PExportModificationRestore(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (pPresetNameSelected is not string lPresetName)
        {
            return;
        }

        if (LExportSpecificState.LPresetTryLoad(lPresetName, lExportSpecificState))
        {
            PExportSummaryUpdate();
        }
    }

    private void PExportPresetNameCommit(string lOldPresetName, string lNewPresetName)
    {
        if (!string.Equals(pPresetNameEditing, lOldPresetName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        pPresetNameEditing = null;
        pPresetNameBoxCurrent = null;
        string lName = lNewPresetName.Trim();
        if (string.IsNullOrWhiteSpace(lName) || string.Equals(lOldPresetName, lName, StringComparison.OrdinalIgnoreCase))
        {
            PExportPresetRebuild();
            return;
        }

        if (LExportSpecificState.LPresetNames.Any(lExisting => string.Equals(lExisting, lName, StringComparison.OrdinalIgnoreCase)))
        {
            PExportPresetRebuild();
            return;
        }

        bool lCurrentPresetRename = string.Equals(pPresetNameSelected, lOldPresetName, StringComparison.OrdinalIgnoreCase);
        var lPresetState = new LExportSpecificState();
        if (lCurrentPresetRename)
        {
            lPresetState.LPresetCopy(lExportSpecificState);
        }
        else if (!LExportSpecificState.LPresetTryLoad(lOldPresetName, lPresetState))
        {
            PExportPresetRebuild();
            return;
        }

        lPresetState.PresetName = lName;
        LExportSpecificState.LPresetSave(lName, lPresetState);
        LExportSpecificState.LPresetDelete(lOldPresetName);
        if (lCurrentPresetRename)
        {
            lExportSpecificState.PresetName = lName;
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

    private static string PExportPresetNameCreate(string pBaseName)
    {
        if (!LExportSpecificState.LPresetNames.Any(lName => string.Equals(lName, pBaseName, StringComparison.OrdinalIgnoreCase)))
        {
            return pBaseName;
        }

        for (int lIndex = 2; ; lIndex++)
        {
            string lCandidate = $"{pBaseName} {lIndex}";
            if (!LExportSpecificState.LPresetNames.Any(lName => string.Equals(lName, lCandidate, StringComparison.OrdinalIgnoreCase)))
            {
                return lCandidate;
            }
        }
    }
}
