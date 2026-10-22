using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

// A section's tips: a notice, when the tool is not capable or has no preview, stands in for every tip's own wording.
public sealed record QInspectorTip(
    bool QInspectorTipEnabled,
    string? QInspectorTipNotice,
    string QInspectorTipBox,
    string QInspectorTipPersistent)
{
    public static QInspectorTip QInspectorTipResolve(
        bool lActive,
        bool lCapable,
        bool lPreviewAvailable,
        string lDisabledKey,
        string lPreviewKey,
        string lApplyKey,
        string lPersistKey) => QInspectorTipResolve(
            CInspectorTip.CInspectorTipResolve(
                lActive, lCapable, lPreviewAvailable, lDisabledKey, lPreviewKey),
            lApplyKey,
            lPersistKey);

    internal static QInspectorTip QInspectorTipResolve(
        CInspectorTip lPlan,
        string lApplyKey,
        string lPersistKey)
    {
        string? lNotice = QInspectorNotice.QInspectorNoticeResolve(lPlan.CInspectorNotice);
        return new QInspectorTip(
            lPlan.CInspectorTipEnabled,
            lNotice,
            lNotice ?? QLocalizationCatalog.QLocalizationTextRead(lApplyKey),
            lNotice ?? QLocalizationCatalog.QLocalizationTextRead(lPersistKey));
    }

    public string QInspectorNoticeRead(string lKey) =>
        QInspectorTipNotice ?? QLocalizationCatalog.QLocalizationTextRead(lKey);
}
