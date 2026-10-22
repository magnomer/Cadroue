using System;
using System.Threading.Tasks;

namespace Cadroue.Conduct;

public sealed class CSMpv
{
    private readonly CSMpvPort _csMpvPort;

    public CSMpv(CSMpvPort port)
    {
        ArgumentNullException.ThrowIfNull(port);
        _csMpvPort = port;
    }

    public bool CSMpvInstalledCheck() => _csMpvPort.CSMpvInstalledCheck();

    public Task<bool> CSMpvAvailabilityCheck() => _csMpvPort.CSMpvAvailabilityCheck();

    public async Task<(bool, string, bool)> CSMpvStart(IProgress<double> progress) =>
        await _csMpvPort.CSMpvStart(progress);
}
