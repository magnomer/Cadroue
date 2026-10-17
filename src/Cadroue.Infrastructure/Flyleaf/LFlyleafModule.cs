using System.Runtime.CompilerServices;
using Cadroue.Core;

namespace Cadroue.Infrastructure;

internal static class LFlyleafModule
{
    [ModuleInitializer]
    internal static void LFlyleafModuleStart()
    {
        try
        {
            LPreferenceState lPreferenceState = LPreferenceStateStore.LPreferenceStateLoad()
                ?? LPreferenceDefault.LPreferenceDefaultCreate();
            LPreferenceRange.LPreferenceNormalize(lPreferenceState);
            LDepot.LDepotRootSet(lPreferenceState.LPreferenceWorkspaceFolder);
            LTrace.LTraceVerbose = lPreferenceState.LPreferenceLogVerbose;
            LFlyleaf.LFlyleafResolverAttach();
        }
        catch (Exception lException)
        {
            LTraceLog.LTraceErrorRecord("Local Flyleaf module initialization failed", lException);
        }
    }
}
