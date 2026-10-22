using System;
using System.Collections.Generic;
using Cadroue.Conduct;

using Xunit;

namespace Cadroue.Tests.Conduct;

public sealed class TSpectrumPort : CSSpectrumPort
{
    private CSSpectrumImportResult tSpectrumPortResult = CSSpectrumImportResult.CSSpectrumImportLoaded;
    private string tSpectrumPortTarget = "target.json";
    private string? tSpectrumPortLoaded = "Imported";
    private CSSpectrumSnapshot tSpectrumPortSnapshot = new([]);
    private bool tSpectrumPortRemoval;
    private bool tSpectrumPortSaved;
    private readonly List<string> tSpectrumPortCalls = [];

    [Fact]
    public void CSSpectrumImport()
    {
        var port = new TSpectrumPort();
        var spectrum = new CSSpectrum("Cadroue", port);

        Assert.Equal(
            CSSpectrumImportResult.CSSpectrumImportLoaded,
            spectrum.CSSpectrumImport("source.json", out string target, out string? loaded));
        Assert.Equal("target.json", target);
        Assert.Equal("Imported", loaded);

        port.tSpectrumPortLoaded = null;
        Assert.Equal(
            CSSpectrumImportResult.CSSpectrumImportInvalid,
            spectrum.CSSpectrumImport("source.json", out _, out _));

        port.tSpectrumPortResult = CSSpectrumImportResult.CSSpectrumImportReserved;
        Assert.Equal(
            CSSpectrumImportResult.CSSpectrumImportReserved,
            spectrum.CSSpectrumImport("source.json", out _, out _));
        Assert.Equal(["import", "import", "import"], port.tSpectrumPortCalls);
    }

    [Fact]
    public void CSSpectrumUpdate()
    {
        var port = new TSpectrumPort
        {
            tSpectrumPortSnapshot = new CSSpectrumSnapshot([
                new CSSpectrumRow("Cadroue", ["#000000"], true, null),
                new CSSpectrumRow("Custom", ["#FFFFFF"], false, "Custom.json")
            ])
        };
        var spectrum = new CSSpectrum("Custom", port);

        Assert.Same(port.tSpectrumPortSnapshot, spectrum.CSSpectrumUpdate());
        Assert.False(spectrum.CSSpectrumRemove("Cadroue", true, null));
        Assert.Equal("Custom", spectrum.CSSpectrumName);

        Assert.False(spectrum.CSSpectrumRemove("Custom", false, "Custom.json"));
        Assert.Equal("Custom", spectrum.CSSpectrumName);

        port.tSpectrumPortRemoval = true;
        Assert.True(spectrum.CSSpectrumRemove("Custom", false, "Custom.json"));
        Assert.Equal("Cadroue", spectrum.CSSpectrumName);
        Assert.Equal(["read", "remove:Custom:False:Custom.json", "remove:Custom:False:Custom.json"],
            port.tSpectrumPortCalls);
    }

    [Fact]
    public void CSSpectrumSave()
    {
        var port = new TSpectrumPort();
        var spectrum = new CSSpectrum("Cadroue", port);

        spectrum.CSSpectrumSave("first.json");
        port.tSpectrumPortSaved = true;
        spectrum.CSSpectrumSave("second.json");

        Assert.Equal(["save:first.json:Cadroue:False", "save:second.json:Cadroue:True"],
            port.tSpectrumPortCalls);
    }

    CSSpectrumImportResult CSSpectrumPort.CSSpectrumPortImport(
        string sourcePath, out string targetPath, out string? loadedName)
    {
        tSpectrumPortCalls.Add("import");
        targetPath = tSpectrumPortTarget;
        loadedName = tSpectrumPortLoaded;
        return tSpectrumPortResult;
    }

    CSSpectrumSnapshot CSSpectrumPort.CSSpectrumPortRead()
    {
        tSpectrumPortCalls.Add("read");
        return tSpectrumPortSnapshot;
    }

    bool CSSpectrumPort.CSSpectrumPortRemove(string name, bool native, string? path)
    {
        tSpectrumPortCalls.Add($"remove:{name}:{native}:{path}");
        return tSpectrumPortRemoval;
    }

    bool CSSpectrumPort.CSSpectrumPortSave(string targetPath, string name)
    {
        tSpectrumPortCalls.Add($"save:{targetPath}:{name}:{tSpectrumPortSaved}");
        return tSpectrumPortSaved;
    }
}
