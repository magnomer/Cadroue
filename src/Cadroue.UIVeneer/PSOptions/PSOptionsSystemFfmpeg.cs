using System.Windows;
using System.Windows.Controls;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed class PSOptionsSystemFfmpeg
{
    private const string PSOptionsDiagnosisIcon = "/PAsset/PPanel/PDiagnosis.svg";

    private readonly QSOptions lsOptions;
    private readonly Window psOptionsWindow;
    private readonly TextBox psSystemFfmpegBox;

    internal PSOptionsSystemFfmpeg(QSOptions lsOptionsOwner, Window pWindow)
    {
        lsOptions = lsOptionsOwner;
        psOptionsWindow = pWindow;
        psSystemFfmpegBox = PSEntry.PSEntryBuild(lsOptions.QSOptionsDraft.LPreferenceFfmpegFolder, 320);
    }

    internal string PSSystemFfmpegRead() => psSystemFfmpegBox.Text;

    internal UIElement PSSystemFfmpegBuild()
    {
        TextBlock pFfmpegState = PSSystemStateBuild();
        PSSystemStateAttach(pFfmpegState);
        (Button pFfmpegBrowse, Button pFfmpegOpen, Button pFfmpegDiagnosis) = PSSystemButtonBuild();
        PSSystemButtonAttach(pFfmpegBrowse, pFfmpegOpen, pFfmpegDiagnosis);
        return PSSystemFfmpegBuild(pFfmpegState, pFfmpegBrowse, pFfmpegOpen, pFfmpegDiagnosis);
    }

    private TextBlock PSSystemStateBuild() => new()
    {
        Foreground = PSField.PSFieldMuted,
        TextWrapping = TextWrapping.Wrap,
        Margin = PSNotice.PSNoticeMargin,
        Text = PSSystemFfmpegFormat(psSystemFfmpegBox.Text)
    };

    private void PSSystemStateAttach(TextBlock pFfmpegState) =>
        psSystemFfmpegBox.TextChanged += (_, _) => PSSystemStateUpdate(pFfmpegState);

    private void PSSystemStateUpdate(TextBlock pFfmpegState) =>
        PSSystemStateShow(pFfmpegState, PSSystemFfmpegFormat(psSystemFfmpegBox.Text));

    private static void PSSystemStateShow(TextBlock pFfmpegState, string pText) => pFfmpegState.Text = pText;

    private static (Button PSSystemBrowse, Button PSSystemFolder, Button PSSystemDiagnosis) PSSystemButtonBuild()
    {
        Button pFfmpegBrowse = PSInline.PSInlineIconBuild(
            PSOptionsFolder.PSOptionsBrowseIcon,
            QLocalizationCatalog.QLocalizationTextRead("Options.System.Browse"),
            new Thickness(8, 0, 0, 0));
        Button pFfmpegOpen = PSInline.PSInlineIconBuild(
            PSOptionsFolder.PSOptionsOpenIcon,
            QLocalizationCatalog.QLocalizationTextRead("Options.System.Open"),
            new Thickness(6, 0, 0, 0));
        Button pFfmpegDiagnosis = PSInline.PSInlineIconBuild(
            PSOptionsDiagnosisIcon,
            QLocalizationCatalog.QLocalizationTextRead("Options.System.Diagnosis"),
            new Thickness(6, 0, 0, 0));
        return (pFfmpegBrowse, pFfmpegOpen, pFfmpegDiagnosis);
    }

    private void PSSystemButtonAttach(Button pFfmpegBrowse, Button pFfmpegOpen, Button pFfmpegDiagnosis)
    {
        pFfmpegBrowse.Click += (_, _) => PSOptionsFolder.PSSystemFolderRead(
            psSystemFfmpegBox,
            QLocalizationCatalog.QLocalizationTextRead("Options.System.ChooseFFmpeg"),
            psSystemFfmpegBox.Text);
        pFfmpegOpen.Click += (_, _) => lsOptions.QSOptionsFolderOpen(psSystemFfmpegBox.Text, string.Empty);
        pFfmpegDiagnosis.Click += (_, _) =>
            PSDiagnosis.PSDiagnosisShow(psOptionsWindow, lsOptions.QSOptionsDiagnosisCreate);
    }

    private UIElement PSSystemFfmpegBuild(
        TextBlock pFfmpegState,
        Button pFfmpegBrowse,
        Button pFfmpegOpen,
        Button pFfmpegDiagnosis) =>
        PSPlate.PSPlateBuild(QLocalizationCatalog.QLocalizationTextRead("Options.System.FFmpeg"),
            PSField.PSFieldButtonBuild(
                QLocalizationCatalog.QLocalizationTextRead("Options.System.Location"),
                psSystemFfmpegBox,
                pFfmpegBrowse,
                pFfmpegOpen,
                pFfmpegDiagnosis),
            pFfmpegState);

    private static string PSSystemFfmpegFormat(string pFolder) =>
        QLocalizationCatalog.QLocalizationTextRead(QSOptions.QSOptionsFfmpegResolve(pFolder));
}
