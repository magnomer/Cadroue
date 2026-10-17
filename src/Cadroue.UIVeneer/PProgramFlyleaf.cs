using Cadroue.Infrastructure;
using Cadroue.UIVeneer.PHouse;
using FlyleafLib;

namespace Cadroue.UIVeneer;

internal static class PProgramFlyleaf
{
    internal static void PProgramFlyleafStart()
    {
        EngineConfig pConfig = new() { UIRefresh = false, UIRefreshInterval = 250 };
        PProgramLogApply(pConfig, LTrace.LTraceVerbose);
        pConfig.FFmpegPath = LRendererFolder.LRendererFfmpegResolve(pConfig.FFmpegPath);
        Engine.Start(pConfig);
    }

    internal static void PProgramVerboseApply(bool pVerbose) => PProgramLogApply(Engine.Config, pVerbose);

    private static void PProgramLogApply(EngineConfig pConfig, bool pVerbose)
    {
        pConfig.LogLevel = PLook.PLookFlyleafLog[pVerbose];
        pConfig.LogOutput = LRendererLog.LRendererLogResolve(pVerbose);
    }
}
