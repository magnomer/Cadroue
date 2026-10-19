namespace Convention.Tests;

internal static class TAuditDemeanorSetting
{
    public static readonly IReadOnlyDictionary<string, string[]> TAuditDemeanorOffer =
        new Dictionary<string, string[]>
        {
            ["Cadroue.UIDemeanor>Cadroue.Conduct"] =
                [
                    "CAtelier", "CUsher", "CLineage", "CLineageAnswer", "CLineageEntry", "CLineageBatch",
                    "CLineageStep", "CWorkspace", "CWorkspaceIntent", "CPlacement", "CInfo", "CInspector", "CGamma",
                    "CExposure", "CVolume",
                    "CViewer", "CEdit", "CEditVerdict",
                    "CSEncoder", "CSEncoderCodec", "CSEncoderDraft", "CSEncoderChoice", "CSEncoderExtra",
                    "CSEncoderQuality", "CSEncoderSpeed", "CSEncoderRow", "CSEncoderTrial",
                    "CSEncoderState", "CSEncoderVideo",
                ],
        };
}
