using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

using Cadroue.Infrastructure;

namespace Cadroue.UIShell.PMainWindow;

internal static class PInteraction
{
    internal static void PInteractionButtonAttach(ButtonBase pButton) =>
        pButton.AddHandler(
            ButtonBase.ClickEvent,
            new RoutedEventHandler(PInteractionButtonHandle),
            handledEventsToo: true);

    internal static void PInteractionButtonHandle(object pSender, RoutedEventArgs pEvent)
    {
        if (pSender is not ButtonBase pButton || !ReferenceEquals(pSender, pEvent.OriginalSource))
        {
            return;
        }

        string pInteractionType = pButton.GetType().Name;
        string pInteractionLabel = PInteractionLabelRead(pButton, pButton.Content);
        LTraceLog.LTraceInteractionRecord(
            PInteractionSummaryRead(pInteractionType, pInteractionLabel),
            PInteractionDetailRead(pButton, pInteractionType, pInteractionLabel));
    }

    internal static void PInteractionMenuHandle(object pSender, RoutedEventArgs pEvent)
    {
        if (pSender is not MenuItem pMenuItem || !ReferenceEquals(pSender, pEvent.OriginalSource))
        {
            return;
        }

        string pInteractionLabel = PInteractionLabelRead(pMenuItem, pMenuItem.Header);
        LTraceLog.LTraceInteractionRecord(
            PInteractionSummaryRead(nameof(MenuItem), pInteractionLabel),
            PInteractionDetailRead(pMenuItem, nameof(MenuItem), pInteractionLabel));
    }

    private static string PInteractionLabelRead(FrameworkElement pElement, object? pContent)
    {
        string pAutomationName = AutomationProperties.GetName(pElement);
        if (!string.IsNullOrWhiteSpace(pAutomationName))
        {
            return PInteractionTextNormalize(pAutomationName);
        }

        string? pContentText = PInteractionContentRead(pContent);
        if (!string.IsNullOrWhiteSpace(pContentText))
        {
            return PInteractionTextNormalize(pContentText);
        }

        if (pElement.ToolTip is string pTooltip && !string.IsNullOrWhiteSpace(pTooltip))
        {
            return PInteractionTextNormalize(pTooltip);
        }

        return pElement.Name;
    }

    private static string? PInteractionContentRead(object? pContent)
    {
        switch (pContent)
        {
            case string pText:
                return pText;
            case TextBlock pTextBlock:
                return pTextBlock.Text;
            case AccessText pAccessText:
                return pAccessText.Text.Replace("_", string.Empty, StringComparison.Ordinal);
            case ContentControl pContentControl:
                return PInteractionContentRead(pContentControl.Content);
            case Decorator pDecorator:
                return PInteractionContentRead(pDecorator.Child);
            case Panel pPanel:
                foreach (UIElement pChild in pPanel.Children)
                {
                    string? pChildText = PInteractionContentRead(pChild);
                    if (!string.IsNullOrWhiteSpace(pChildText))
                    {
                        return pChildText;
                    }
                }

                return null;
            default:
                return null;
        }
    }

    private static string PInteractionDetailRead(
        FrameworkElement pElement,
        string pControlType,
        string pLabel)
    {
        var pLines = new List<string>();
        string pResolved = PNameplate.PNameplateResolve(pElement);
        pLines.Add($"Control: {(string.IsNullOrWhiteSpace(pLabel) ? pResolved : pLabel)}");
        pLines.Add($"Type: {pControlType}");

        const string pOwnerSeparator = " › ";
        int pOwnerEnd = pResolved.LastIndexOf(pOwnerSeparator, StringComparison.Ordinal);
        if (pOwnerEnd > 0)
        {
            pLines.Add($"Owner: {pResolved[..pOwnerEnd]}");
        }

        if (pElement is ToggleButton pToggle)
        {
            pLines.Add($"State: {pToggle.IsChecked?.ToString() ?? "indeterminate"}");
        }

        return string.Join('\n', pLines);
    }

    private static string PInteractionTextNormalize(string pText) =>
        pText.Trim().TrimEnd('.', '!', '?').TrimEnd();

    private static string PInteractionSummaryRead(string pControlType, string pLabel) =>
        string.IsNullOrWhiteSpace(pLabel)
            ? $"{pControlType} activated"
            : $"{pControlType} activated: {pLabel}";
}
