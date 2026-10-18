using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed class PRailName
{
    private static readonly IReadOnlyDictionary<Key, bool> pRailEnterKeys = new Dictionary<Key, bool>
    {
        [Key.Enter] = true,
    };

    private static readonly IReadOnlyDictionary<Key, bool> pRailEscapeKeys = new Dictionary<Key, bool>
    {
        [Key.Escape] = true,
    };

    private static readonly IReadOnlyDictionary<bool, Action<TextBox>> pRailVisibles =
        new Dictionary<bool, Action<TextBox>>
        {
            [true] = PTabNamePrepare,
            [false] = pNameBox => { },
        };

    private readonly QRail lRail;

    internal PRailName(QRail lRailOwner) => lRail = lRailOwner;

    internal static void PTabLoadHandle(object sender)
    {
        var pNameBox = (TextBox)sender;
        PTabAutomationSet(pNameBox);
        PTabVisibleAttach(pNameBox);
    }

    private static void PTabAutomationSet(TextBox pNameBox) =>
        System.Windows.Automation.AutomationProperties.SetName(
            pNameBox, QLocalizationCatalog.QLocalizationTextRead("Tab.Rename.Name"));

    private static void PTabVisibleAttach(TextBox pNameBox) => pNameBox.IsVisibleChanged += PTabVisibleHandle;

    internal void PTabOutsideHandle(object sender, MouseButtonEventArgs e)
    {
        var pNameBox = Keyboard.FocusedElement as TextBox;
        lRail.QRailOutsideHandle(
            QSender.QSenderItemRead<QStripTab>(pNameBox),
            QWalk.QWalkParentCheck(e.OriginalSource as DependencyObject, pNode => ReferenceEquals(pNode, pNameBox)),
            pNameBox?.Text);
    }

    private static void PTabVisibleHandle(object sender, DependencyPropertyChangedEventArgs e)
    {
        var pNameBox = (TextBox)sender;
        pRailVisibles[pNameBox.IsVisible](pNameBox);
    }

    private static void PTabNamePrepare(TextBox pNameBox) =>
        pNameBox.Dispatcher.BeginInvoke(
            DispatcherPriority.Input,
            new Action(() =>
            {
                PTabNameShow(pNameBox);
                PTabNameSelect(pNameBox);
            }));

    private static void PTabNameShow(TextBox pNameBox) =>
        pNameBox.Text = QSender.QSenderItemRead<QStripTab>(pNameBox)!.QStripTabTitle;

    private static void PTabNameSelect(TextBox pNameBox)
    {
        pNameBox.SelectAll();
        pNameBox.Focus();
    }

    internal void PTabKeyHandle(object sender, KeyEventArgs e)
    {
        var pNameBox = (TextBox)sender;
        e.Handled = lRail.QRailKeyHandle(
            QSender.QSenderItemRead<QStripTab>(sender),
            pRailEnterKeys.GetValueOrDefault(e.Key),
            pRailEscapeKeys.GetValueOrDefault(e.Key),
            pNameBox.Text);
    }

    internal void PTabLeaveHandle(object sender) =>
        lRail.QRailLeaveHandle(QSender.QSenderItemRead<QStripTab>(sender), ((TextBox)sender).Text);
}
