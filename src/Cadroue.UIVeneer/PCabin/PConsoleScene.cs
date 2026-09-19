using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using Cadroue.Application;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;
using Microsoft.Win32;

namespace Cadroue.UIVeneer.PCabin;

public sealed class PConsoleScene : UserControl
{
    private const string PConsoleEditablePart = "PART_EditableTextBox";

    private static readonly IReadOnlyDictionary<bool, FontStyle> pConsoleMarkStyles =
        new Dictionary<bool, FontStyle>
        {
            [true] = FontStyles.Italic,
            [false] = FontStyles.Normal,
        };

    private readonly LConsoleScene lScene;
    private readonly ComboBox pConsoleRelayCombo;
    private readonly DispatcherTimer pConsoleSceneTimer = new() { Interval = TimeSpan.FromMilliseconds(500) };

    public PConsoleScene(LConsoleScene lSceneOwner)
    {
        lScene = lSceneOwner;
        pConsoleRelayCombo = PConsoleControl.PConsoleComboBuild();
        var pControls = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };
        pControls.Children.Add(pConsoleRelayCombo);
        pControls.Children.Add(PConsoleControl.PConsoleInlineBuild(
            "PConsoleSave.svg",
            LLocalization.LLocalizationTextRead("Console.Scene.SaveTooltip"),
            PConsoleSaveHandle));
        pControls.Children.Add(PConsoleControl.PConsoleInlineBuild(
            "PExportExport.svg",
            LLocalization.LLocalizationTextRead("Console.Scene.ExportTooltip"),
            PConsoleExportHandle));
        pControls.Children.Add(PConsoleControl.PConsoleInlineBuild(
            "PExportImport.svg",
            LLocalization.LLocalizationTextRead("Console.Scene.ImportTooltip"),
            PConsoleImportHandle));
        Content = pControls;

        pConsoleRelayCombo.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(PConsoleDeleteHandle));
        pConsoleRelayCombo.SelectionChanged += PConsoleSelectHandle;
        pConsoleRelayCombo.DropDownOpened += PConsoleOpenHandle;
        pConsoleRelayCombo.DropDownClosed += PConsoleCloseHandle;
        pConsoleRelayCombo.MouseEnter += PConsoleDropHandle;
        pConsoleRelayCombo.ItemContainerGenerator.StatusChanged += PConsoleRowsHandle;
        pConsoleSceneTimer.Tick += PConsoleTickHandle;
        lScene.LConsoleNamesApply += PConsoleListApply;
        lScene.LConsoleSceneApply += PConsoleSceneApply;
        lScene.LConsoleMarkApply += PConsoleMarkApply;
        lScene.LConsoleFocusClear += PConsoleFocusClear;
        lScene.LConsoleNoticeShow += PConsoleNoticeShow;
        lScene.LConsoleWarningShow += PConsoleWarningShow;
        Loaded += PConsoleCaretAttach;
        Loaded += PConsoleLoadHandle;
        Unloaded += PConsoleUnloadHandle;
        lScene.LConsoleSceneRebuild();
    }

    private void PConsoleLoadHandle(object pSender, RoutedEventArgs pArguments)
    {
        lScene.LConsoleSceneStart();
        pConsoleSceneTimer.Start();
    }

    private void PConsoleUnloadHandle(object pSender, RoutedEventArgs pArguments) => pConsoleSceneTimer.Stop();

    private void PConsoleTickHandle(object? pSender, EventArgs pArguments) =>
        lScene.LConsoleTickHandle(pConsoleRelayCombo.IsKeyboardFocusWithin);

    private void PConsoleDropHandle(object? pSender, EventArgs pArguments) => lScene.LConsoleMarkUpdate();

    private void PConsoleOpenHandle(object? pSender, EventArgs pArguments)
    {
        lScene.LConsoleReloadSet(null);
        PConsoleBoxRead()?.SetValue(TextBoxBase.IsReadOnlyProperty, true);
        lScene.LConsoleMarkUpdate();
    }

    private void PConsoleCloseHandle(object? pSender, EventArgs pArguments) => lScene.LConsoleCloseHandle();

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
        lScene.LConsoleRowHandle(PSender.PSenderItemRead<string>(pSender));

    private void PConsoleDeleteHandle(object pSender, RoutedEventArgs pArguments) =>
        pArguments.Handled = lScene.LConsoleDeleteHandle(PSender.PSenderItemRead<string>(pArguments.OriginalSource));

    private void PConsoleSelectHandle(object pSender, SelectionChangedEventArgs pArguments) =>
        lScene.LConsoleSelectHandle(pConsoleRelayCombo.SelectedItem, pConsoleRelayCombo.IsDropDownOpen);

    private void PConsoleSaveHandle(object pSender, RoutedEventArgs pArguments) =>
        lScene.LConsoleSceneSave(pConsoleRelayCombo.Text);

    private void PConsoleExportHandle(object pSender, RoutedEventArgs pArguments)
    {
        var pDialog = new SaveFileDialog
        {
            Title = LLocalization.LLocalizationTextRead("Console.Scene.Dialog.Export"),
            Filter = LLocalization.LLocalizationTextRead("Console.Scene.Dialog.Filter"),
            DefaultExt = "json",
            AddExtension = true,
            FileName = lScene.LConsoleFileResolve(pConsoleRelayCombo.Text)
        };
        lScene.LConsoleExportCommit(pDialog.ShowDialog(), pDialog.FileName, pConsoleRelayCombo.Text);
    }

    private void PConsoleImportHandle(object pSender, RoutedEventArgs pArguments)
    {
        var pDialog = new OpenFileDialog
        {
            Title = LLocalization.LLocalizationTextRead("Console.Scene.Dialog.Import"),
            Filter = LLocalization.LLocalizationTextRead("Console.Scene.Dialog.Filter"),
            DefaultExt = "json",
            CheckFileExists = true
        };
        lScene.LConsoleImportCommit(pDialog.ShowDialog(), pDialog.FileName);
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

    private void PConsoleCaretAttach(object pSender, RoutedEventArgs pArguments)
    {
        Loaded -= PConsoleCaretAttach;
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
        lScene.LConsolePressHandle(
            pConsoleRelayCombo.IsDropDownOpen,
            pConsoleRelayCombo.IsKeyboardFocusWithin,
            PWalk.PWalkParentCheck(pArguments.OriginalSource as DependencyObject, pConsoleRelayCombo.Equals));

    public void PConsoleDeactivateHandle(object? pSender, EventArgs pArguments) =>
        lScene.LConsoleDeactivateHandle(pConsoleRelayCombo.IsKeyboardFocusWithin);

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
