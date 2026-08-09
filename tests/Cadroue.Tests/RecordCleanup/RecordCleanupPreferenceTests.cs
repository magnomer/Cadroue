using Cadroue.Core;
using Xunit;

namespace Cadroue.Tests;

public sealed class RecordCleanupPreferenceTests
{
    [Fact]
    public void RecordCleanup_DefaultsToOffAndThirtyDays()
    {
        LPreferenceState lPreferenceState = new();

        Assert.False(lPreferenceState.LPreferenceRecordCleanupActive);
        Assert.Equal(30, lPreferenceState.LPreferenceRecordCleanupDays);
    }

    [Fact]
    public void RecordCleanupDays_OutOfRange_AreClamped()
    {
        LPreferenceState lPreferenceLow = new() { LPreferenceRecordCleanupDays = 0 };
        lPreferenceLow.LPreferenceNormalize();
        Assert.Equal(1, lPreferenceLow.LPreferenceRecordCleanupDays);

        LPreferenceState lPreferenceHigh = new() { LPreferenceRecordCleanupDays = 9999 };
        lPreferenceHigh.LPreferenceNormalize();
        Assert.Equal(365, lPreferenceHigh.LPreferenceRecordCleanupDays);
    }

    [Fact]
    public void RecordCleanup_Clone_PreservesSettings()
    {
        LPreferenceState lPreferenceState = LPreferenceState.LPreferenceDefaultCreate();
        lPreferenceState.LPreferenceRecordCleanupActive = true;
        lPreferenceState.LPreferenceRecordCleanupDays = 90;

        LPreferenceState lPreferenceClone = lPreferenceState.LPreferenceClone();

        Assert.True(lPreferenceClone.LPreferenceRecordCleanupActive);
        Assert.Equal(90, lPreferenceClone.LPreferenceRecordCleanupDays);
    }
}
