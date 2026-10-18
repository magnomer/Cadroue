using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

// A preset row's wording: the row's own text, or, for the custom row, its text around the preset it drifted from.
// A broken translation is traced by the catalog and shown in English or raw, never thrown.
public static class QChoice
{
    public static string QChoiceTextRead(CChoice lRow) =>
        lRow.CChoiceBasis is { } lBasis
            ? QLocalizationCatalog.QLocalizationFormat(
                lRow.CChoiceKey, QLocalizationCatalog.QLocalizationTextRead(lBasis))
            : QLocalizationCatalog.QLocalizationTextRead(lRow.CChoiceKey);
}
