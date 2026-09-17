using Xunit;

namespace Cadroue.Tests;

public sealed class TUsherExist
{
    [Fact]
    public void FileExist_DistinguishesFilesFoldersAndBlanks()
    {
        string file = Path.Combine(Path.GetTempPath(), $"cadroue-usher-{Guid.NewGuid():N}.tmp");
        File.WriteAllText(file, string.Empty);
        try
        {
            Assert.True(TInterface.TUsherFileExist(file));
            Assert.False(TInterface.TUsherFileExist(Path.GetTempPath()));
            Assert.False(TInterface.TUsherFileExist(null));
            Assert.False(TInterface.TUsherFileExist(" "));
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public void FolderExist_DistinguishesFoldersFilesAndBlanks()
    {
        string file = Path.Combine(Path.GetTempPath(), $"cadroue-usher-{Guid.NewGuid():N}.tmp");
        File.WriteAllText(file, string.Empty);
        try
        {
            Assert.True(TInterface.TUsherFolderExist(Path.GetTempPath()));
            Assert.False(TInterface.TUsherFolderExist(file));
            Assert.False(TInterface.TUsherFolderExist(null));
            Assert.False(TInterface.TUsherFolderExist(string.Empty));
        }
        finally
        {
            File.Delete(file);
        }
    }
}
