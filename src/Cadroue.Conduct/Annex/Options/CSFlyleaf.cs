using System;
using System.Threading.Tasks;

namespace Cadroue.Conduct;

public sealed class CSFlyleaf
{
    private readonly CSFlyleafPort _csFlyleafPort;
    private bool csFlyleafActive;

    public CSFlyleaf(CSFlyleafPort port)
    {
        ArgumentNullException.ThrowIfNull(port);
        _csFlyleafPort = port;
    }

    public bool CSFlyleafInstalledCheck() => _csFlyleafPort.CSFlyleafInstalledCheck();

    public async Task CSFlyleafStart(
        IProgress<double> progress,
        Action started,
        Action<bool> completed,
        Action<bool, string> failed)
    {
        if (csFlyleafActive)
        {
            return;
        }

        csFlyleafActive = true;
        (bool, string, bool) result;
        try
        {
            started();
            result = await _csFlyleafPort.CSFlyleafStart(progress);
        }
        finally
        {
            csFlyleafActive = false;
        }

        (bool success, string failure, bool installed) = result;
        if (success)
        {
            completed(installed);
            return;
        }
        failed(installed, failure);
    }
}
