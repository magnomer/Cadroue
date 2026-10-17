using Cadroue.Core;

namespace Cadroue.Infrastructure;

public static class LRigFactoryUsher
{
    public static LRigUsher LRigUsherCreate()
    {
        return new LRigUsher(
            LUsher.LUsherFileExist,
            LUsher.LUsherFolderExist,
            LUsher.LUsherProgramResolve,
            LUsher.LUsherPathResolve,
            LUsher.LUsherNameRead,
            LUsher.LUsherStemRead,
            LUsher.LUsherExtensionRead,
            LUsherShell.LUsherPathOpen,
            LUsherShell.LUsherFolderOpen,
            LUsherShell.LUsherLinkOpen);
    }
}
