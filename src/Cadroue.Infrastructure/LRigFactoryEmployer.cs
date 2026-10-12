using Cadroue.Core;
using Cadroue.Media;

namespace Cadroue.Infrastructure;

public static class LRigFactoryEmployer
{
    public static LRigEmployer LRigEmployerCreate()
    {
        return new LRigEmployer(
            (program, prefix, family, level, idle, arguments, token, work, output, error) =>
                new LEmployer(program, prefix)
                {
                    LEmployerFamily = family,
                    LEmployerLevel = level,
                    LEmployerIdleLimit = idle
                }.LEmployerRun(arguments, token, work, output, error),
            (program, family, level, idle, arguments, token, output) =>
                new LEmployer(program)
                {
                    LEmployerFamily = family,
                    LEmployerLevel = level,
                    LEmployerIdleLimit = idle
                }.LEmployerRun(arguments, token, output),
            LEmployerChild.LEmployerChildSuspend,
            LEmployerChild.LEmployerChildResume,
            LEmployerChild.LEmployerChildCheck);
    }
}
