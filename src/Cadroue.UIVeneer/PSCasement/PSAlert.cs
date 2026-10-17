using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer;

internal sealed class PSAlert : Window
{
    private const double PSAlertWidth = 420;

    private PSAlert(Window? pOwner, string pTitle, string pQuestion, string pAction, string? pCancel)
    {
        PSAlertTitleSet(pTitle);
        PSAlertFrameApply();
        PSAlertOwnerApply(PSAlertOwnerResolve(pOwner));
        PSDialog.PSDialogLocationAttach(this);
        PSAlertBuild(pTitle, pQuestion, pAction, pCancel);
    }

    private void PSAlertTitleSet(string pTitle) => Title = pTitle;

    private void PSAlertFrameApply()
    {
        Width = PSAlertWidth;
        SizeToContent = SizeToContent.Height;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        PSDialog.PSDialogApply(this, PSDialogTheme.PSDialogThemeRed);
    }

    private static Window? PSAlertOwnerResolve(Window? pOwner) =>
        pOwner ?? System.Windows.Application.Current?.MainWindow;

    private void PSAlertOwnerApply(Window? pResolvedOwner)
    {
        if (pResolvedOwner is not null && pResolvedOwner != this)
        {
            Owner = pResolvedOwner;
        }
        else
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
    }

    internal static bool PSAlertConfirm(
        Window? pOwner,
        string pTitle,
        string pQuestion,
        string pAction,
        string? pCancel = null) =>
        new PSAlert(pOwner, pTitle, pQuestion, pAction, pCancel).ShowDialog() == true;

    internal static void PSAlertConfirm(Window? pOwner, QAsk lAsk, Action<bool> lAnswer) =>
        lAnswer(PSAlertConfirm(pOwner, lAsk.QAskTitle, lAsk.QAskQuestion, lAsk.QAskAction, lAsk.QAskDismiss));

    private void PSAlertBuild(string pTitle, string pQuestion, string pAction, string? pCancel) =>
        Content = PSDialog.PSDialogBuild(
            this,
            pTitle,
            PSAlertBodyBuild(pQuestion, pAction, pCancel),
            PSDialogTheme.PSDialogThemeRed);

    private DockPanel PSAlertBodyBuild(string pQuestion, string pAction, string? pCancel)
    {
        Button pCancelButton = PSAlertCancelBuild(pCancel);
        Button pConfirm = PSAlertConfirmBuild(pAction);
        return PSAlertPanelBuild(pQuestion, pCancelButton, pConfirm);
    }

    private Button PSAlertCancelBuild(string? pCancel)
    {
        Button pCancelButton = PSFooter.PSFooterButtonBuild(PSAlertCancelResolve(pCancel));
        PSAlertEscapeSet(pCancelButton);
        PSAlertCancelAttach(pCancelButton);
        return pCancelButton;
    }

    private static string PSAlertCancelResolve(string? pCancel) =>
        pCancel ?? QLocalizationCatalog.QLocalizationTextRead("Terms.Cancel");

    private static void PSAlertEscapeSet(Button pCancelButton) => pCancelButton.IsCancel = true;

    private void PSAlertCancelAttach(Button pCancelButton) => pCancelButton.Click += (_, _) => Close();

    private Button PSAlertConfirmBuild(string pAction)
    {
        Button pConfirm = PSFooter.PSFooterButtonBuild(pAction);
        PSAlertConfirmApply(pConfirm);
        PSAlertConfirmAttach(pConfirm);
        return pConfirm;
    }

    private static void PSAlertConfirmApply(Button pConfirm)
    {
        pConfirm.Foreground = new SolidColorBrush(Color.FromRgb(0xE8, 0x11, 0x23));
        pConfirm.BorderBrush = new SolidColorBrush(Color.FromRgb(0xE8, 0x11, 0x23));
    }

    private void PSAlertConfirmAttach(Button pConfirm) => pConfirm.Click += PSAlertConfirmHandle;

    private void PSAlertConfirmHandle(object sender, RoutedEventArgs e) => DialogResult = true;

    private static DockPanel PSAlertPanelBuild(string pQuestion, Button pCancelButton, Button pConfirm)
    {
        var pBody = new DockPanel
        {
            Background = Brushes.White
        };

        var pFooter = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(12)
        };

        pFooter.Children.Add(pCancelButton);
        pFooter.Children.Add(pConfirm);
        DockPanel.SetDock(pFooter, Dock.Bottom);
        pBody.Children.Add(pFooter);

        pBody.Children.Add(PSAlertContentBuild(pQuestion));
        return pBody;
    }

    private static Grid PSAlertContentBuild(string pMessage)
    {
        var pContent = new Grid();
        pContent.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pContent.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var pIcon = new Image
        {
            Source = QIcon.QIconRead(
                "/PAsset/PSDialog/PSAlert.svg",
                new SolidColorBrush(Color.FromRgb(0xB4, 0x23, 0x18))),
            Width = 28,
            Height = 28,
            Stretch = Stretch.Uniform,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(24, 24, 0, 12)
        };
        Grid.SetColumn(pIcon, 0);
        pContent.Children.Add(pIcon);

        var pMessageText = new TextBlock
        {
            Text = pMessage,
            FontSize = 14,
            Foreground = PSField.PSFieldText,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(16, 24, 24, 12)
        };
        Grid.SetColumn(pMessageText, 1);
        pContent.Children.Add(pMessageText);
        return pContent;
    }
}
