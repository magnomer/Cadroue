using Cadroue.UIDeportment;
using Cadroue.UIDeportment.Capsule;

namespace Cadroue.Tests.Windows;

internal sealed class TPlacement : IDisposable
{
    private readonly Func<string> tPlacementPrevious;
    private readonly string tPlacementRoot;

    internal TPlacement()
    {
        tPlacementPrevious = QPlacement.QPlacementRootSeam;
        tPlacementRoot = Path.Combine(Path.GetTempPath(), "cadroue-placement-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tPlacementRoot);
        QPlacement.QPlacementRootSeam = () => tPlacementRoot;
    }

    internal string TPlacementPath => Path.Combine(tPlacementRoot, "placement.json");

    internal bool TPlacementSave(string key, double marker) =>
        QPlacement.QPlacementSave(key, marker, marker + 1, marker + 100, marker + 200);

    internal LPlacement? TPlacementRead(string key) =>
        QPlacement.QPlacementRead(key, out double left, out double top, out double width, out double height)
            ? new LPlacement(left, top, width, height)
            : null;

    internal void TPlacementMalformSave() => File.WriteAllText(TPlacementPath, "{ invalid placement json");

    internal void TPlacementBlockCreate() => Directory.CreateDirectory(TPlacementPath);

    public void Dispose()
    {
        QPlacement.QPlacementRootSeam = tPlacementPrevious;
        try
        {
            Directory.Delete(tPlacementRoot, true);
        }
        catch
        {
        }
    }
}
