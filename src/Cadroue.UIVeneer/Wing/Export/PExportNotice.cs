using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

public static class PExportNotice
{
    public static void PExportIncompatibleShow() => PSAnnouncement.PSAnnouncementShow(
        null,
        QLocalizationCatalog.QLocalizationTextRead("ExportPreset.Incompatible.Title"),
        QLocalizationCatalog.QLocalizationTextRead("ExportPreset.Incompatible.Body"));

    public static void PExportMissingShow() => PSAnnouncement.PSAnnouncementShow(
        null,
        QLocalizationCatalog.QLocalizationTextRead("ExportPreset.Missing.Title"),
        QLocalizationCatalog.QLocalizationTextRead("ExportPreset.Missing.Body"));
}
