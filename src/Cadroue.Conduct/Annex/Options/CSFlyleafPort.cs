using System;
using System.Threading.Tasks;

namespace Cadroue.Conduct;

public interface CSFlyleafPort
{
    bool CSFlyleafInstalledCheck();

    Task<(bool, string, bool)> CSFlyleafStart(IProgress<double> progress);
}
