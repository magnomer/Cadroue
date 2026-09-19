using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PHouse;

internal static class PInteraction
{
    private sealed record PInteractionReader(Type PInteractionType, Func<object, string?> PInteractionSource);

    private static readonly IReadOnlyList<PInteractionReader> PInteractionReaders =
    [
        new(typeof(string), pContent => (string)pContent),
        new(typeof(TextBlock), pContent => ((TextBlock)pContent).Text),
        new(
            typeof(AccessText),
            pContent => ((AccessText)pContent).Text.Replace("_", string.Empty, StringComparison.Ordinal)),
        new(typeof(ContentControl), pContent => PInteractionContentRead(((ContentControl)pContent).Content)),
        new(typeof(Decorator), pContent => PInteractionContentRead(((Decorator)pContent).Child)),
        new(typeof(Panel), pContent => PInteractionPanelRead((Panel)pContent)),
    ];

    internal static void PInteractionButtonAttach(ButtonBase pButton) =>
        pButton.AddHandler(
            ButtonBase.ClickEvent,
            new RoutedEventHandler(PInteractionButtonHandle),
            handledEventsToo: true);

    internal static void PInteractionButtonHandle(object pSender, RoutedEventArgs pEvent) =>
        PInteractionRecord((ButtonBase)pSender, pEvent, ((ButtonBase)pSender).Content, pSender.GetType().Name);

    internal static void PInteractionMenuHandle(object pSender, RoutedEventArgs pEvent) =>
        PInteractionRecord((MenuItem)pSender, pEvent, ((MenuItem)pSender).Header, nameof(MenuItem));

    private static void PInteractionRecord(
        FrameworkElement pElement,
        RoutedEventArgs pEvent,
        object? pContent,
        string pType) =>
        LInteraction.LInteractionRecord(
            ReferenceEquals(pElement, pEvent.OriginalSource),
            pType,
            () => PInteractionLabelRead(pElement, pContent),
            () => PNameplate.PNameplateResolve(pElement),
            typeof(ToggleButton).IsInstanceOfType(pElement),
            (pElement as ToggleButton)?.IsChecked);

    private static string PInteractionLabelRead(FrameworkElement pElement, object? pContent) =>
        LInteraction.LInteractionLabelResolve(
            AutomationProperties.GetName(pElement),
            PInteractionContentRead(pContent),
            pElement.ToolTip as string,
            pElement.Name);

    private static string? PInteractionContentRead(object? pContent) =>
        PInteractionReaders
            .FirstOrDefault(pReader => pReader.PInteractionType.IsInstanceOfType(pContent))
            ?.PInteractionSource(pContent!);

    private static string? PInteractionPanelRead(Panel pPanel) =>
        pPanel.Children
            .OfType<UIElement>()
            .Select(PInteractionContentRead)
            .FirstOrDefault(pText => !string.IsNullOrWhiteSpace(pText));
}
