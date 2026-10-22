using System;
using System.Collections.Generic;
using System.Linq;

namespace Cadroue.Conduct;

public sealed record CInspectorNotice(
    string CInspectorNoticeKey,
    IReadOnlyList<object?> CInspectorNoticeArguments)
{
    public IReadOnlyList<object?> CInspectorNoticeArguments { get; } =
        CInspectorNoticeArguments.ToList().AsReadOnly();

    public static CInspectorNotice? CInspectorNoticeResolve(
        (string CCropNoticeKey, int? CCropNoticeExcess)? notice) => notice switch
    {
        null => null,
        { CCropNoticeExcess: { } excess } value =>
            new CInspectorNotice(value.CCropNoticeKey, new List<object?> { excess }.AsReadOnly()),
        { } value => new CInspectorNotice(value.CCropNoticeKey, Array.Empty<object?>())
    };
}

public sealed record CInspectorTip(bool CInspectorTipEnabled, CInspectorNotice? CInspectorNotice)
{
    public static CInspectorTip CInspectorTipResolve(
        bool active,
        bool capable,
        bool previewAvailable,
        string disabledKey,
        string previewKey)
    {
        CInspectorNotice? notice = !capable
            ? new CInspectorNotice(disabledKey, Array.Empty<object?>())
            : !previewAvailable && previewKey.Length > 0
                ? new CInspectorNotice(previewKey, Array.Empty<object?>())
                : null;
        return new CInspectorTip(capable && active, notice);
    }
}
