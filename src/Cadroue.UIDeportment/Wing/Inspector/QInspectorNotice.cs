using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

internal static class QInspectorNotice
{
    internal static string? QInspectorNoticeResolve(CInspectorNotice? lNotice) => lNotice switch
    {
        null => null,
        { CInspectorNoticeArguments.Count: > 0 } => QLocalizationCatalog.QLocalizationFormat(
            lNotice.CInspectorNoticeKey, lNotice.CInspectorNoticeArguments.ToArray()),
        _ => QLocalizationCatalog.QLocalizationTextRead(lNotice.CInspectorNoticeKey)
    };
}
