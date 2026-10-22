using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Cadroue.UIDeportment;

public sealed class QExportRow(
    QExport lExport,
    StackPanel pPresetRowPanel,
    string lPresetName,
    QExportFace face,
    Action pExportDragClear,
    Action<Border?> pExportDragSet,
    Action<Point?> pExportOriginSet,
    RoutedEventHandler acceptance,
    RoutedEventHandler reversion,
    Action<string> pPresetClick)
{
    private UIElement?[] qExportRow = new UIElement?[6];

    public Border QExportRowBuild(ResourceDictionary surface)
    {
        var root = (Border)((ControlTemplate)surface["PExportRow"]).LoadContent();
        (TextBlock label, Border badge, StackPanel actions) = (
            (TextBlock)((StackPanel)((Grid)root.Child).Children[0]).Children[0],
            (Border)((StackPanel)((Grid)root.Child).Children[0]).Children[1],
            (StackPanel)((Grid)root.Child).Children[1]);
        (Button accept, Button revert) = (
            (Button)actions.Children[0],
            (Button)actions.Children[1]);
        TextBox? editor = null;

        string title = face.QExportFaceModified
            ? $"{face.QExportFaceName} (Modified)"
            : face.QExportFaceName;
        label.Text = face.QExportFaceSupported
            ? title
            : $"{title} {QLocalizationCatalog.QLocalizationTextRead("ExportPreset.Incompatible")}";
        label.FontStyle = face.QExportFaceModified ? FontStyles.Italic : FontStyles.Normal;
        badge.Visibility = face.QExportFaceNative ? Visibility.Visible : Visibility.Collapsed;
        ((TextBlock)badge.Child).Text = QLocalizationCatalog.QLocalizationTextRead("ExportPreset.Native");
        actions.Visibility = face.QExportFaceModified ? Visibility.Visible : Visibility.Collapsed;
        root.Background = face.QExportFaceSelected
            ? new SolidColorBrush(Color.FromRgb(0xEE, 0xF4, 0xFB))
            : Brushes.White;
        root.Opacity = face.QExportFaceSupported ? 1 : 0.42;
        root.ToolTip = face.QExportFaceSupported
            ? null
            : QLocalizationCatalog.QLocalizationTextRead(face.QExportFaceNotice);
        if (face.QExportFaceEditing)
        {
            editor = (TextBox)root.FindResource("PExportEditor");
            root.Child = editor;
        }

        Action<Button, string, Brush, string> attach = (button, icon, brush, tooltip) =>
        {
            ((Image)button.Content).Source = QIcon.QIconRead(icon, brush);
            button.ToolTip = QLocalizationCatalog.QLocalizationTextRead(tooltip);
            QButton.QButtonAttach(button, QButtonLook.QButtonLookPanel);
        };
        attach(
            accept,
            "/PAsset/PPanel/PExportCheck.svg",
            new SolidColorBrush(Color.FromRgb(0x2E, 0xA3, 0x66)),
            "ExportPreset.ApplyTooltip");
        attach(
            revert,
            "/PAsset/PPanel/PExportCancel.svg",
            new SolidColorBrush(Color.FromRgb(0xD1, 0x43, 0x43)),
            "ExportPreset.DiscardTooltip");

        root.AddHandler(Mouse.PreviewMouseDownEvent, new MouseButtonEventHandler(QExportDownHandle), true);
        var acceptImage = (Image)accept.Content;
        var revertImage = (Image)revert.Content;
        qExportRow = [root, accept, revert, acceptImage, revertImage, editor];
        acceptImage.AddHandler(Mouse.PreviewMouseDownEvent, new MouseButtonEventHandler(QExportDownHandle), true);
        revertImage.AddHandler(Mouse.PreviewMouseDownEvent, new MouseButtonEventHandler(QExportDownHandle), true);
        root.MouseLeftButtonUp += QExportClickHandle;
        accept.Click += acceptance;
        revert.Click += reversion;
        if (editor is not null)
        {
            editor.Text = lPresetName;
            lExport.QExportName.QExportNameAttach(editor, lPresetName);
        }

        return root;
    }

    public void QExportRowDetach()
    {
        if (qExportRow[0] is not Border root)
        {
            return;
        }

        root.RemoveHandler(Mouse.PreviewMouseDownEvent, new MouseButtonEventHandler(QExportDownHandle));
        ((Image)qExportRow[3]!).RemoveHandler(
            Mouse.PreviewMouseDownEvent,
            new MouseButtonEventHandler(QExportDownHandle));
        ((Image)qExportRow[4]!).RemoveHandler(
            Mouse.PreviewMouseDownEvent,
            new MouseButtonEventHandler(QExportDownHandle));
        root.MouseLeftButtonUp -= QExportClickHandle;
        ((Button)qExportRow[1]!).Click -= acceptance;
        ((Button)qExportRow[2]!).Click -= reversion;
        if (qExportRow[5] is TextBox editor)
        {
            lExport.QExportName.QExportNameDetach(editor);
        }
    }

    private void QExportDownHandle(object sender, MouseButtonEventArgs pEvent)
    {
        if (pEvent.ChangedButton != MouseButton.Left || face.QExportFaceEditing)
        {
            return;
        }

        if (sender is Image || QExportSourceCheck(pEvent.OriginalSource))
        {
            pExportOriginSet(null);
            lExport.QExportDrag.QExportDragClear();
            return;
        }

        if (!face.QExportFaceNative && pEvent.ClickCount >= 2)
        {
            pExportDragClear();
            pPresetRowPanel.ReleaseMouseCapture();
            lExport.QExportEditStart(lPresetName);
            pEvent.Handled = true;
            return;
        }

        if (face.QExportFaceNative)
        {
            pExportOriginSet(null);
            lExport.QExportDrag.QExportDragClear();
            return;
        }

        var root = (Border)qExportRow[0]!;
        Point grab = pEvent.GetPosition(root);
        lExport.QExportDrag.QExportDragStart(lPresetName, grab.X, grab.Y);
        pExportDragSet(root);
        pExportOriginSet(pEvent.GetPosition(pPresetRowPanel));
        pPresetRowPanel.CaptureMouse();
    }

    private static bool QExportSourceCheck(object source)
    {
        DependencyObject? element = source as DependencyObject;
        while (element is not null)
        {
            if (element is Button)
            {
                return true;
            }

            element = VisualTreeHelper.GetParent(element);
        }

        return false;
    }

    private void QExportClickHandle(object sender, MouseButtonEventArgs pEvent)
    {
        if (face.QExportFaceEditing)
        {
            return;
        }

        if (!lExport.QExportEditingCheck(lPresetName))
        {
            lExport.QExportName.QExportNameCommit();
            pPresetClick(lPresetName);
        }

        pEvent.Handled = true;
    }
}
