using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

// A section gate row in the panel's words: its number, its wording when unnamed, its affixes and its times.
public sealed record QSectionRow(
    int QSectionRowIndex,
    string QSectionRowNumber,
    string QSectionRowName,
    string QSectionRowText,
    bool QSectionRowUnnamed,
    string QSectionRowPrefix,
    string QSectionRowSuffix,
    bool QSectionRowPrefixed,
    bool QSectionRowSuffixed,
    IReadOnlyList<string> QSectionRowAffixes,
    string QSectionRowOrigin,
    string QSectionRowEnd,
    string QSectionRowSpan,
    int QSectionRowColor,
    bool QSectionRowHidden,
    bool QSectionRowSelected,
    bool QSectionRowEditing)
{
    public static string QSectionNumberRead(int lIndex) => (lIndex + 1).ToString();

    public static QSectionRow QSectionRowCreate(CSectionRow lRow) =>
        new(
            lRow.CSectionRowIndex,
            QSectionNumberRead(lRow.CSectionRowIndex),
            lRow.CSectionRowName,
            lRow.CSectionRowUnnamed
                ? QLocalizationCatalog.QLocalizationFormat("Section.DefaultName", lRow.CSectionRowIndex + 1)
                : lRow.CSectionRowName,
            lRow.CSectionRowUnnamed,
            lRow.CSectionRowPrefix,
            lRow.CSectionRowSuffix,
            lRow.CSectionRowPrefixed,
            lRow.CSectionRowSuffixed,
            lRow.CSectionRowAffixes.Select(lAffix => $"  /  {lAffix}").ToArray(),
            QSectionTimeFormat(lRow.CSectionRowOrigin),
            QSectionTimeFormat(lRow.CSectionRowEnd),
            $"  ({QSectionTimeFormat(lRow.CSectionRowSpan)})",
            lRow.CSectionRowColor,
            lRow.CSectionRowHidden,
            lRow.CSectionRowSelected,
            lRow.CSectionRowEditing);

    private static string QSectionTimeFormat(TimeSpan lTime) =>
        lTime.TotalHours >= 1
            ? $"{(int)lTime.TotalHours}:{lTime.Minutes:D2}:{lTime.Seconds:D2}"
            : $"{lTime.Minutes}:{lTime.Seconds:D2}";
}
