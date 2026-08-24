using Cadroue.Infrastructure;

using Xunit;

namespace Cadroue.Tests;

[CollectionDefinition("Placement", DisableParallelization = true)]
public sealed class TPlacementCollection
{
}

internal sealed class TPlacement : IDisposable
{
    private readonly string tPlacementPrevious;
    private readonly string tPlacementRoot;

    internal TPlacement()
    {
        tPlacementPrevious = LDepot.LDepotRootRead();
        tPlacementRoot = Path.Combine(Path.GetTempPath(), "cadroue-placement-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tPlacementRoot);
        LDepot.LDepotRootSet(tPlacementRoot);
    }

    internal string PathCurrent => Path.Combine(tPlacementRoot, "placement.json");

    internal bool Save(string key, double marker) =>
        LPlacement.LPlacementSave(key, marker, marker + 1, marker + 100, marker + 200);

    internal LPlacementRecord? Read(string key) => LPlacement.LPlacementRead(key);

    internal void Malform() => File.WriteAllText(PathCurrent, "{ invalid placement json");

    internal void BlockWrite() => Directory.CreateDirectory(PathCurrent);

    public void Dispose()
    {
        LDepot.LDepotRootSet(tPlacementPrevious);
        try
        {
            Directory.Delete(tPlacementRoot, true);
        }
        catch
        {
        }
    }
}
