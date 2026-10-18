using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public sealed record QViewerEngineChoice(
    string QViewerChoiceKey,
    string QViewerChoiceText,
    string QViewerChoiceTip,
    bool QViewerChoiceActive,
    bool QViewerChoiceEnabled);

public sealed record QViewerEnginePlan(
    QViewerEngineChoice QViewerChoiceFlyleaf,
    QViewerEngineChoice QViewerChoiceMpv);

// The preview engine the user prefers: the two choices offered, and the chosen one saved and applied.
public static class QViewerEnginePreference
{
    public static QViewerEnginePlan QViewerEngineRead()
    {
        bool lMpv = LRenderer.LRendererEngineRead() == LPreviewEngine.LPreviewEngineMpv;
        bool lInstalled = LMpvAvailability.LMpvAvailableCheck();
        return new QViewerEnginePlan(
            new QViewerEngineChoice(
                LRenderer.LRendererFlyleafToken,
                QLocalizationCatalog.QLocalizationTextRead("Processing.Engine.Flyleaf"),
                QLocalizationCatalog.QLocalizationTextRead("Processing.Engine.FlyleafTooltip"),
                !lMpv,
                true),
            new QViewerEngineChoice(
                LRenderer.LRendererMpvToken,
                QLocalizationCatalog.QLocalizationTextRead("Processing.Engine.Mpv"),
                QLocalizationCatalog.QLocalizationTextRead(
                    lInstalled ? "Processing.Engine.MpvTooltip" : "Processing.Engine.MpvMissing"),
                lMpv,
                lInstalled));
    }

    public static void QViewerEngineSelect(string lKey)
    {
        bool lMpv = string.Equals(lKey, LRenderer.LRendererMpvToken, StringComparison.Ordinal);
        if (lMpv && !LMpvAvailability.LMpvAvailableCheck())
        {
            return;
        }

        if (lMpv == (LRenderer.LRendererEngineRead() == LPreviewEngine.LPreviewEngineMpv))
        {
            return;
        }

        QViewerEngineSave(lKey);
        LRenderer.LRendererEngineSet(lMpv ? LPreviewEngine.LPreviewEngineMpv : LPreviewEngine.LPreviewEngineFlyleaf);
    }

    // The chosen engine's key goes into a fresh preference record, which replaces the current one.
    private static void QViewerEngineSave(string lKey)
    {
        LPreferenceState lPreference = LPreference.LPreferenceStateCurrent.LPreferenceClone();
        lPreference.LPreferencePreviewEngine = lKey;
        LPreference.LPreferenceStateSet(lPreference);
    }
}
