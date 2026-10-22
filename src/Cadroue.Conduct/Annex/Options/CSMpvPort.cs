using System;
using System.Threading.Tasks;

namespace Cadroue.Conduct;

public interface CSMpvPort
{
    bool CSMpvInstalledCheck();

    Task<bool> CSMpvAvailabilityCheck();

    Task<(bool, string, bool)> CSMpvStart(IProgress<double> progress);
}
