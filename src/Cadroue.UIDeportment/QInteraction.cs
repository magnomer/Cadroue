using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public static class QInteraction
{
    public static void QInteractionRecord(
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
            QInteractionSummaryFormat(lType, lLabel),
            QInteractionDetailFormat(lType, lLabel, lNameRead(), lToggle, lChecked));
    }

    public static string QInteractionLabelResolve(string? lAutomation, string? lContent, string? lTooltip, string lName)
    {
        if (!string.IsNullOrWhiteSpace(lAutomation))
        {
            return QInteractionTextNormalize(lAutomation);
        }

        if (!string.IsNullOrWhiteSpace(lContent))
        {
            return QInteractionTextNormalize(lContent);
        }

        if (!string.IsNullOrWhiteSpace(lTooltip))
        {
            return QInteractionTextNormalize(lTooltip);
        }

        return lName;
    }

    public static string QInteractionSummaryFormat(string lType, string lLabel) =>
        string.IsNullOrWhiteSpace(lLabel) ? $"{lType} activated" : $"{lType} activated: {lLabel}";

    public static string QInteractionDetailFormat(
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
        string lOwner = QNameplate.QNameplateOwnerRead(lResolved);
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

    private static string QInteractionTextNormalize(string lText) =>
        lText.Trim().TrimEnd('.', '!', '?').TrimEnd();
}
