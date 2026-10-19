using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Cadroue.UIDeportment;

internal static class QLook
{
    internal const string QLookPopupPart = "PART_Popup";

    // Set on a surface its driver owns; inherited, it hands every element below a Loaded handler, so the
    // Loaded broadcast reaches each one and the class handler wires it by its style.
    private static readonly DependencyProperty QLookReachProperty = DependencyProperty.RegisterAttached(
        "QLookReach",
        typeof(bool),
        typeof(QLook),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits, QLookReachApply));

    private static readonly RoutedEventHandler QLookBroadcast = (_, _) => { };

    private static readonly Dictionary<Style, string> QLookStyle = [];

    // An element is wired once, by whichever path reaches it first; the entry holds the sheets its states follow.
    private static readonly ConditionalWeakTable<FrameworkElement, IReadOnlyList<string>> QLookWatch = new();

    // The style level a Kit owner last ran its work for; the work runs again only when the element is restyled.
    private static readonly ConditionalWeakTable<FrameworkElement, Style> QLookHeld = new();

    internal static void QLookStateAttach()
    {
        if (System.Windows.Application.Current is { } lProgram)
        {
            QLookStyleScan(
                lProgram.Resources, QLookSheet.QLookSheetState.Select(lRow => lRow.QLookSetterStyle).ToHashSet());
        }

        RoutedEventHandler lRouted = QLookStateApply;
        MouseButtonEventHandler lPreview = (lSender, lEvent) =>
        {
            if (lSender is not ButtonBase)
            {
                QLookStateApply(lSender, lEvent);
            }
        };

        EventManager.RegisterClassHandler(
            typeof(FrameworkElement), FrameworkElement.LoadedEvent, new RoutedEventHandler(QLookStateLoad), true);
        EventManager.RegisterClassHandler(
            typeof(FrameworkElement), UIElement.MouseEnterEvent, new MouseEventHandler(QLookStateApply));
        EventManager.RegisterClassHandler(
            typeof(FrameworkElement), UIElement.MouseLeaveEvent, new MouseEventHandler(QLookStateApply));
        EventManager.RegisterClassHandler(
            typeof(FrameworkElement), UIElement.PreviewMouseLeftButtonDownEvent, lPreview);
        EventManager.RegisterClassHandler(
            typeof(FrameworkElement), UIElement.PreviewMouseLeftButtonUpEvent, lPreview);
        EventManager.RegisterClassHandler(typeof(ToggleButton), ToggleButton.CheckedEvent, lRouted);
        EventManager.RegisterClassHandler(typeof(ToggleButton), ToggleButton.UncheckedEvent, lRouted);
        EventManager.RegisterClassHandler(typeof(ToggleButton), ToggleButton.IndeterminateEvent, lRouted);
        EventManager.RegisterClassHandler(
            typeof(Thumb), Thumb.DragStartedEvent, new DragStartedEventHandler(QLookStateApply));
        EventManager.RegisterClassHandler(
            typeof(Thumb), Thumb.DragCompletedEvent, new DragCompletedEventHandler(QLookStateApply));
        EventManager.RegisterClassHandler(typeof(ListBoxItem), ListBoxItem.SelectedEvent, lRouted);
        EventManager.RegisterClassHandler(typeof(ListBoxItem), ListBoxItem.UnselectedEvent, lRouted);
        EventManager.RegisterClassHandler(
            typeof(ButtonBase), UIElement.MouseDownEvent, new MouseButtonEventHandler(QLookStateApply), true);
        EventManager.RegisterClassHandler(
            typeof(ButtonBase), UIElement.MouseUpEvent, new MouseButtonEventHandler(QLookStateApply), true);
        EventManager.RegisterClassHandler(
            typeof(ButtonBase), UIElement.MouseMoveEvent, new MouseEventHandler(QLookStateApply), true);
        EventManager.RegisterClassHandler(
            typeof(ButtonBase), UIElement.LostMouseCaptureEvent, new MouseEventHandler(QLookStateApply), true);
        EventManager.RegisterClassHandler(
            typeof(ButtonBase), UIElement.KeyDownEvent, new KeyEventHandler(QLookStateApply), true);
        EventManager.RegisterClassHandler(
            typeof(ButtonBase), UIElement.KeyUpEvent, new KeyEventHandler(QLookStateApply), true);
    }

    internal static void QLookStyleAttach(FrameworkElement lSurface)
    {
        ArgumentNullException.ThrowIfNull(lSurface);

        QLookStyleScan(
            lSurface.Resources, QLookSheet.QLookSheetState.Select(lRow => lRow.QLookSetterStyle).ToHashSet());
        lSurface.SetValue(QLookReachProperty, true);
    }

    internal static void QLookAttach(FrameworkElement lElement, string lStyle)
    {
        ArgumentNullException.ThrowIfNull(lElement);
        ArgumentException.ThrowIfNullOrEmpty(lStyle);
        QLookHandlersAttach(lElement, [lStyle]);
        QLookStateApply(lElement, EventArgs.Empty);
    }

    // The entry for an element a surface Veneer still builds: it follows the sheets its style chain names.
    internal static void QLookAttach(FrameworkElement lElement)
    {
        ArgumentNullException.ThrowIfNull(lElement);

        List<string> lStyles = QLookSheetRead(lElement);
        if (lStyles.Count == 0)
        {
            return;
        }

        QLookHandlersAttach(lElement, lStyles);
        QLookStateApply(lElement, EventArgs.Empty);
    }

    // A Kit owner asks with its style keys; the most derived key in the element's style chain comes back the first
    // time for that style level, and null on every later ask, so each owner's work runs once per style.
    internal static string? QLookStyleResolve(FrameworkElement lElement, IReadOnlyList<string> lKeys)
    {
        ArgumentNullException.ThrowIfNull(lElement);
        ArgumentNullException.ThrowIfNull(lKeys);

        for (Style? lLevel = lElement.Style; lLevel is not null; lLevel = lLevel.BasedOn)
        {
            string? lKey = lKeys.FirstOrDefault(
                lCandidate => ReferenceEquals(lElement.TryFindResource(lCandidate), lLevel));
            if (lKey is null)
            {
                continue;
            }

            if (QLookHeld.TryGetValue(lElement, out Style? lHeld) && ReferenceEquals(lHeld, lLevel))
            {
                return null;
            }

            QLookHeld.AddOrUpdate(lElement, lLevel);
            return lKey;
        }

        return null;
    }

    private static void QLookStyleScan(ResourceDictionary lDictionary, HashSet<string> lKeys)
    {
        foreach (string lKey in lKeys)
        {
            if (lDictionary.Contains(lKey) && lDictionary[lKey] is Style lStyle)
            {
                QLookStyle[lStyle] = lKey;
            }
        }

        foreach (ResourceDictionary lMerged in lDictionary.MergedDictionaries)
        {
            QLookStyleScan(lMerged, lKeys);
        }
    }

    private static void QLookReachApply(DependencyObject lSender, DependencyPropertyChangedEventArgs lEvent)
    {
        if (lSender is not FrameworkElement lElement || lEvent.NewValue is not true)
        {
            return;
        }

        lElement.Loaded -= QLookBroadcast;
        lElement.Loaded += QLookBroadcast;
        if (lElement.IsLoaded)
        {
            QLookStateLoad(lElement, new RoutedEventArgs(FrameworkElement.LoadedEvent, lElement));
        }
    }

    // A reached element follows every sheet its style and the styles it is BasedOn name, base first.
    private static void QLookStateLoad(object lSender, RoutedEventArgs lEvent)
    {
        if (lSender is not FrameworkElement lElement || !(bool)lElement.GetValue(QLookReachProperty))
        {
            return;
        }

        List<string> lStyles = QLookSheetRead(lElement);
        if (lStyles.Count > 0)
        {
            QLookHandlersAttach(lElement, lStyles);
        }

        QLookStateApply(lElement, lEvent);
    }

    private static List<string> QLookSheetRead(FrameworkElement lElement)
    {
        List<string> lStyles = [];
        for (Style? lLevel = lElement.Style; lLevel is not null; lLevel = lLevel.BasedOn)
        {
            if (QLookStyle.TryGetValue(lLevel, out string? lKey))
            {
                lStyles.Insert(0, lKey);
            }
        }

        return lStyles;
    }

    // The class handlers carry the routed events; this hooks the events no class handler can reach.
    private static void QLookHandlersAttach(FrameworkElement lElement, IReadOnlyList<string> lStyles)
    {
        if (QLookWatch.TryGetValue(lElement, out _))
        {
            return;
        }

        QLookWatch.Add(lElement, lStyles);
        DependencyPropertyChangedEventHandler lChanged = (_, _) => QLookStateApply(lElement, EventArgs.Empty);
        lElement.IsKeyboardFocusWithinChanged += lChanged;
        lElement.IsEnabledChanged += lChanged;
        if (lElement is ComboBox lCombo)
        {
            lCombo.DropDownOpened += QLookStateApply;
            lCombo.DropDownClosed += QLookStateApply;
        }

        if (lElement is MenuItem)
        {
            QLookHighlightAttach(lElement);
        }

        _ = lElement.ApplyTemplate();
        QLookPopupAttach(lElement);
    }

    private static void QLookHighlightAttach(FrameworkElement lElement)
    {
        DependencyPropertyDescriptor lHighlight =
            DependencyPropertyDescriptor.FromProperty(MenuItem.IsHighlightedProperty, typeof(MenuItem));
        EventHandler lShifted = (_, _) => QLookStateApply(lElement, EventArgs.Empty);
        lElement.Loaded += (_, _) =>
        {
            lHighlight.RemoveValueChanged(lElement, lShifted);
            lHighlight.AddValueChanged(lElement, lShifted);
            QLookStateApply(lElement, EventArgs.Empty);
        };
        lElement.Unloaded += (_, _) => lHighlight.RemoveValueChanged(lElement, lShifted);
        if (lElement.IsLoaded)
        {
            lHighlight.AddValueChanged(lElement, lShifted);
        }
    }

    private static void QLookPopupAttach(FrameworkElement lElement)
    {
        if ((lElement as Control)?.Template?.FindName(QLookPopupPart, lElement) is Popup lPopup)
        {
            lPopup.Opened += (_, _) => QLookStateApply(lElement, EventArgs.Empty);
            lPopup.Closed += (_, _) => QLookStateApply(lElement, EventArgs.Empty);
        }
    }

    // A later sheet's matching row wins a slot; an earlier winner stands where the later sheet matches none.
    private static void QLookStateApply(object? lSender, EventArgs lEvent)
    {
        if (lSender is not FrameworkElement lElement
            || !QLookWatch.TryGetValue(lElement, out IReadOnlyList<string>? lStyles))
        {
            return;
        }

        QLookCue lActive = QLookFact.QLookFactRead(lElement);
        Dictionary<(string, DependencyProperty), QLookSetter?> lWinners = [];
        foreach (string lStyle in lStyles)
        {
            foreach (((string, DependencyProperty) lSlot, QLookSetter? lWinner) in
                QLookSheet.QLookSheetResolve(lStyle, lActive))
            {
                if (lWinner is not null || !lWinners.ContainsKey(lSlot))
                {
                    lWinners[lSlot] = lWinner;
                }
            }
        }

        foreach (((string lPart, DependencyProperty lProperty), QLookSetter? lWinner) in lWinners)
        {
            if ((lElement as Control)?.Template?.FindName(lPart, lElement) is not FrameworkElement lTarget)
            {
                continue;
            }

            if (lWinner is null)
            {
                lTarget.ClearValue(lProperty);
            }
            else
            {
                lTarget.SetResourceReference(lProperty, lWinner.QLookSetterKey);
            }
        }
    }
}
