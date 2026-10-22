using System;
using System.Collections.Generic;
using Cadroue.Conduct;
using Cadroue.Core;
using Xunit;

namespace Cadroue.Tests.Conduct;

public sealed class TExportTransfer
{
    [Fact]
    public void TransferResolve_CurrentNameOrSelection_FormatsTheResolvedName()
    {
        LPresetDocument first = TInterface.TInterfaceExportRead(" Current ");
        LPresetDocument second = TInterface.TInterfaceExportRead("Replacement");
        LPresetDocument current = first;
        LPresetDocument? written = null;
        CExportTransfer transfer = TExportTransferCreate(
            value: () => current,
            selected: () => "Selected",
            fileSave: (record, _) =>
            {
                written = record;
                return true;
            },
            format: name => $"<{name}>");

        (string name, LPresetDocument prepared) = TInterface.TInterfaceExportRead(transfer);
        Assert.Equal("< Current >", name);
        Assert.Same(first, prepared);

        (string preparedName, Func<string, CExportTransferResult> save) =
            TInterface.TInterfaceExportRead(transfer, true);
        Assert.Equal("< Current >", preparedName);
        current = second;
        _ = save("prepared.json");
        Assert.Same(first, written);

        second.LPresetName = " ";
        Assert.Equal("<Selected>", TInterface.TInterfaceExportRead(transfer).Item1);
    }

    [Fact]
    public void TransferSave_CatalogueWriteFailureAndSuccess_AreDistinctAndOrdered()
    {
        var calls = new List<string>();
        bool catalogue = true;
        bool saved = false;
        CExportTransfer transfer = TExportTransferCreate(
            catalogue: path =>
            {
                calls.Add($"catalogue:{path}");
                return catalogue;
            },
            fileSave: (_, path) =>
            {
                calls.Add($"write:{path}");
                return saved;
            });

        CExportTransferResult refused = TInterface.TInterfaceExportRead(transfer, true).Item2("catalogue.json");
        Assert.Null(refused.CExportTransferOutcome);
        Assert.Equal("catalogue.json", refused.CExportTransferName);
        Assert.Null(refused.CExportTransferDetail);
        Assert.Equal(["catalogue:catalogue.json"], calls);

        calls.Clear();
        catalogue = false;
        CExportTransferResult failed = TInterface.TInterfaceExportRead(transfer, true).Item2("failed.json");
        Assert.False(failed.CExportTransferOutcome);
        Assert.Equal("failed.json", failed.CExportTransferName);
        Assert.Equal(["catalogue:failed.json", "write:failed.json"], calls);

        calls.Clear();
        saved = true;
        CExportTransferResult succeeded = TInterface.TInterfaceExportRead(transfer, true).Item2("saved.json");
        Assert.True(succeeded.CExportTransferOutcome);
        Assert.Equal("saved.json", succeeded.CExportTransferName);
        Assert.Equal(["catalogue:saved.json", "write:saved.json"], calls);
    }

    [Fact]
    public void TransferImport_ThrowAndNull_AreReadOutcomesWithoutMutation()
    {
        int saves = 0;
        int selections = 0;
        CExportTransfer throwing = TExportTransferCreate(
            fileLoad: _ => throw new InvalidOperationException("broken"),
            presetSave: (_, _) =>
            {
                saves++;
                return true;
            },
            select: _ => selections++);

        CExportTransferResult unreadable = TInterface.TInterfaceExportRead(throwing, "broken.json", "Imported");
        Assert.Null(unreadable.CExportTransferOutcome);
        Assert.Equal("broken.json", unreadable.CExportTransferName);
        Assert.Equal("broken", unreadable.CExportTransferDetail);

        CExportTransfer invalid = TExportTransferCreate(
            fileLoad: _ => null,
            presetSave: (_, _) =>
            {
                saves++;
                return true;
            },
            select: _ => selections++);
        CExportTransferResult missing = TInterface.TInterfaceExportRead(invalid, "invalid.json", "Imported");
        Assert.Null(missing.CExportTransferOutcome);
        Assert.Equal("invalid.json", missing.CExportTransferName);
        Assert.Null(missing.CExportTransferDetail);
        Assert.Equal(0, saves);
        Assert.Equal(0, selections);
    }

    [Fact]
    public void TransferImport_BlankNameUsesFallback_SavesBeforeSelectingAndStopsOnFailure()
    {
        var calls = new List<string>();
        LPresetDocument record = TInterface.TInterfaceExportRead(" ");
        bool saved = false;
        CExportTransfer transfer = TExportTransferCreate(
            fileLoad: path =>
            {
                calls.Add($"read:{path}");
                return record;
            },
            nameResolve: (name, path) =>
            {
                calls.Add($"resolve:{name}:{path}");
                return name.Trim();
            },
            nameCreate: name =>
            {
                calls.Add($"unique:{name}");
                return $"{name} 2";
            },
            presetSave: (name, value) =>
            {
                calls.Add($"save:{name}:{value.LPresetName}");
                return saved;
            },
            select: name => calls.Add($"select:{name}"));

        CExportTransferResult failed = TInterface.TInterfaceExportRead(transfer, "blank.json", "Imported");
        Assert.False(failed.CExportTransferOutcome);
        Assert.Equal("Imported 2", failed.CExportTransferName);
        Assert.Equal("Imported 2", record.LPresetName);
        Assert.DoesNotContain("select:Imported 2", calls);

        calls.Clear();
        saved = true;
        CExportTransferResult succeeded = TInterface.TInterfaceExportRead(transfer, "blank.json", "Imported");
        Assert.True(succeeded.CExportTransferOutcome);
        Assert.Equal("Imported 2 2", succeeded.CExportTransferName);
        Assert.Equal(
            [
                "read:blank.json",
                "resolve:Imported 2:blank.json",
                "unique:Imported 2",
                "save:Imported 2 2:Imported 2 2",
                "select:Imported 2 2"
            ],
            calls);
    }

    private static CExportTransfer TExportTransferCreate(
        Func<LPresetDocument>? value = null,
        Func<string?>? selected = null,
        Func<string, bool>? catalogue = null,
        Func<LPresetDocument, string, bool>? fileSave = null,
        Func<string, LPresetDocument?>? fileLoad = null,
        Func<string, string, string>? nameResolve = null,
        Func<string, string>? nameCreate = null,
        Func<string, LPresetDocument, bool>? presetSave = null,
        Action<string>? select = null,
        Func<string, string>? format = null) =>
        TInterface.TInterfaceExportRead(
            value ?? (() => TInterface.TInterfaceExportRead("Selected")),
            selected ?? (() => "Selected"),
            catalogue ?? (_ => false),
            fileSave ?? ((_, _) => true),
            fileLoad ?? (_ => TInterface.TInterfaceExportRead("Imported")),
            nameResolve ?? ((name, _) => name),
            nameCreate ?? (name => name),
            presetSave ?? ((_, _) => true),
            select ?? (_ => { }),
            format ?? (name => name));
}
