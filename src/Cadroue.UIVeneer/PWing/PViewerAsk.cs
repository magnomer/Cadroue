using System;
using System.Collections.Generic;
using System.Windows;

using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PWing;

// The viewer's asks: the warning and decision windows, and the file dialog that locates a source.
public static class PViewerAsk
{
    private static readonly IReadOnlyDictionary<QViewerAskKind, Action<DependencyObject, QViewerAsk, Action<bool>>>
        pViewerAskKinds = new Dictionary<QViewerAskKind, Action<DependencyObject, QViewerAsk, Action<bool>>>
        {
            [QViewerAskKind.QViewerAskWarning] = (pViewer, lAsk, pAnswer) =>
            {
                PSWarning.PSWarningShow(Window.GetWindow(pViewer), lAsk.QViewerAskTitle, lAsk.QViewerAskMessage);
                pAnswer(false);
            },
            [QViewerAskKind.QViewerAskDecision] = (pViewer, lAsk, pAnswer) => pAnswer(PSDecision.PSDecisionConfirm(
                Window.GetWindow(pViewer),
                lAsk.QViewerAskTitle,
                lAsk.QViewerAskMessage,
                lAsk.QViewerAskPrimary,
                lAsk.QViewerAskDismiss)),
        };

    private static readonly IReadOnlyDictionary<bool, Func<Microsoft.Win32.OpenFileDialog, string?>> pViewerAskLocates =
        new Dictionary<bool, Func<Microsoft.Win32.OpenFileDialog, string?>>
        {
            [true] = pDialog => pDialog.FileName,
            [false] = _ => null,
        };

    internal static void PViewerAskShow(DependencyObject pViewer, QViewerAsk lAsk, Action<bool> lAnswer) =>
        pViewerAskKinds[lAsk.QViewerAskKind](pViewer, lAsk, lAnswer);

    internal static void PViewerAskOpen(QViewerAsk lAsk, Action<string?> lAnswer)
    {
        Microsoft.Win32.OpenFileDialog pDialog = PViewerAskCreate(lAsk);
        lAnswer(pViewerAskLocates[pDialog.ShowDialog().GetValueOrDefault()](pDialog));
    }

    private static Microsoft.Win32.OpenFileDialog PViewerAskCreate(QViewerAsk lAsk) => new()
    {
        Title = lAsk.QViewerAskTitle,
        FileName = lAsk.QViewerAskName,
        Filter = lAsk.QViewerAskFilter
    };
}
