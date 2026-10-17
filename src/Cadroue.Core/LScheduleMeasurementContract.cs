namespace Cadroue.Core;

public interface LScheduleMeasurementContract
{
    void LScheduleDurationSet(Guid lWorkId, TimeSpan lWorkDuration);

    void LScheduleSourceSet(
        Guid lWorkId,
        TimeSpan lWorkDuration,
        LWorkMedia? lWorkSourceMedia,
        long? lWorkSourceBytes,
        IReadOnlyList<long> lWorkMergeBytes);

    void LScheduleBytesSet(Guid lWorkId, long? lWorkSourceBytes, IReadOnlyList<long> lWorkMergeBytes);

    void LScheduleLoudnessSet(Guid lWorkId, double lWorkLoudness);
}
