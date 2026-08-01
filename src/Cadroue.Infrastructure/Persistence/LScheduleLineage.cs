using Cadroue.Core;

namespace Cadroue.Infrastructure;

public static class LScheduleLineage
{
    internal static Guid LScheduleLineageRead(LWorkItem lWorkItem) =>
        lWorkItem.LWorkLineage == Guid.Empty
            ? LScheduleRootRead(lWorkItem)
            : lWorkItem.LWorkLineage;

    internal static Guid LScheduleLineageResolve(LWorkItem lWorkItem, IEnumerable<LWorkItem> lScheduleItems)
    {
        if (lWorkItem.LWorkKind == LWorkKind.LWorkKindMerge)
        {
            return Guid.NewGuid();
        }

        LWorkItem? lScheduleParent = LScheduleParentFind(lWorkItem.LWorkSourcePath, lScheduleItems);
        return lScheduleParent is not null && lScheduleParent.LWorkKind != LWorkKind.LWorkKindSplit
            ? LScheduleLineageRead(lScheduleParent)
            : LScheduleRootRead(lWorkItem);
    }

    private static Guid LScheduleRootRead(LWorkItem lWorkItem)
    {
        string lScheduleKey;
        try
        {
            lScheduleKey = Path.GetFullPath(lWorkItem.LWorkSourcePath).ToLowerInvariant();
        }
        catch (Exception lScheduleError) when (
            lScheduleError is ArgumentException or IOException or NotSupportedException)
        {
            lScheduleKey = lWorkItem.LWorkSourcePath.ToLowerInvariant();
        }

        byte[] lScheduleHash = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes($"{lWorkItem.LWorkBatchId:N}|{lScheduleKey}"));
        return new Guid(lScheduleHash.AsSpan(0, 16));
    }

    private static LWorkItem? LScheduleParentFind(string lWorkSourcePath, IEnumerable<LWorkItem> lScheduleItems)
    {
        if (string.IsNullOrWhiteSpace(lWorkSourcePath))
        {
            return null;
        }

        LWorkItem? lScheduleParent = null;
        foreach (LWorkItem lScheduleItem in lScheduleItems)
        {
            if (!LSchedulePathMatch(lScheduleItem.LWorkOutputPath, lWorkSourcePath))
            {
                continue;
            }

            if (lScheduleParent is null || lScheduleItem.LWorkCreateTime > lScheduleParent.LWorkCreateTime)
            {
                lScheduleParent = lScheduleItem;
            }
        }

        return lScheduleParent;
    }

    private static bool LSchedulePathMatch(string lScheduleLeft, string lScheduleRight)
    {
        if (string.IsNullOrWhiteSpace(lScheduleLeft) || string.IsNullOrWhiteSpace(lScheduleRight))
        {
            return false;
        }

        try
        {
            return string.Equals(
                Path.GetFullPath(lScheduleLeft),
                Path.GetFullPath(lScheduleRight),
                StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception lScheduleError) when (
            lScheduleError is ArgumentException or IOException or NotSupportedException)
        {
            return string.Equals(lScheduleLeft, lScheduleRight, StringComparison.OrdinalIgnoreCase);
        }
    }

    public static Guid LScheduleFileRead(string lWorkFilePath)
    {
        if (string.IsNullOrWhiteSpace(lWorkFilePath))
        {
            return Guid.NewGuid();
        }

        string lScheduleKey;
        try
        {
            lScheduleKey = Path.GetFullPath(lWorkFilePath).ToLowerInvariant();
        }
        catch (Exception lScheduleError) when (
            lScheduleError is ArgumentException or IOException or NotSupportedException)
        {
            lScheduleKey = lWorkFilePath.ToLowerInvariant();
        }

        byte[] lScheduleHash = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(lScheduleKey));
        return new Guid(lScheduleHash.AsSpan(0, 16));
    }
}
