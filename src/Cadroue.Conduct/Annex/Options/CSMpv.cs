using System;
using System.Threading.Tasks;

namespace Cadroue.Conduct;

public sealed class CSMpv
{
    private readonly CSMpvPort _csMpvPort;
    private bool csMpvActive;

    public CSMpv(CSMpvPort port)
    {
        ArgumentNullException.ThrowIfNull(port);
        _csMpvPort = port;
    }

    public bool CSMpvInstalledCheck() => _csMpvPort.CSMpvInstalledCheck();

    public Task<bool> CSMpvAvailabilityCheck() => _csMpvPort.CSMpvAvailabilityCheck();

    public async Task CSMpvStart(
        IProgress<double> progress,
        Action started,
        Func<bool, Task> completed,
        Func<bool, string, Task> failed)
    {
        if (csMpvActive)
        {
            return;
        }

        csMpvActive = true;
        (bool, string, bool) result;
        try
        {
            started();
            result = await _csMpvPort.CSMpvStart(progress);
        }
        finally
        {
            csMpvActive = false;
        }

        (bool success, string failure, bool installed) = result;
        if (success)
        {
            await completed(installed);
            return;
        }
        await failed(installed, failure);
    }
}
