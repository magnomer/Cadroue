using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;

namespace Cadroue.UIDeportment;

internal static class QInteractionFact
{
    private sealed record QInteractionReader(
        Type QInteractionReaderType,
        Func<object, string?> QInteractionReaderSource);

    private static readonly IReadOnlyList<QInteractionReader> QInteractionFactReaders =
    [
        new(typeof(string), lContent => (string)lContent),
        new(typeof(TextBlock), lContent => ((TextBlock)lContent).Text),
        new(
            typeof(AccessText),
            lContent => ((AccessText)lContent).Text.Replace("_", string.Empty, StringComparison.Ordinal)),
        new(typeof(ContentControl), lContent => QInteractionFactScan(((ContentControl)lContent).Content)),
        new(typeof(Decorator), lContent => QInteractionFactScan(((Decorator)lContent).Child)),
        new(
            typeof(Panel),
            lContent => ((Panel)lContent).Children
                .OfType<UIElement>()
                .Select(QInteractionFactScan)
                .FirstOrDefault(lText => !string.IsNullOrWhiteSpace(lText))),
    ];

    internal static string QInteractionFactRead(FrameworkElement lElement, object? lContent) =>
        QInteraction.QInteractionLabelResolve(
            AutomationProperties.GetName(lElement),
            QInteractionFactScan(lContent),
            lElement.ToolTip as string,
            lElement.Name);

    private static string? QInteractionFactScan(object? lContent) =>
        QInteractionFactReaders
            .FirstOrDefault(lReader => lReader.QInteractionReaderType.IsInstanceOfType(lContent))
            ?.QInteractionReaderSource(lContent!);
}
