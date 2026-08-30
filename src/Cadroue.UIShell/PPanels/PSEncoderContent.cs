using Cadroue.UIShell.PMainWindow;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using static Cadroue.UIShell.PSShared.PSField;
using static Cadroue.UIShell.PSShared.PSFooter;

namespace Cadroue.UIShell.PPanels;

internal sealed partial class PSEncoder
{

    private UIElement PSEncoderRootBuild(UIElement pTabContent)
    {
        var pRoot = new DockPanel { Background = Brushes.White };
        var pFooter = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(12) };
        var pApply = PSFooterButtonBuild(LLocalization.LLocalizationTextRead("Encoder.Button.Apply"));
        var pOk = PSFooterButtonBuild(LLocalization.LLocalizationTextRead("Encoder.Button.OK"));
        var pCancel = PSFooterButtonBuild(LLocalization.LLocalizationTextRead("Encoder.Button.Cancel"));
        pApply.Click += (_, _) => PSEncoderApply();
        pOk.Click += (_, _) => { PSEncoderApply(); DialogResult = true; };
        pCancel.Click += (_, _) => Close();
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
