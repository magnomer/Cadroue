using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.Application;
using Microsoft.Win32;

namespace Cadroue.UIDeportment;

public sealed class QExportCommands(
    QExport export,
    Func<Window> encoderCreate,
    Action<Window, QSVerdict> verdict,
    Action<Window?, string, string> warning,
    Brush iconBrush)
{
    public (
        RoutedEventHandler,
        RoutedEventHandler,
        RoutedEventHandler,
        RoutedEventHandler,
        RoutedEventHandler) QExportCommandsAttach() =>
        (
            (sender, _) => export.QExportFailureRaise(
                () => QLocalizationCatalog.QLocalizationTextRead("ExportPreset.DefaultName"),
                name => warning(
                    Window.GetWindow((DependencyObject)sender),
                    QLocalizationCatalog.QLocalizationTextRead("ExportPreset.Dialog.Export"),
                    QLocalizationCatalog.QLocalizationFormat("ExportPreset.Error.Write", name))),
            (sender, _) => export.QExportFailureRaise(
                name => warning(
                    Window.GetWindow((DependencyObject)sender),
                    QLocalizationCatalog.QLocalizationTextRead("ExportPreset.Dialog.Export"),
                    QLocalizationCatalog.QLocalizationFormat("ExportPreset.Error.Write", name))),
            (sender, pEvent) =>
            {
                var button = (Button)sender;
                Window encoderSurface = encoderCreate();
                encoderSurface.Owner = Window.GetWindow(button);
                QSEncoder encoder = export.QExportEncoderCreate();
                _ = new QSEncoderWindow(
                    encoderSurface,
                    encoder,
                    encoder.QSEncoderSave,
                    result => verdict(encoderSurface, result));
                encoderSurface.ShowDialog();
            },
            (sender, pEvent) =>
            {
                pEvent.Handled = true;
                export.QExportFailureRaise(
                    export.QExportSelected ?? string.Empty,
                    failureName => warning(
                        Window.GetWindow((DependencyObject)sender),
                        QLocalizationCatalog.QLocalizationTextRead("ExportPreset.Dialog.Export"),
                        QLocalizationCatalog.QLocalizationFormat("ExportPreset.Error.Write", failureName)));
            },
            (_, pEvent) =>
            {
                pEvent.Handled = true;
                export.QExportFailureRaise();
            });

    public RoutedEventHandler QExportCommandsAttach(RoutedEventHandler? outbound) =>
        outbound ?? ((sender, _) => export.QExportTransfer.QExportTransferSave(
            name =>
            {
                string file = string.IsNullOrWhiteSpace(name)
                    ? QLocalizationCatalog.QLocalizationTextRead("ExportPreset.Dialog.DefaultFile")
                    : $"{name}.json";
                var dialog = new SaveFileDialog();
                dialog.Title = QLocalizationCatalog.QLocalizationTextRead("ExportPreset.Dialog.Export");
                dialog.Filter = QLocalizationCatalog.QLocalizationTextRead("ExportPreset.Dialog.Filter");
                dialog.DefaultExt = "json";
                dialog.AddExtension = true;
                dialog.FileName = file;
                return (dialog.ShowDialog(), dialog.FileName);
            },
            name => warning(
                Window.GetWindow((DependencyObject)sender),
                QLocalizationCatalog.QLocalizationTextRead("ExportPreset.Dialog.Export"),
                QLocalizationCatalog.QLocalizationFormat("ExportPreset.Error.Write", name)),
            (_, _) => warning(
                Window.GetWindow((DependencyObject)sender),
                QLocalizationCatalog.QLocalizationTextRead("ExportPreset.Dialog.Export"),
                QLocalizationCatalog.QLocalizationTextRead("ExportPreset.Error.Catalogue"))));

    public RoutedEventHandler QExportCommandsAttach(RoutedEventHandler? inbound, string fallback) =>
        inbound ?? ((sender, _) =>
        {
            var dialog = new OpenFileDialog();
            dialog.Title = QLocalizationCatalog.QLocalizationTextRead("ExportPreset.Dialog.Import");
            dialog.Filter = QLocalizationCatalog.QLocalizationTextRead("ExportPreset.Dialog.Filter");
            dialog.DefaultExt = "json";
            dialog.CheckFileExists = true;
            export.QExportTransfer.QExportTransferImport(
                dialog.ShowDialog(),
                dialog.FileName,
                fallback,
                name => warning(
                    Window.GetWindow((DependencyObject)sender),
                    QLocalizationCatalog.QLocalizationTextRead("ExportPreset.Dialog.Export"),
                    QLocalizationCatalog.QLocalizationFormat("ExportPreset.Error.Write", name)),
                (_, detail) => warning(
                    Window.GetWindow((DependencyObject)sender),
                    QLocalizationCatalog.QLocalizationTextRead("ExportPreset.Dialog.Import"),
                    detail is null
                        ? QLocalizationCatalog.QLocalizationTextRead("ExportPreset.Error.Invalid")
                        : QLocalizationCatalog.QLocalizationFormat("ExportPreset.Error.Read", detail)));
        });

    public void QExportCommandsAttach(
        UserControl surface,
        TextBlock title,
        ContentControl body,
        ContentControl commandHost,
        Border innerBorder,
        ResourceDictionary commandSurface,
        Func<UIElement> presetBuild,
        (
            RoutedEventHandler,
            RoutedEventHandler,
            RoutedEventHandler,
            RoutedEventHandler,
            RoutedEventHandler) defaults,
        RoutedEventHandler save,
        RoutedEventHandler load,
        Action presetAttach,
        Action presetDetach,
        Action update,
        RoutedEventHandler? creation,
        RoutedEventHandler? deletion,
        RoutedEventHandler? configuration)
    {
        title.Text = QLocalizationCatalog.QLocalizationTextRead("ExportSummary.Header.Export");
        QScrollbar.QScrollbarAttach(surface);
        body.Content = presetBuild();

        var commands = (Grid)((ControlTemplate)commandSurface["PExportCommands"]).LoadContent();
        commandHost.Content = commands;
        var left = (StackPanel)commands.Children[0];
        var right = (StackPanel)commands.Children[1];
        var creationButton = (Button)left.Children[0];
        var deletionButton = (Button)left.Children[1];
        var configurationButton = (Button)right.Children[0];
        var outboundButton = (Button)right.Children[1];
        var inboundButton = (Button)right.Children[2];

        Action<Button, string, string, RoutedEventHandler> attach = (button, icon, tooltip, click) =>
        {
            ((Image)button.Content).Source = QIcon.QIconRead(icon, iconBrush);
            button.ToolTip = QLocalizationCatalog.QLocalizationTextRead(tooltip);
            button.Click += click;
            QButton.QButtonAttach(button, QButtonLook.QButtonLookPanel);
        };
        attach(
            creationButton,
            "/PAsset/PPanel/PExportPlus.svg",
            "ExportPreset.AddTooltip",
            creation ?? defaults.Item1);
        attach(
            deletionButton,
            "/PAsset/PPanel/PExportMinus.svg",
            "ExportPreset.DeleteTooltip",
            deletion ?? defaults.Item2);
        attach(
            configurationButton,
            "/PAsset/PPanel/PExportSetting.svg",
            "ExportPreset.SettingsTooltip",
            configuration ?? defaults.Item3);
        attach(outboundButton, "/PAsset/PPanel/PExportExport.svg", "ExportPreset.ExportTooltip", save);
        attach(inboundButton, "/PAsset/PPanel/PExportImport.svg", "ExportPreset.ImportTooltip", load);

        innerBorder.SizeChanged += (_, _) => innerBorder.Clip = new RectangleGeometry(
            new Rect(0, 0, innerBorder.ActualWidth, innerBorder.ActualHeight),
            9,
            9);
        export.QExportPresetsChanged += () => surface.Dispatcher.Invoke(update);
        presetAttach();
        update();
        surface.Loaded += (_, _) =>
        {
            export.QExportAttach();
            surface.Dispatcher.Invoke(update);
        };
        surface.Unloaded += (_, _) =>
        {
            presetDetach();
            export.QExportDetach();
        };
    }
}
