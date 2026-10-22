using Cadroue.Application;

namespace Cadroue.ShellEngine;

// The developer preference for the About gate. A save equal to the held value writes nothing.
public sealed class LAboutFacade : LAboutPort
{
    internal LAboutFacade()
    {
    }

    public bool LAboutDeveloperRead() => LPreference.LPreferenceStateCurrent.LPreferenceDeveloperActive;

    public void LAboutDeveloperSave(bool lDeveloper) => LPreference.LPreferenceDeveloperSet(lDeveloper);
}
