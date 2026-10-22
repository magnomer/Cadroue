using System.Windows.Controls;
using Cadroue.Application;

namespace Cadroue.UIDeportment;

public sealed class QExportSummary
{
    private readonly Func<QExportDigest> qExportSummaryProjection;
    private TextBlock qExportSummaryVideo = null!;
    private TextBlock qExportSummaryAudio = null!;
    private TextBlock qExportSummaryContainer = null!;
    private TextBlock qExportSummaryOutput = null!;

    public QExportSummary(Func<QExportDigest> projection)
    {
        qExportSummaryProjection = projection;
    }

    public void QExportSummaryAttach(Border summary)
    {
        var panel = (StackPanel)summary.Child;
        qExportSummaryContainer = (TextBlock)summary.FindName("PExportWrapper");
        qExportSummaryVideo = (TextBlock)summary.FindName("PExportPicture");
        qExportSummaryAudio = (TextBlock)summary.FindName("PExportSound");
        qExportSummaryOutput = (TextBlock)summary.FindName("PExportDestination");
        ((TextBlock)panel.Children[0]).Text =
            QLocalizationCatalog.QLocalizationTextRead("ExportSummary.Header.Summary");
        ((TextBlock)((Grid)qExportSummaryContainer.Parent).Children[0]).Text =
            QLocalizationCatalog.QLocalizationTextRead("Roster.Field.Container");
        ((TextBlock)((Grid)qExportSummaryVideo.Parent).Children[0]).Text =
            QLocalizationCatalog.QLocalizationTextRead("ExportSummary.Video");
        ((TextBlock)((Grid)qExportSummaryAudio.Parent).Children[0]).Text =
            QLocalizationCatalog.QLocalizationTextRead("ExportSummary.Audio");
        ((TextBlock)((Grid)qExportSummaryOutput.Parent).Children[0]).Text =
            QLocalizationCatalog.QLocalizationTextRead("Roster.Section.Output");
    }

    public void QExportSummaryUpdate()
    {
        QExportDigest digest = qExportSummaryProjection();
        qExportSummaryContainer.Text = digest.QExportDigestContainer;
        qExportSummaryVideo.Text = digest.QExportDigestVideo;
        qExportSummaryAudio.Text = digest.QExportDigestAudio;
        qExportSummaryOutput.Text = digest.QExportDigestOutput;
    }
}
