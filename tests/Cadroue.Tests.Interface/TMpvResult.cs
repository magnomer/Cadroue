using System.IO;

using Cadroue.Core;
using Cadroue.Infrastructure;

using Xunit;

namespace Cadroue.Tests.Interface;

[CollectionDefinition("MpvResult", DisableParallelization = true)]
public sealed class TMpvResultCollection;

[Collection("MpvResult")]
public sealed class TMpvResult : IDisposable
{
    private static readonly string TMpvResultPath = Path.Combine(LDepot.LDepotHomeRead(), "mpvprobe.json");

    private readonly string? tMpvBackup;

    public TMpvResult()
    {
        tMpvBackup = File.Exists(TMpvResultPath) ? File.ReadAllText(TMpvResultPath) : null;
        TMpvResultDelete();
    }

    public void Dispose()
    {
        if (tMpvBackup is null)
        {
            TMpvResultDelete();
        }
        else
        {
            File.WriteAllText(TMpvResultPath, tMpvBackup);
        }
    }

    [Fact]
    public void LMpvResultRead_SameStamp_ReturnsSavedOutcome()
    {
        LMpvResult.LMpvResultSave(LMpvProbe.LMpvProbeUsable, "stamp-a");

        Assert.Equal(LMpvProbe.LMpvProbeUsable, LMpvResult.LMpvResultRead("stamp-a"));
    }

    [Fact]
    public void LMpvResultRead_ChangedStamp_ReturnsUnknown()
    {
        LMpvResult.LMpvResultSave(LMpvProbe.LMpvProbeUsable, "stamp-a");

        Assert.Equal(LMpvProbe.LMpvProbeUnknown, LMpvResult.LMpvResultRead("stamp-b"));
    }

    [Fact]
    public void LMpvResultRead_MissingFile_ReturnsUnknown()
    {
        TMpvResultDelete();

        Assert.Equal(LMpvProbe.LMpvProbeUnknown, LMpvResult.LMpvResultRead("stamp-a"));
    }

    [Fact]
    public void LMpvResultRead_GarbageFile_ReturnsUnknown()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(TMpvResultPath)!);
        File.WriteAllText(TMpvResultPath, "{ not valid json ]");

        Assert.Equal(LMpvProbe.LMpvProbeUnknown, LMpvResult.LMpvResultRead("stamp-a"));
    }

    [Fact]
    public void LMpvStampCreate_DifferentAppVersion_ProducesDifferentStamp()
    {
        string tFirst = LMpvResult.LMpvStampCreate("1.0.0", "libmpv-2.dll");
        string tSecond = LMpvResult.LMpvStampCreate("2.0.0", "libmpv-2.dll");

        Assert.NotEqual(tFirst, tSecond);
    }

    private static void TMpvResultDelete()
    {
        if (File.Exists(TMpvResultPath))
        {
            File.Delete(TMpvResultPath);
        }
    }
}
