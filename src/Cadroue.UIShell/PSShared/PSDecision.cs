using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Cadroue.UIShell.PSShared;

internal enum PSDecisionChoice
{
    PSDecisionPrimary,
    PSDecisionAlternate,
    PSDecisionDismiss
}

internal sealed class PSDecision : Window
{
    private const double PSDecisionWidth = 440;

    private PSDecisionChoice psDecisionChoice = PSDecisionChoice.PSDecisionDismiss;

    private PSDecision(Window? pOwner, string pTitle, string pMessage, string pPrimary, string? pAlternate, string pCancel)
    {
        Title = pTitle;
        Width = PSDecisionWidth;
        SizeToContent = SizeToContent.Height;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        PSDialog.PSDialogApply(this, PSDialogTheme.PSDialogThemeBlue);
        Window? pResolvedOwner = pOwner ?? System.Windows.Application.Current?.MainWindow;
        if (pResolvedOwner is not null && pResolvedOwner != this)
        {
            Owner = pResolvedOwner;
        }
        else
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
        PSDialog.PSDialogLocationAttach(this);
        Content = PSDecisionBuild(pTitle, pMessage, pPrimary, pAlternate, pCancel);
    }

    internal static bool PSDecisionConfirm(Window? pOwner, string pTitle, string pMessage, string pPrimary, string pCancel) =>
        new PSDecision(pOwner, pTitle, pMessage, pPrimary, null, pCancel).PSDecisionShow() == PSDecisionChoice.PSDecisionPrimary;

    internal static PSDecisionChoice PSDecisionSelect(Window? pOwner, string pTitle, string pMessage, string pPrimary, string pAlternate, string pCancel) =>
        new PSDecision(pOwner, pTitle, pMessage, pPrimary, pAlternate, pCancel).PSDecisionShow();

    private PSDecisionChoice PSDecisionShow()
    {
        ShowDialog();
        return psDecisionChoice;
    }

    private UIElement PSDecisionBuild(string pTitle, string pMessage, string pPrimary, string? pAlternate, string pCancel) =>
        PSDialog.PSDialogBuild(this, pTitle, PSDecisionBodyBuild(pMessage, pPrimary, pAlternate, pCancel), PSDialogTheme.PSDialogThemeBlue);

    private DockPanel PSDecisionBodyBuild(string pMessage, string pPrimary, string? pAlternate, string pCancel)
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

        Button pCancelButton = PSFooter.PSFooterButtonBuild(pCancel);
        pCancelButton.IsCancel = true;
        pCancelButton.Click += (_, _) => PSDecisionClose(PSDecisionChoice.PSDecisionDismiss);
        pFooter.Children.Add(pCancelButton);

        if (pAlternate is not null)
        {
            Button pAlternateButton = PSFooter.PSFooterButtonBuild(pAlternate);
            pAlternateButton.Click += (_, _) => PSDecisionClose(PSDecisionChoice.PSDecisionAlternate);
            pFooter.Children.Add(pAlternateButton);
        }

        Button pPrimaryButton = PSFooter.PSFooterButtonBuild(pPrimary);
        pPrimaryButton.IsDefault = true;
        pPrimaryButton.Foreground = new SolidColorBrush(Color.FromRgb(0x1F, 0x6F, 0xB4));
        pPrimaryButton.BorderBrush = new SolidColorBrush(Color.FromRgb(0xC2, 0xD8, 0xEF));
        pPrimaryButton.Click += (_, _) => PSDecisionClose(PSDecisionChoice.PSDecisionPrimary);
        pFooter.Children.Add(pPrimaryButton);

        DockPanel.SetDock(pFooter, Dock.Bottom);
        pBody.Children.Add(pFooter);

        pBody.Children.Add(PSDecisionContentBuild(pMessage));
        return pBody;
    }

    private static Grid PSDecisionContentBuild(string pMessage)
    {
        var pContent = new Grid();
        pContent.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pContent.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var pIcon = new Image
        {
            Source = PAssets.PIcon.PIconRead("/PAssets/PSShared/PSDecision.svg", new SolidColorBrush(Color.FromRgb(0x1F, 0x6F, 0xB4))),
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

    private void PSDecisionClose(PSDecisionChoice pChoice)
    {
        psDecisionChoice = pChoice;
        Close();
    }
}
