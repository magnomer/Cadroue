using Cadroue.Conduct;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed class QSensorPreset
{
    private readonly CDetectorPreset qSensorPreset;

    public QSensorPreset(QSensor lOwner)
    {
        qSensorPreset = lOwner.CDetector.CDetectorPreset;
    }

    public QInspectorChoice QSensorChoiceRead(LDetectorKind lKind) =>
        QInspectorPlan.QInspectorChoiceBuild(
            CChoicePlan.CChoicePlanBuild(qSensorPreset.CDetectorChoiceRead(lKind)));
}
