using System;
using Cadroue.Conduct;
using Cadroue.Core;

namespace Cadroue.Tests.Conduct;

internal static partial class TInterface
{
    internal static CExportRow TInterfaceExportRead(
        Func<string, bool> native,
        Func<string, LPresetDocument, bool> matched,
        Func<string, LPresetDocument, bool, bool> supported) =>
        new(native, matched, supported);

    internal static CExportRowState TInterfaceExportRead(
        CExportRow row,
        string name,
        string? selected,
        LPresetDocument working) =>
        row.CExportRowRead(name, selected, working);

    internal static LPresetDocument TInterfaceExportRead(string name) => new() { LPresetName = name };

    internal static CExportTransfer TInterfaceExportRead(
        Func<LPresetDocument> value,
        Func<string?> selected,
        Func<string, bool> catalogue,
        Func<LPresetDocument, string, bool> fileSave,
        Func<string, LPresetDocument?> fileLoad,
        Func<string, string, string> nameResolve,
        Func<string, string> nameCreate,
        Func<string, LPresetDocument, bool> presetSave,
        Action<string> select,
        Func<string, string> format) =>
        new(
            value,
            selected,
            catalogue,
            fileSave,
            fileLoad,
            nameResolve,
            nameCreate,
            presetSave,
            select,
            format);

    internal static (string, LPresetDocument) TInterfaceExportRead(CExportTransfer transfer) =>
        transfer.CExportTransferResolve();

    internal static (string, Func<string, CExportTransferResult>) TInterfaceExportRead(
        CExportTransfer transfer,
        bool save) =>
        transfer.CExportTransferSave();

    internal static CExportTransferResult TInterfaceExportRead(
        CExportTransfer transfer,
        string path,
        string fallback) =>
        transfer.CExportTransferImport(path, fallback);
}
