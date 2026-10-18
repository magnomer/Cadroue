using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.Application;

namespace Cadroue.UIVeneer;

internal static class PSMode
{
    private readonly record struct PSModeSegment(
        string PSModeSegmentToken,
        Border PSModeSegmentBorder,
        TextBlock PSModeSegmentText);

    internal static string PSModeTextRead(Border pMode) => (string)(pMode.Tag ?? string.Empty);

    internal static Border PSModeBuild(string pSelected, Action pChange, params LLocalizationChoice[] pChoices) =>
        PSModeBuild(pSelected, pChange, out _, pChoices);

    internal static Border PSModeBuild(
        string pSelected,
        Action pChange,
        out Action<string, bool> pEnableSet,
        params LLocalizationChoice[] pChoices)
    {
        var pDisabled = new HashSet<string>(StringComparer.Ordinal);
        StackPanel pStrip = PSModeStripBuild();
        Border pHost = PSModeHostBuild(pStrip);
        PSModeTokenSet(pHost, PSModeSelectedResolve(pSelected, pChoices));
        List<PSModeSegment> pSegments = PSModeSegmentsBuild(pStrip, pChoices);
        PSModeSegmentsAttach(pHost, pSegments, pDisabled, pChange);
        pEnableSet = (pToken, pEnabled) => PSModeEnableSet(pHost, pSegments, pDisabled, pToken, pEnabled);
        PSModeStyleApply(pHost, pSegments, pDisabled);
        return pHost;
    }

    private static StackPanel PSModeStripBuild() => new() { Orientation = Orientation.Horizontal };

    private static Border PSModeHostBuild(StackPanel pStrip) => new()
    {
        BorderBrush = PSField.PSFieldLine,
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(6),
        Background = Brushes.White,
        HorizontalAlignment = HorizontalAlignment.Left,
        Height = PSField.PSFieldControlHeight,
        Child = pStrip
    };

    private static string PSModeSelectedResolve(string pSelected, LLocalizationChoice[] pChoices) =>
        pChoices.Any(
            pChoice => string.Equals(pChoice.LLocalizationChoiceToken, pSelected, StringComparison.Ordinal))
                ? pSelected
                : pChoices.FirstOrDefault()?.LLocalizationChoiceToken ?? string.Empty;

    private static void PSModeTokenSet(Border pHost, string pToken) => pHost.Tag = pToken;

    private static void PSModeStyleApply(Border pHost, List<PSModeSegment> pSegments, HashSet<string> pDisabled)
    {
        for (int pStyleIndex = 0; pStyleIndex < pSegments.Count; pStyleIndex++)
        {
            (string pToken, Border pSegment, TextBlock pText) = pSegments[pStyleIndex];
            (bool pActive, bool pOff, bool pFirst, bool pLast) =
                PSModeStateResolve(pHost, pDisabled, pToken, pStyleIndex, pSegments.Count);
            pSegment.CornerRadius = new CornerRadius(pFirst ? 5 : 0, pLast ? 5 : 0, pLast ? 5 : 0, pFirst ? 5 : 0);
            pSegment.Background = pActive ? PSField.PSFieldAccent : PSField.PSFieldInactive;
            pSegment.IsHitTestVisible = !pOff;
            pSegment.Cursor = pOff ? Cursors.Arrow : Cursors.Hand;
            pSegment.Opacity = pOff ? 0.45 : 1;
            pText.Foreground = pActive ? Brushes.White : PSField.PSFieldText;
        }
    }

    private static (bool, bool, bool, bool) PSModeStateResolve(
        Border pHost,
        HashSet<string> pDisabled,
        string pToken,
        int pStyleIndex,
        int pCount)
    {
        bool pActive = string.Equals(pToken, (string)pHost.Tag, StringComparison.Ordinal);
        bool pOff = pDisabled.Contains(pToken);
        bool pFirst = pStyleIndex == 0;
        bool pLast = pStyleIndex == pCount - 1;
        return (pActive, pOff, pFirst, pLast);
    }

    private static List<PSModeSegment> PSModeSegmentsBuild(StackPanel pStrip, LLocalizationChoice[] pChoices)
    {
        var pSegments = new List<PSModeSegment>();
        for (int pIndex = 0; pIndex < pChoices.Length; pIndex++)
        {
            LLocalizationChoice pChoice = pChoices[pIndex];
            string pToken = pChoice.LLocalizationChoiceToken;
            var pText = new TextBlock
            {
                Text = pChoice.ToString(),
                FontSize = PSField.PSFieldFontSize,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            var pSegment = new Border
            {
                Child = pText,
                Padding = new Thickness(16, 0, 16, 0),
                Cursor = Cursors.Hand,
                BorderBrush = PSField.PSFieldLine,
                BorderThickness = new Thickness(pIndex == 0 ? 0 : 1, 0, 0, 0)
            };
            pSegments.Add(new PSModeSegment(pToken, pSegment, pText));
            pStrip.Children.Add(pSegment);
        }

        return pSegments;
    }

    private static void PSModeSegmentsAttach(
        Border pHost,
        List<PSModeSegment> pSegments,
        HashSet<string> pDisabled,
        Action pChange)
    {
        foreach (PSModeSegment pEntry in pSegments)
        {
            string pToken = pEntry.PSModeSegmentToken;
            pEntry.PSModeSegmentBorder.MouseLeftButtonUp += (_, _) =>
                PSModeSegmentHandle(pHost, pSegments, pDisabled, pToken, pChange);
        }
    }

    private static void PSModeSegmentHandle(
        Border pHost,
        List<PSModeSegment> pSegments,
        HashSet<string> pDisabled,
        string pToken,
        Action pChange)
    {
        if (string.Equals((string)pHost.Tag, pToken, StringComparison.Ordinal))
        {
            return;
        }

        PSModeTokenSet(pHost, pToken);
        PSModeStyleApply(pHost, pSegments, pDisabled);
        pChange();
    }

    private static void PSModeEnableSet(
        Border pHost,
        List<PSModeSegment> pSegments,
        HashSet<string> pDisabled,
        string pToken,
        bool pEnabled)
    {
        PSModeDisabledSet(pDisabled, pToken, pEnabled);
        if (!pEnabled && string.Equals((string)pHost.Tag, pToken, StringComparison.Ordinal))
        {
            PSModeTokenSet(pHost, PSModeFallbackResolve(pSegments, pDisabled, pToken));
        }

        PSModeStyleApply(pHost, pSegments, pDisabled);
    }

    private static void PSModeDisabledSet(HashSet<string> pDisabled, string pToken, bool pEnabled)
    {
        if (pEnabled)
        {
            pDisabled.Remove(pToken);
        }
        else
        {
            pDisabled.Add(pToken);
        }
    }

    private static string PSModeFallbackResolve(
        List<PSModeSegment> pSegments,
        HashSet<string> pDisabled,
        string pToken) =>
        pSegments
            .Select(pEntry => pEntry.PSModeSegmentToken)
            .FirstOrDefault(pOther => !pDisabled.Contains(pOther)) ?? pToken;
}
