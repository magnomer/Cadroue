using System;
using Cadroue.Conduct;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

public readonly record struct QExportTransferResult(CExportTransferResult CExportTransferResult);

public sealed class QExportTransfer(CExportTransfer transfer)
{
    public void QExportTransferSave(
        Func<string, (bool?, string)> prompt,
        Action<string> failure,
        Action<string, string?> refusal)
    {
        (string name, Func<string, CExportTransferResult> save) = transfer.CExportTransferSave();
        (bool? confirmed, string path) = prompt(name);
        if (confirmed == true)
        {
            QExportTransferHandle(new(save(path)), _ => { }, failure, refusal);
        }
    }

    public void QExportTransferImport(
        bool? confirmed,
        string path,
        string fallback,
        Action<string> failure,
        Action<string, string?> refusal)
    {
        if (confirmed == true)
        {
            QExportTransferHandle(
                new(transfer.CExportTransferImport(path, fallback)),
                _ => { },
                failure,
                refusal);
        }
    }

    public void QExportTransferHandle(
        QExportTransferResult result,
        Action<string> success,
        Action<string> failure,
        Action<string, string?> refusal)
    {
        CExportTransferResult transferResult = result.CExportTransferResult;
        if (transferResult.CExportTransferOutcome == true)
        {
            success(transferResult.CExportTransferName);
        }
        else if (transferResult.CExportTransferOutcome == false)
        {
            failure(transferResult.CExportTransferName);
        }
        else
        {
            refusal(transferResult.CExportTransferName, transferResult.CExportTransferDetail);
        }
    }
}
