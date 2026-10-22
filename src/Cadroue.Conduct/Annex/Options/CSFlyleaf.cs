using System;
using System.Threading.Tasks;

namespace Cadroue.Conduct;

public sealed class CSFlyleaf
{
    private readonly CSFlyleafPort _csFlyleafPort;

    public CSFlyleaf(CSFlyleafPort port)
    {
        ArgumentNullException.ThrowIfNull(port);
        _csFlyleafPort = port;
    }

    public bool CSFlyleafInstalledCheck() => _csFlyleafPort.CSFlyleafInstalledCheck();

    public async Task<(bool, string, bool)> CSFlyleafStart(IProgress<double> progress) =>
        await _csFlyleafPort.CSFlyleafStart(progress);
}
