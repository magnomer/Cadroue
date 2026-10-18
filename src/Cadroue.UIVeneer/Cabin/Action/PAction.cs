using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

public sealed class PAction : UserControl
{
    private static readonly Brush pActionPositiveBrush = new SolidColorBrush(Color.FromRgb(0x2F, 0x9E, 0x64));
    private static readonly Brush pActionNegativeBrush = new SolidColorBrush(Color.FromRgb(0xD6, 0x45, 0x45));

    private static readonly IReadOnlyDictionary<string, Brush> pActionAccents = new Dictionary<string, Brush>
    {
        ["AddList"] = pActionPositiveBrush,
        ["AddAll"] = pActionPositiveBrush,
        ["Execute"] = pActionNegativeBrush,
    };

    private readonly Button pActionAllButton;
    private readonly PActionRelay pActionRelay;

    public PAction(QAction lAction)
    {
        QAction = lAction;
        Button pAddListButton = PActionButtonBuild("AddList", "PActionAddList.svg", "Action.AddList");
        pActionAllButton = PActionButtonBuild("AddAll", "PActionAddAll.svg", "Action.AddAll");
        Button pExecuteButton = PActionButtonBuild("Execute", "PActionExecute.svg", "Action.Execute");
        PActionButtonsAttach(pAddListButton, pExecuteButton);
        PActionTipSet();
        pActionRelay = new PActionRelay(lAction);
        Content = PActionPanelBuild(pAddListButton, pExecuteButton);
        PActionEventsAttach();
        pActionRelay.PActionFaceUpdate();
    }

    public QAction QAction { get; }

    public void PActionRelayHide() => pActionRelay.PActionRelayHide();

    public void PActionAutoApply(bool lActionAutoRelay) => pActionRelay.PActionAutoApply(lActionAutoRelay);

    public void PActionAllSet(bool pActionAllAllowed, string pActionAllTooltip)
    {
        pActionAllButton.IsEnabled = pActionAllAllowed;
        pActionAllButton.Opacity = PLook.PLookOpacity[pActionAllAllowed];
        pActionAllButton.ToolTip = pActionAllTooltip;
    }

    private void PActionButtonsAttach(Button pAddListButton, Button pExecuteButton)
    {
        pAddListButton.Click += (_, _) => QAction.QActionListRun();
        pActionAllButton.Click += (_, _) => QAction.QActionAllRun();
        pExecuteButton.Click += (_, _) => QAction.QActionHighRun();
    }

    private void PActionTipSet() =>
        pActionAllButton.ToolTip = QLocalizationCatalog.QLocalizationTextRead("Action.AddAll.Tooltip");

    private Border PActionPanelBuild(Button pAddListButton, Button pExecuteButton)
    {
        var pPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        pPanel.Children.Add(pAddListButton);
        pPanel.Children.Add(new Border { Width = 2 });
        pPanel.Children.Add(pActionAllButton);
        pPanel.Children.Add(new Border { Width = 2 });
        pPanel.Children.Add(pExecuteButton);
        pPanel.Children.Add(new Border { Width = 12 });
        pPanel.Children.Add(pActionRelay.PActionRelayButton);
        pPanel.Children.Add(new Border { Width = 6 });
        pPanel.Children.Add(pActionRelay.PActionAutoBox);
        return new Border { Child = pPanel };
    }

    private void PActionEventsAttach()
    {
        QAction.QActionFaceChanged += pActionRelay.PActionFaceUpdate;
        QAction.QActionEmptyRaised += PActionEmptyShow;
    }

    private static void PActionEmptyShow() => PSWarning.PSWarningShow(
        null,
        QLocalizationCatalog.QLocalizationTextRead("Action.Empty.Title"),
        QLocalizationCatalog.QLocalizationTextRead("Action.Empty.Body"));

    private static Button PActionButtonBuild(string pActionToken, string pIconAssetName, string pLabelKey)
    {
        string pLabelText = QLocalizationCatalog.QLocalizationTextRead(pLabelKey);
        var pStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        pStack.Children.Add(new Image
        {
            Source = QIcon.QIconRead(
                $"/PAsset/PCompass/{pIconAssetName}",
                pActionAccents.GetValueOrDefault(pActionToken)),
            Width = 24,
            Height = 24,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center
        });
        pStack.Children.Add(new Border { Height = 1 });
        pStack.Children.Add(new TextBlock
        {
            Text = pLabelText,
            FontSize = 11,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            Foreground = new SolidColorBrush(Color.FromRgb(0x0D, 0x1B, 0x2F))
        });

        return QButton.QButtonBuild(
            new Button
            {
                Width = 58,
                Height = 58,
                Content = pStack,
                ToolTip = pLabelText
            },
            QButtonLook.QButtonLookCommand);
    }
}
