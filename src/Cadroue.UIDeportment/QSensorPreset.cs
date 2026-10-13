using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed class QSensorPreset
{
    private readonly Dictionary<LDetectorKind, string?> qSensorTokens = new();
    private readonly QSensor qSensor;

    public QSensorPreset(QSensor lOwner)
    {
        qSensor = lOwner;
    }

    public event Action? QSensorPresetChanged;

    internal void QSensorKindAdd(LDetectorKind lKind) =>
        qSensorTokens[lKind] = QSensorPresetCheck(lKind) ? LDetectorCatalog.LDetectorTokenDefault : null;

    public static bool QSensorPresetCheck(LDetectorKind lKind) => LDetectorCatalog.LDetectorTokensRead(lKind).Count > 0;

    public string? QSensorTokenRead(LDetectorKind lKind) => qSensorTokens.GetValueOrDefault(lKind);

    public QInspectorChoice QSensorChoiceRead(LDetectorKind lKind) => QInspectorPlan.QInspectorChoiceRead(
        LDetectorCatalog.LDetectorTokensRead(lKind),
        lToken => QSensorPlan.QSensorKeyRead(lKind, lToken),
        QSensorTokenRead(lKind),
        QSensorMatchRead(lKind));

    public void QSensorChoiceSelect(LDetectorKind lKind, int lIndex)
    {
        string? lToken = QInspectorPlan.QInspectorChoiceResolve(
            LDetectorCatalog.LDetectorTokensRead(lKind), lIndex, QSensorTokenRead(lKind), QSensorMatchRead(lKind));
        if (lToken is not null)
        {
            QSensorPresetSelect(lKind, lToken);
        }
    }

    public string? QSensorMatchRead(LDetectorKind lKind) =>
        LDetectorCatalog.LDetectorStepMatch(qSensor.QSensorStepRead(lKind), qSensor.QSensorMetric);

    public void QSensorPresetSelect(LDetectorKind lKind, string lToken)
    {
        if (!QSensorPresetCheck(lKind) || !qSensorTokens.ContainsKey(lKind))
        {
            return;
        }

        LDetectorStep? lTarget = LDetectorCatalog.LDetectorStepResolve(
            qSensor.QSensorStepRead(lKind), lToken, qSensor.QSensorMetric);
        if (lTarget is not { } lResolved)
        {
            return;
        }

        LDetectorStep lNormal = LDetector.LDetectorStepNormalize(lResolved);
        if (qSensor.QSensorStepRead(lKind) == lNormal && qSensorTokens[lKind] == lToken)
        {
            return;
        }

        qSensor.QSensorStepCommit(lNormal);
        qSensorTokens[lKind] = lToken;
        QSensorPresetChanged?.Invoke();
    }

    public void QSensorTokenSet(LDetectorKind lKind, string lToken)
    {
        if (!qSensorTokens.ContainsKey(lKind))
        {
            return;
        }

        string? lNormal = LDetectorCatalog.LDetectorTokensRead(lKind).Contains(lToken) ? lToken : null;
        if (qSensorTokens[lKind] == lNormal)
        {
            return;
        }

        qSensorTokens[lKind] = lNormal;
        QSensorPresetChanged?.Invoke();
    }
}
