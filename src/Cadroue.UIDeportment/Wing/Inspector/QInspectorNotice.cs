using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

// The crop notice's wording: the crop gate decides the notice, and its excess, when it has one, fills the wording.
internal static class QInspectorNotice
{
    internal static string? QInspectorNoticeResolve(CCrop lGate) => lGate.CCropNotice switch
    {
        null => null,
        { CCropNoticeExcess: { } lExcess } lNotice =>
            QLocalizationCatalog.QLocalizationFormat(lNotice.CCropNoticeKey, lExcess),
        { } lNotice => QLocalizationCatalog.QLocalizationTextRead(lNotice.CCropNoticeKey)
    };
}
