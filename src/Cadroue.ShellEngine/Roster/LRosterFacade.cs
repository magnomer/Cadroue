using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

// The worklist preferences for the roster gate. A save equal to the held value is not written and answers false.
// Sharing the worklist changes which records the schedule holds, so the station's schedule reloads after that save.
public sealed class LRosterFacade : LRosterPort
{
    internal LRosterFacade()
    {
    }

    public bool LRosterSharedRead() => LPreference.LPreferenceStateCurrent.LPreferenceWorklistShared;

    public bool LRosterDoneRead() => LPreference.LPreferenceStateCurrent.LPreferenceCollapseDone;

    public bool LRosterSharedSave(bool lShared)
    {
        if (lShared == LRosterSharedRead())
        {
            return false;
        }

        LPreferenceState lNext = LPreference.LPreferenceStateCurrent.LPreferenceClone();
        lNext.LPreferenceWorklistShared = lShared;
        LPreference.LPreferenceStateSet(lNext);
        LStationEngine.LStationSchedule!.LScheduleLoad();
        return true;
    }

    public bool LRosterDoneSave(bool lDone)
    {
        if (lDone == LRosterDoneRead())
        {
            return false;
        }

        LPreferenceState lNext = LPreference.LPreferenceStateCurrent.LPreferenceClone();
        lNext.LPreferenceCollapseDone = lDone;
        LPreference.LPreferenceStateSet(lNext);
        return true;
    }
}
