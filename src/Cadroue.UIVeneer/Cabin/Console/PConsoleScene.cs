using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using Cadroue.UIDeportment;
using Microsoft.Win32;

namespace Cadroue.UIVeneer;

public sealed class PConsoleScene : UserControl
{
    private const string PConsoleEditablePart = "PART_EditableTextBox";

    private static readonly IReadOnlyDictionary<bool, FontStyle> pConsoleMarkStyles =
        new Dictionary<bool, FontStyle>
        {
            [true] = FontStyles.Italic,
            [false] = FontStyles.Normal,
        };

    private readonly QConsoleScene lScene;
    private readonly ComboBox pConsoleRelayCombo;
    private readonly DispatcherTimer pConsoleSceneTimer = new() { Interval = TimeSpan.FromMilliseconds(500) };

    public PConsoleScene(QConsoleScene lSceneOwner)
    {
        lScene = lSceneOwner;
        pConsoleRelayCombo = PConsoleControl.PConsoleComboBuild();
        Content = PConsolePanelBuild();
        PConsoleEventsAttach();
        lScene.QConsoleSceneRebuild();
    }

    private StackPanel PConsolePanelBuild()
    {
        var pControls = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };
        pControls.Children.Add(pConsoleRelayCombo);
        pControls.Children.Add(PConsoleControl.PConsoleInlineBuild(
            "PConsoleSave.svg",
            QLocalizationCatalog.QLocalizationTextRead("Console.Scene.SaveTooltip"),
            PConsoleSaveHandle));
        pControls.Children.Add(PConsoleControl.PConsoleInlineBuild(
            "PExportExport.svg",
            QLocalizationCatalog.QLocalizationTextRead("Console.Scene.ExportTooltip"),
            PConsoleExportHandle));
        pControls.Children.Add(PConsoleControl.PConsoleInlineBuild(
            "PExportImport.svg",
            QLocalizationCatalog.QLocalizationTextRead("Console.Scene.ImportTooltip"),
            PConsoleImportHandle));
        return pControls;
    }

    private void PConsoleEventsAttach()
    {
        pConsoleRelayCombo.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(PConsoleDeleteHandle));
        pConsoleRelayCombo.SelectionChanged += PConsoleSelectHandle;
        pConsoleRelayCombo.DropDownOpened += PConsoleOpenHandle;
        pConsoleRelayCombo.DropDownClosed += PConsoleCloseHandle;
        pConsoleRelayCombo.MouseEnter += PConsoleDropHandle;
        pConsoleRelayCombo.ItemContainerGenerator.StatusChanged += PConsoleRowsHandle;
        pConsoleSceneTimer.Tick += PConsoleTickHandle;
        lScene.QConsoleNamesApplied += PConsoleListApply;
        lScene.QConsoleSceneApplied += PConsoleSceneApply;
        lScene.QConsoleMarkApplied += PConsoleMarkApply;
        lScene.QConsoleFocusCleared += PConsoleFocusClear;
        lScene.QConsoleNoticeShown += PConsoleNoticeShow;
        lScene.QConsoleFile.QConsoleWarningShown += PConsoleWarningShow;
        Loaded += PConsoleCaretAttach;
        Loaded += PConsoleLoadHandle;
        Unloaded += PConsoleUnloadHandle;
    }

    private void PConsoleLoadHandle(object pSender, RoutedEventArgs pArguments)
    {
        lScene.QConsoleSceneStart();
        pConsoleSceneTimer.Start();
    }

    private void PConsoleUnloadHandle(object pSender, RoutedEventArgs pArguments) => pConsoleSceneTimer.Stop();

    private void PConsoleTickHandle(object? pSender, EventArgs pArguments) =>
        lScene.QConsoleTickHandle(pConsoleRelayCombo.IsKeyboardFocusWithin);

    private void PConsoleDropHandle(object? pSender, EventArgs pArguments) => lScene.QConsoleMarkUpdate();

    private void PConsoleOpenHandle(object? pSender, EventArgs pArguments)
    {
        lScene.QConsoleReloadSet(null);
        PConsoleBoxRead()?.SetValue(TextBoxBase.IsReadOnlyProperty, true);
        lScene.QConsoleMarkUpdate();
    }

    private void PConsoleCloseHandle(object? pSender, EventArgs pArguments) => lScene.QConsoleCloseHandle();

    private void PConsoleRowsHandle(object? pSender, EventArgs pArguments) =>
        pConsoleRelayCombo.Items.Cast<object>()
            .Select(pConsoleRelayCombo.ItemContainerGenerator.ContainerFromItem)
            .OfType<ComboBoxItem>()
            .ToList()
            .ForEach(PConsoleRowAttach);

    private void PConsoleRowAttach(ComboBoxItem pRow)
    {
        pRow.PreviewMouseLeftButtonUp -= PConsoleRowHandle;
        pRow.PreviewMouseLeftButtonUp += PConsoleRowHandle;
    }

    private void PConsoleRowHandle(object pSender, MouseButtonEventArgs pArguments) =>
        lScene.QConsoleRowHandle(QSender.QSenderItemRead<string>(pSender));

    private void PConsoleDeleteHandle(object pSender, RoutedEventArgs pArguments) =>
        pArguments.Handled = lScene.QConsoleDeleteHandle(QSender.QSenderItemRead<string>(pArguments.OriginalSource));

    private void PConsoleSelectHandle(object pSender, SelectionChangedEventArgs pArguments) =>
        lScene.QConsoleSelectHandle(pConsoleRelayCombo.SelectedItem, pConsoleRelayCombo.IsDropDownOpen);

    private void PConsoleSaveHandle(object pSender, RoutedEventArgs pArguments) =>
        lScene.QConsoleSceneSave(pConsoleRelayCombo.Text);

    private void PConsoleExportHandle(object pSender, RoutedEventArgs pArguments)
    {
        SaveFileDialog pDialog = lScene.QConsoleFile.QConsoleExportBuild(pConsoleRelayCombo.Text);
        lScene.QConsoleFile.QConsoleExportCommit(pDialog.ShowDialog(), pDialog.FileName, pConsoleRelayCombo.Text);
    }

    private void PConsoleImportHandle(object pSender, RoutedEventArgs pArguments)
    {
        OpenFileDialog pDialog = lScene.QConsoleFile.QConsoleImportBuild();
        lScene.QConsoleFile.QConsoleImportCommit(pDialog.ShowDialog(), pDialog.FileName);
    }

    private void PConsoleListApply(IReadOnlyList<string> pNames) => pConsoleRelayCombo.ItemsSource = pNames;

    private void PConsoleSceneApply(string? pSelected, string pText, bool pDirty)
    {
        pConsoleRelayCombo.SelectedItem = pSelected;
        pConsoleRelayCombo.Text = pText;
        PConsoleMarkApply(pDirty);
    }

    private void PConsoleMarkApply(bool pDirty)
    {
        pConsoleRelayCombo.ApplyTemplate();
        PConsoleBoxRead()?.SetValue(FontStyleProperty, pConsoleMarkStyles[pDirty]);
    }

    private TextBox? PConsoleBoxRead() =>
        pConsoleRelayCombo.Template.FindName(PConsoleEditablePart, pConsoleRelayCombo) as TextBox;

    private void PConsoleLoadedDetach() => Loaded -= PConsoleCaretAttach;

    private void PConsoleCaretAttach(object pSender, RoutedEventArgs pArguments)
    {
        PConsoleLoadedDetach();
        pConsoleRelayCombo.ApplyTemplate();
        TextBox? pEditableBox = PConsoleBoxRead();
        pEditableBox?.SetValue(TextBoxBase.IsReadOnlyProperty, true);
        pEditableBox?.AddHandler(
            PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(PConsoleReadonlyClear));
        pEditableBox?.AddHandler(
            LostKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(PConsoleReadonlySet));
    }

    private void PConsoleReadonlyClear(object pSender, MouseButtonEventArgs pArguments) =>
        PConsoleBoxRead()?.SetValue(TextBoxBase.IsReadOnlyProperty, false);

    private void PConsoleReadonlySet(object pSender, KeyboardFocusChangedEventArgs pArguments) =>
        PConsoleBoxRead()?.SetValue(TextBoxBase.IsReadOnlyProperty, true);

    public void PConsolePressHandle(object pSender, MouseButtonEventArgs pArguments) =>
        lScene.QConsolePressHandle(
            pConsoleRelayCombo.IsDropDownOpen,
            pConsoleRelayCombo.IsKeyboardFocusWithin,
            QWalk.QWalkParentCheck(pArguments.OriginalSource as DependencyObject, pConsoleRelayCombo.Equals));

    public void PConsoleDeactivateHandle(object? pSender, EventArgs pArguments) =>
        lScene.QConsoleDeactivateHandle(pConsoleRelayCombo.IsKeyboardFocusWithin);

    private void PConsoleFocusClear()
    {
        pConsoleRelayCombo.IsDropDownOpen = false;
        Keyboard.ClearFocus();
    }

    private void PConsoleNoticeShow(string pTitle, string pMessage) =>
        PSAnnouncement.PSAnnouncementShow(Window.GetWindow(this), pTitle, pMessage);

    private void PConsoleWarningShow(string pTitle, string pMessage) =>
        PSWarning.PSWarningShow(Window.GetWindow(this), pTitle, pMessage);
}
