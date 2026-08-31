using Cadroue.Core;
using Xunit;

namespace Cadroue.Tests;

public sealed class TRetentionPreference
{
    [Fact]
    public void RecordCleanup_DefaultsToOffAndThirtyDays()
    {
        LPreferenceState lPreferenceState = TInterface.TPreferenceCreate();

        Assert.False(lPreferenceState.LPreferenceCleanupActive);
        Assert.Equal(30, lPreferenceState.LPreferenceCleanupDays);
    }

    [Fact]
    public void RecordCleanupDays_OutOfRange_AreClamped()
    {
        LPreferenceState lPreferenceLow = TInterface.TPreferenceCreate(0);
        TInterface.TPreferenceNormalize(lPreferenceLow);
        Assert.Equal(1, lPreferenceLow.LPreferenceCleanupDays);

        LPreferenceState lPreferenceHigh = TInterface.TPreferenceCreate(9999);
        TInterface.TPreferenceNormalize(lPreferenceHigh);
        Assert.Equal(365, lPreferenceHigh.LPreferenceCleanupDays);
    }

    [Fact]
    public void RecordCleanup_Clone_PreservesSettings()
    {
        LPreferenceState lPreferenceState = TInterface.TPreferenceDefaultCreate();
        lPreferenceState.LPreferenceCleanupActive = true;
        lPreferenceState.LPreferenceCleanupDays = 90;

        LPreferenceState lPreferenceClone = TInterface.TPreferenceClone(lPreferenceState);

        Assert.True(lPreferenceClone.LPreferenceCleanupActive);
        Assert.Equal(90, lPreferenceClone.LPreferenceCleanupDays);
    }
}
