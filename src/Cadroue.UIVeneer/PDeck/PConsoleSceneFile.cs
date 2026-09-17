using Cadroue.Core;
using Cadroue.UIVeneer.PSCasement;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Win32;

using Cadroue.Infrastructure;
using Cadroue.Application;

namespace Cadroue.UIVeneer.PDeck;

public sealed partial class PConsole
{
    private void PConsoleExportHandle(object pSender, RoutedEventArgs pArguments)
    {
        string lSceneName = (pConsoleRelayCombo.Text ?? string.Empty).Trim();
        LSceneRecord? lScene = lSceneName.Length > 0 ? LScene.LSceneRead(lSceneName) : null;
        if (lScene is null)
        {
            if (PConsoleWindowRead() is not { } pWindow)
            {
                return;
            }

            lScene = pWindow.PWindowSceneRead(
                lSceneName.Length > 0 ? lSceneName : LLocalization.LLocalizationTextRead("Console.Scene.DefaultName"));
        }

        var pDialog = new SaveFileDialog
        {
            Title = LLocalization.LLocalizationTextRead("Console.Scene.Dialog.Export"),
            Filter = LLocalization.LLocalizationTextRead("Console.Scene.Dialog.Filter"),
            DefaultExt = "json",
            AddExtension = true,
            FileName = PConsoleFileResolve(lScene.LSceneName)
        };

        if (pDialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            LScene.LSceneFileSave(lScene, pDialog.FileName);
        }
        catch (Exception pError)
        {
            PConsoleErrorShow("Console.Scene.Error.Write", pError.Message, "Console.Scene.Dialog.Export");
        }
    }

    private void PConsoleImportHandle(object pSender, RoutedEventArgs pArguments)
    {
        var pDialog = new OpenFileDialog
        {
            Title = LLocalization.LLocalizationTextRead("Console.Scene.Dialog.Import"),
            Filter = LLocalization.LLocalizationTextRead("Console.Scene.Dialog.Filter"),
            DefaultExt = "json",
            CheckFileExists = true
        };

        if (pDialog.ShowDialog() != true)
        {
            return;
        }

        LSceneRecord? lScene;
        try
        {
            lScene = LScene.LSceneFileLoad(pDialog.FileName);
        }
        catch (Exception pError)
        {
            PConsoleErrorShow("Console.Scene.Error.Read", pError.Message, "Console.Scene.Dialog.Import");
            return;
        }

        if (lScene is null)
        {
            PConsoleErrorShow("Console.Scene.Error.Invalid", string.Empty, "Console.Scene.Dialog.Import");
            return;
        }

        string lSceneName = lScene.LSceneName.Trim();
        if (lSceneName.Length == 0)
        {
            lSceneName = Path.GetFileNameWithoutExtension(pDialog.FileName).Trim();
        }

        lScene.LSceneName = PConsoleNameCreate(
            lSceneName.Length > 0 ? lSceneName : LLocalization.LLocalizationTextRead("Console.Scene.ImportedName"));
        LScene.LSceneSave(lScene);
        PConsoleSceneRebuild();
        LTraceLog.LTraceLoadingRecord($"Scene imported '{lScene.LSceneName}'");
    }

    private static void PConsoleErrorShow(string lSceneMessageKey, string lSceneDetail, string lSceneTitleKey) =>
        PSWarning.PSWarningShow(
            null,
            LLocalization.LLocalizationTextRead(lSceneTitleKey),
            lSceneDetail.Length > 0
                ? LLocalization.LLocalizationFormat(lSceneMessageKey, lSceneDetail)
                : LLocalization.LLocalizationTextRead(lSceneMessageKey));

    private static string PConsoleFileResolve(string lSceneName)
    {
        char[] pInvalid = Path.GetInvalidFileNameChars();
        string pClean = new string(lSceneName.Trim()
            .Select(pCharacter => pInvalid.Contains(pCharacter) ? '_' : pCharacter)
            .ToArray());
        return string.IsNullOrWhiteSpace(pClean)
            ? $"{LLocalization.LLocalizationTextRead("Console.Scene.DefaultName")}.json"
            : $"{pClean}.json";
    }

    private static string PConsoleNameCreate(string lSceneBaseName)
    {
        if (!LScene.LSceneNames.Any(lName => string.Equals(lName, lSceneBaseName, StringComparison.OrdinalIgnoreCase)))
        {
            return lSceneBaseName;
        }

        for (int lIndex = 2; ; lIndex++)
        {
            string lCandidate = $"{lSceneBaseName} {lIndex}";
            if (!LScene.LSceneNames.Any(lName => string.Equals(lName, lCandidate, StringComparison.OrdinalIgnoreCase)))
            {
                return lCandidate;
            }
        }
    }
}
