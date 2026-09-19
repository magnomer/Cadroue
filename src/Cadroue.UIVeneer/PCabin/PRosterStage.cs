using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.Core;
using Cadroue.Application;
using Cadroue.UIVeneer.PAsset;

namespace Cadroue.UIVeneer.PCabin;

public sealed partial class PRoster
{
    private static HashSet<string> PRosterConsumedRead(IReadOnlyList<LWorkItem> pBatchItems)
    {
        var pConsumed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (LWorkItem pWorkItem in pBatchItems)
        {
            IEnumerable<string> pInputs = pWorkItem.LWorkKind == LWorkKind.LWorkKindMerge
                ? pWorkItem.LWorkMergeSources
                : new[] { pWorkItem.LWorkSourcePath };
            foreach (string pInput in pInputs)
            {
                if (PLineagePathRead(pInput) is { } pInputKey)
                {
                    pConsumed.Add(pInputKey);
                }
            }
        }

        return pConsumed;
    }

    private static HashSet<string> PRosterProducedRead(IReadOnlyList<LWorkItem> pBatchItems)
    {
        var pProduced = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (LWorkItem pWorkItem in pBatchItems)
        {
            if (PLineagePathRead(pWorkItem.LWorkOutputPath) is { } pOutputKey)
            {
                pProduced.Add(pOutputKey);
            }
        }

        return pProduced;
    }

    private static bool PRosterStageCheck(LWorkItem pWorkItem, HashSet<string> pConsumed, HashSet<string> pProduced)
    {
        if (PLineagePathRead(pWorkItem.LWorkOutputPath) is not { } pOutputKey || !pConsumed.Contains(pOutputKey))
        {
            return false;
        }

        IEnumerable<string> pInputs = pWorkItem.LWorkKind == LWorkKind.LWorkKindMerge
            ? pWorkItem.LWorkMergeSources
            : new[] { pWorkItem.LWorkSourcePath };
        return pInputs.Any(pInput => PLineagePathRead(pInput) is { } pInputKey && pProduced.Contains(pInputKey));
    }

    private static int PRosterInitialRead(IReadOnlyList<LWorkItem> pBatchItems)
    {
        var pOutputs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (LWorkItem pWorkItem in pBatchItems)
        {
            if (PLineagePathRead(pWorkItem.LWorkOutputPath) is { } pOutputKey)
            {
                pOutputs.Add(pOutputKey);
            }
        }

        var pInitials = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (LWorkItem pWorkItem in pBatchItems)
        {
            IEnumerable<string> pInputs = pWorkItem.LWorkKind == LWorkKind.LWorkKindMerge
                ? pWorkItem.LWorkMergeSources
                : new[] { pWorkItem.LWorkSourcePath };
            foreach (string pInput in pInputs)
            {
                if (PLineagePathRead(pInput) is { } pInputKey && !pOutputs.Contains(pInputKey))
                {
                    pInitials.Add(pInputKey);
                }
            }
        }

        return pInitials.Count;
    }
}
