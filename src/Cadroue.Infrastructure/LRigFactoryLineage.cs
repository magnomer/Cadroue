using Cadroue.Core;

namespace Cadroue.Infrastructure;

public static class LRigFactoryLineage
{
    public static LRigLineage LRigLineageCreate()
    {
        return new LRigLineage(
            LScheduleLineage.LScheduleFileRead,
            LScheduleLineage.LScheduleLineageRead);
    }
}
