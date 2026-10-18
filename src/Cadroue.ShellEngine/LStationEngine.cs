using Cadroue.Core;

namespace Cadroue.ShellEngine;

public static class LStationEngine
{
    public static LScheduleContract? LStationSchedule { get; set; }

    public static LScheduleIntakeContract? LStationIntake { get; set; }

    public static LScheduleLeaseContract? LStationLease { get; set; }

    public static LScheduleTransitionContract? LStationTransition { get; set; }

    public static LScheduleMeasurementContract? LStationMeasurement { get; set; }

    public static LScheduleRemoverContract? LStationRemover { get; set; }

    public static Action<Action>? LStationPost { get; set; }

    public static Func<string>? LStationProgramSource { get; set; }

    public static Func<LPreferenceState>? LStationPreferenceSource { get; set; }
}
