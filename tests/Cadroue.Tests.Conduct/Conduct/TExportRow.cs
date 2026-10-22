using System;
using Cadroue.Conduct;
using Cadroue.Core;
using Xunit;

namespace Cadroue.Tests.Conduct;

public sealed class TExportRow
{
    [Fact]
    public void RowRead_NativeSelectionMatchAndSupport_PreserveShortCircuitAndWorkingIdentity()
    {
        LPresetDocument working = TInterface.TInterfaceExportRead("Working");
        LPresetDocument? matchedWorking = null;
        LPresetDocument? supportedWorking = null;
        int matches = 0;
        CExportRow row = TInterface.TInterfaceExportRead(
            name => name.StartsWith("Native", StringComparison.Ordinal),
            (name, document) =>
            {
                matches++;
                matchedWorking = document;
                return name == "Matched";
            },
            (name, document, _) =>
            {
                supportedWorking = document;
                return name != "Unsupported";
            });

        CExportRowState native = TInterface.TInterfaceExportRead(row, "Native One", "native one", working);
        Assert.True(native.CExportRowNative);
        Assert.True(native.CExportRowSelected);
        Assert.False(native.CExportRowModified);
        Assert.True(native.CExportRowSupported);
        Assert.Equal(0, matches);

        CExportRowState unselected = TInterface.TInterfaceExportRead(row, "Custom", "Other", working);
        Assert.False(unselected.CExportRowSelected);
        Assert.False(unselected.CExportRowModified);
        Assert.Equal(0, matches);

        CExportRowState matched = TInterface.TInterfaceExportRead(row, "Matched", "matched", working);
        Assert.False(matched.CExportRowModified);

        CExportRowState modified = TInterface.TInterfaceExportRead(row, "Changed", "CHANGED", working);
        Assert.True(modified.CExportRowModified);

        CExportRowState unsupported = TInterface.TInterfaceExportRead(row, "Unsupported", "Other", working);
        Assert.False(unsupported.CExportRowSupported);
        Assert.Same(working, matchedWorking);
        Assert.Same(working, supportedWorking);
        Assert.Equal(2, matches);
    }
}
