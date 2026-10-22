using Cadroue.Conduct;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

public static class QSensorName
{
    public static string QSensorNameRead(LDetectorKind lKind) => CDetectorPlan.CDetectorNameRead(lKind);

    public static LDetectorKind? QSensorKindRead(string? lName) => CDetectorPlan.CDetectorKindRead(lName);
}
