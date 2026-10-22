using System;
using Cadroue.Core;

namespace Cadroue.Conduct;

public sealed record CExportTransferResult(
    bool? CExportTransferOutcome,
    string CExportTransferName,
    string? CExportTransferDetail);

public sealed class CExportTransfer(
    Func<LPresetDocument> value,
    Func<string?> selected,
    Func<string, bool> catalogue,
    Func<LPresetDocument, string, bool> fileSave,
    Func<string, LPresetDocument?> fileLoad,
    Func<string, string, string> nameResolve,
    Func<string, string> nameCreate,
    Func<string, LPresetDocument, bool> presetSave,
    Action<string> select,
    Func<string, string> format)
{
    public (string, LPresetDocument) CExportTransferResolve()
    {
        LPresetDocument record = value();
        string name = string.IsNullOrWhiteSpace(record.LPresetName)
            ? selected() ?? string.Empty
            : record.LPresetName;
        return (format(name), record);
    }

    public (string, Func<string, CExportTransferResult>) CExportTransferSave()
    {
        (string name, LPresetDocument record) = CExportTransferResolve();
        return (name, path =>
        {
            if (catalogue(path))
            {
                return new(null, path, null);
            }

            return new(fileSave(record, path), path, null);
        });
    }

    public CExportTransferResult CExportTransferImport(string path, string fallback)
    {
        LPresetDocument? record;
        try
        {
            record = fileLoad(path);
        }
        catch (Exception error)
        {
            return new(null, path, error.Message);
        }

        if (record is null)
        {
            return new(null, path, null);
        }

        string imported = nameResolve(record.LPresetName, path);
        string name = nameCreate(string.IsNullOrWhiteSpace(imported) ? fallback : imported);
        record.LPresetName = name;
        if (!presetSave(name, record))
        {
            return new(false, name, null);
        }

        select(name);
        return new(true, name, null);
    }
}
