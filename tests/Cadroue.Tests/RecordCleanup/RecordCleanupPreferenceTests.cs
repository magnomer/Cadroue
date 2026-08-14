using Cadroue.Core;
using Xunit;

namespace Cadroue.Tests;

public sealed class RecordCleanupPreferenceTests
{
    [Fact]
    public void RecordCleanup_DefaultsToOffAndThirtyDays()
    {
        LPreferenceState lPreferenceState = TInterface.PreferenceCreate();

        Assert.False(lPreferenceState.LPreferenceCleanupActive);
        Assert.Equal(30, lPreferenceState.LPreferenceCleanupDays);
    }

    [Fact]
    public void RecordCleanupDays_OutOfRange_AreClamped()
    {
        LPreferenceState lPreferenceLow = TInterface.PreferenceCreate(0);
        TInterface.PreferenceNormalize(lPreferenceLow);
        Assert.Equal(1, lPreferenceLow.LPreferenceCleanupDays);

        LPreferenceState lPreferenceHigh = TInterface.PreferenceCreate(9999);
        TInterface.PreferenceNormalize(lPreferenceHigh);
        Assert.Equal(365, lPreferenceHigh.LPreferenceCleanupDays);
    }

    [Fact]
    public void RecordCleanup_Clone_PreservesSettings()
    {
        LPreferenceState lPreferenceState = TInterface.PreferenceDefaultCreate();
        lPreferenceState.LPreferenceCleanupActive = true;
        lPreferenceState.LPreferenceCleanupDays = 90;

        LPreferenceState lPreferenceClone = TInterface.PreferenceClone(lPreferenceState);

        Assert.True(lPreferenceClone.LPreferenceCleanupActive);
        Assert.Equal(90, lPreferenceClone.LPreferenceCleanupDays);
    }
}
