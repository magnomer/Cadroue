using System;
using System.Collections.Generic;
using System.Linq;
using Cadroue.Core;

namespace Cadroue.Conduct;

public sealed class CDetectorPreset
{
    private readonly Dictionary<LDetectorKind, string?> _cDetectorTokens = new();
    private readonly CDetector _cDetector;

    internal CDetectorPreset(CDetector detector)
    {
        _cDetector = detector;
    }

    public event Action? CDetectorChanged;

    internal void CDetectorKindAdd(LDetectorKind kind) => _cDetectorTokens[kind] = CDetectorTokenResolve(kind);

    private static string? CDetectorTokenResolve(LDetectorKind kind) =>
        CDetectorPresetCheck(kind) ? LDetectorCatalog.LDetectorTokenDefault : null;

    public static bool CDetectorPresetCheck(LDetectorKind kind) =>
        LDetectorCatalog.LDetectorTokensRead(kind).Count > 0;

    public string? CDetectorTokenRead(LDetectorKind kind) => _cDetectorTokens.GetValueOrDefault(kind);

    public IReadOnlyList<CChoice> CDetectorChoiceRead(LDetectorKind kind) => CChoicePreset.CChoicePresetRead(
        LDetectorCatalog.LDetectorTokensRead(kind),
        CDetectorMatchRead(kind),
        CDetectorTokenRead(kind),
        token => CDetectorPlan.CDetectorKeyRead(kind, token));

    public void CDetectorChoiceSelect(LDetectorKind kind, int index)
    {
        IReadOnlyList<CChoice> choices = CDetectorChoiceRead(kind);
        if (index < 0 || index >= choices.Count || choices[index] is not { CChoiceToken: { } token } choice
            || choice.CChoiceChosen)
        {
            return;
        }

        CDetectorPresetSelect(kind, token);
    }

    public string? CDetectorMatchRead(LDetectorKind kind) =>
        LDetectorCatalog.LDetectorStepMatch(_cDetector.CDetectorStepRead(kind), _cDetector.CDetectorMetric);

    public void CDetectorPresetSelect(LDetectorKind kind, string token)
    {
        if (!CDetectorPresetCheck(kind) || !_cDetectorTokens.ContainsKey(kind))
        {
            return;
        }

        LDetectorStep? target = LDetectorCatalog.LDetectorStepResolve(
            _cDetector.CDetectorStepRead(kind), token, _cDetector.CDetectorMetric);
        if (target is not { } resolved)
        {
            return;
        }

        CDetectorPresetApply(kind, token, LDetector.LDetectorStepNormalize(resolved));
    }

    private void CDetectorPresetApply(LDetectorKind kind, string token, LDetectorStep normal)
    {
        if (_cDetector.CDetectorStepRead(kind) == normal && _cDetectorTokens[kind] == token)
        {
            return;
        }

        _cDetector.CDetectorStepCommit(normal);
        _cDetectorTokens[kind] = token;
        CDetectorChanged?.Invoke();
    }

    private static string? CDetectorTokenNormalize(LDetectorKind kind, string token) =>
        LDetectorCatalog.LDetectorTokensRead(kind).Contains(token) ? token : null;

    public void CDetectorTokenSet(LDetectorKind kind, string token)
    {
        if (!_cDetectorTokens.ContainsKey(kind))
        {
            return;
        }

        string? normal = CDetectorTokenNormalize(kind, token);
        if (_cDetectorTokens[kind] == normal)
        {
            return;
        }

        _cDetectorTokens[kind] = normal;
        CDetectorChanged?.Invoke();
    }
}
