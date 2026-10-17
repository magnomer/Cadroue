using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Cadroue.UIVeneer.PWing;

internal sealed partial class PSEncoder
{

    private UIElement PSEncoderRootBuild(UIElement pTabContent)
    {
        var pApply = PSFooter.PSFooterButtonBuild(QLocalizationCatalog.QLocalizationTextRead("Encoder.Button.Apply"));
        var pOk = PSFooter.PSFooterButtonBuild(QLocalizationCatalog.QLocalizationTextRead("Encoder.Button.OK"));
        var pCancel = PSFooter.PSFooterButtonBuild(QLocalizationCatalog.QLocalizationTextRead("Encoder.Button.Cancel"));
        PSEncoderFooterAttach(pApply, pOk, pCancel);
        return PSEncoderLayoutBuild(pTabContent, pApply, pOk, pCancel);
    }

    private void PSEncoderFooterAttach(Button pApply, Button pOk, Button pCancel)
    {
        pApply.Click += (_, _) => PSEncoderApply();
        pOk.Click += (_, _) => PSEncoderOkHandle();
        pCancel.Click += (_, _) => Close();
    }

    private void PSEncoderOkHandle()
    {
        PSEncoderApply();
        DialogResult = true;
    }

    private UIElement PSEncoderLayoutBuild(UIElement pTabContent, Button pApply, Button pOk, Button pCancel)
    {
        var pRoot = new DockPanel { Background = Brushes.White };
        var pFooter = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(12)
        };
        pFooter.Children.Add(pApply);
        pFooter.Children.Add(pOk);
        pFooter.Children.Add(pCancel);
        DockPanel.SetDock(pFooter, Dock.Bottom);
        pRoot.Children.Add(pFooter);
        pRoot.Children.Add(PSEncoderContentBuild(pTabContent));
        return pRoot;
    }

    private UIElement PSEncoderContentBuild(UIElement pTabContent)
    {
        var pPanel = new DockPanel { Margin = new Thickness(18) };
        pPanel.Children.Add(pTabContent);
        return pPanel;
    }

    private UIElement PSOutputBuild()
    {
        var pPanel = new StackPanel();
        pPanel.Children.Add(PSOutputPlateBuild());
        return pPanel;
    }

    private UIElement PSVideoBuild()
    {
        var pPanel = new StackPanel();
        pPanel.Children.Add(PSVideoPlateBuild());
        return pPanel;
    }

    private UIElement PSAudioBuild()
    {
        var pPanel = new StackPanel();
        pPanel.Children.Add(PSAudioPlateBuild());
        return pPanel;
    }

}
