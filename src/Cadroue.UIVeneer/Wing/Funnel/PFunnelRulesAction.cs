using Cadroue.UIDeportment;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Cadroue.UIVeneer;

internal sealed class PFunnelRulesAction
{
    private const string pFunnelAddIcon = "/PAsset/PPanel/PFunnelAdd.svg";
    private const string pFunnelRemoveIcon = "/PAsset/PPanel/PFunnelRemove.svg";
    private static readonly Brush pFunnelLineBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7));
    private static readonly Brush pFunnelIconBrush = new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x3D));

    private readonly QFunnel lFunnel;
    private readonly PFunnelRulesMenu pFunnelMenu;

    public PFunnelRulesAction(QFunnel lFunnelOwner)
    {
        lFunnel = lFunnelOwner;
        pFunnelMenu = new PFunnelRulesMenu(lFunnelOwner);
    }

    public Border PFunnelActionBuild()
    {
        Button pAddButton = PFunnelButtonBuild(pFunnelAddIcon, "Inspector.Funnel.Add");
        Button pRemoveButton = PFunnelButtonBuild(pFunnelRemoveIcon, "Inspector.Funnel.Remove");
        PFunnelActionAttach(pAddButton, pRemoveButton);
        return PFunnelBarBuild(pAddButton, pRemoveButton);
    }

    private void PFunnelActionAttach(Button pAddButton, Button pRemoveButton)
    {
        pAddButton.Click += (_, _) => pFunnelMenu.PFunnelMenuShow(pAddButton);
        pRemoveButton.Click += (_, _) => lFunnel.QFunnelSelectedRemove();
    }

    private static Border PFunnelBarBuild(Button pAddButton, Button pRemoveButton)
    {
        var pButtonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        pButtonPanel.Children.Add(pAddButton);
        pButtonPanel.Children.Add(pRemoveButton);

        return new Border
        {
            Padding = new Thickness(10, 4, 10, 6),
            BorderBrush = pFunnelLineBrush,
            BorderThickness = new Thickness(0, 1, 0, 0),
            Background = Brushes.White,
            Child = pButtonPanel
        };
    }

    private static Button PFunnelButtonBuild(string pIconPath, string pTooltipKey)
    {
        return QButton.QButtonBuild(
            new Button
            {
                Content = new Image
                {
                    Width = 20,
                    Height = 20,
                    Source = QIcon.QIconRead(pIconPath, pFunnelIconBrush),
                    Stretch = Stretch.Uniform
                },
                Width = 28,
                Height = 26,
                Margin = new Thickness(0, 0, 2, 0),
                ToolTip = QLocalizationCatalog.QLocalizationTextRead(pTooltipKey)
            },
            QButtonLook.QButtonLookPanel);
    }
}
