using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

[Collection("Logging")]
public sealed class TProgramDepot : IDisposable
{
    private readonly LPreferenceState tDepotPrevious = TInterface.TPreferenceCurrentRead();
    private readonly string tDepotRootPrevious = TInterface.TDepotRootRead();
    private readonly string tDepotParent = Path.Combine(
        Path.GetTempPath(), "Cadroue.Tests", "program-depot", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        TInterface.TDepotIndexRelease();
        TInterface.TPreferenceRestore(tDepotPrevious);
        TInterface.TDepotRootSet(tDepotRootPrevious);
        try
        {
            if (Directory.Exists(tDepotParent))
            {
                Directory.Delete(tDepotParent, recursive: true);
            }
        }
        catch (IOException)
        {
        }
    }

    private static void TDepotFolderSet(string folder)
    {
        LPreferenceState draft = TInterface.TPreferenceClone(TInterface.TPreferenceCurrentRead());
        draft.LPreferenceWorkspaceFolder = folder;
        Assert.True(TInterface.TPreferenceRestore(draft));
    }

    [Fact]
    public void Apply_UnchangedRootIsNoOp_ChangedRootMovesOnce()
    {
        string first = Path.Combine(tDepotParent, "first");
        string second = Path.Combine(tDepotParent, "second");
        LProgram program = TInterface.TProgramCreate();

        TDepotFolderSet(first);
        Assert.True(TInterface.TProgramDepotApply(program));
        Assert.Equal(first, TInterface.TProgramDepotRead(program));
        Assert.Equal(first, TInterface.TDepotRootRead());
        Assert.True(File.Exists(TInterface.TDepotIndexFind()));

        Assert.True(TInterface.TProgramDepotApply(program));
        Assert.Equal(first, TInterface.TProgramDepotRead(program));

        TDepotFolderSet(second);
        Assert.True(TInterface.TProgramDepotApply(program));
        Assert.Equal(second, TInterface.TProgramDepotRead(program));
        Assert.Equal(second, TInterface.TDepotRootRead());
        Assert.False(Directory.Exists(first));
        Assert.True(File.Exists(TInterface.TDepotIndexFind()));
    }

    [Fact]
    public void Apply_OccupiedTarget_RefusesAndKeepsAppliedRoot()
    {
        string first = Path.Combine(tDepotParent, "first");
        string occupied = Path.Combine(tDepotParent, "occupied");
        Directory.CreateDirectory(occupied);
        File.WriteAllText(Path.Combine(occupied, "keep.txt"), "occupied");
        LProgram program = TInterface.TProgramCreate();

        TDepotFolderSet(first);
        Assert.True(TInterface.TProgramDepotApply(program));

        TDepotFolderSet(occupied);
        Assert.False(TInterface.TProgramDepotApply(program));
        Assert.Equal(first, TInterface.TProgramDepotRead(program));
        Assert.Equal(first, TInterface.TDepotRootRead());
        Assert.True(Directory.Exists(first));
    }
}
