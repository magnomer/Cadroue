using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public static class LInteraction
{
    public static void LInteractionRecord(
        bool lSameSource,
        string lType,
        Func<string> lLabelRead,
        Func<string> lNameRead,
        bool lToggle,
        bool? lChecked)
    {
        if (!lSameSource)
        {
            return;
        }

        string lLabel = lLabelRead();
        LTraceLog.LTraceInteractionRecord(
            LInteractionSummaryFormat(lType, lLabel),
            LInteractionDetailFormat(lType, lLabel, lNameRead(), lToggle, lChecked));
    }

    public static string LInteractionLabelResolve(string? lAutomation, string? lContent, string? lTooltip, string lName)
    {
        if (!string.IsNullOrWhiteSpace(lAutomation))
        {
            return LInteractionTextNormalize(lAutomation);
        }

        if (!string.IsNullOrWhiteSpace(lContent))
        {
            return LInteractionTextNormalize(lContent);
        }

        if (!string.IsNullOrWhiteSpace(lTooltip))
        {
            return LInteractionTextNormalize(lTooltip);
        }

        return lName;
    }

    public static string LInteractionSummaryFormat(string lType, string lLabel) =>
        string.IsNullOrWhiteSpace(lLabel) ? $"{lType} activated" : $"{lType} activated: {lLabel}";

    public static string LInteractionDetailFormat(
        string lType,
        string lLabel,
        string lResolved,
        bool lToggle,
        bool? lChecked)
    {
        var lLines = new List<string>
        {
            $"Control: {(string.IsNullOrWhiteSpace(lLabel) ? lResolved : lLabel)}",
            $"Type: {lType}",
        };
        string lOwner = LNameplate.LNameplateOwnerRead(lResolved);
        if (lOwner.Length > 0)
        {
            lLines.Add($"Owner: {lOwner}");
        }

        if (lToggle)
        {
            lLines.Add($"State: {lChecked?.ToString() ?? "indeterminate"}");
        }

        return string.Join('\n', lLines);
    }

    private static string LInteractionTextNormalize(string lText) =>
        lText.Trim().TrimEnd('.', '!', '?').TrimEnd();
}
