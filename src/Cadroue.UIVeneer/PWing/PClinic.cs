using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PWing;

public sealed class PClinic : UserControl
{
    private static readonly FontFamily pClinicFontFamily = new("Segoe UI");
    private static readonly Brush pClinicTitleBrush = new SolidColorBrush(Color.FromRgb(0x26, 0x36, 0x4A));
    private static readonly Brush pClinicMutedBrush = new SolidColorBrush(Color.FromRgb(0x8A, 0x93, 0x9E));
    private static readonly Brush pClinicIconBrush = new SolidColorBrush(Color.FromRgb(0x56, 0x62, 0x73));

    public const double PClinicStripWidth = 48;

    public event Action<bool>? PClinicMinimizeChanged;
    public event Action? PClinicDiagnosisExecuted;

    private UIElement pClinicFullBody = null!;
    private UIElement pClinicStripBody = null!;
    private TextBlock pClinicTitleLabel = null!;
    private TextBlock pClinicEmptyNotice = null!;
    private UIElement pClinicItemBody = null!;
    private StackPanel pClinicToggleRow = null!;
    private TextBlock pClinicItemSimple = null!;
    private TextBlock pClinicItemTechnical = null!;
    private UIElement pClinicResultBody = null!;
    private TextBlock pClinicResultText = null!;
    private ProgressBar pClinicDiagnosisProgress = null!;
    private CheckBox pClinicApplyBox = null!;
    private CheckBox pClinicPersistentBox = null!;
    private Button pClinicDiagnosisButton = null!;
    private readonly PClinicSalvage pClinicSalvage;

    public QClinic QClinic { get; }

    public PClinic(QClinic lClinic)
    {
        QPanel.QPanelAttach(this);
        QClinic = lClinic;
        pClinicSalvage = new PClinicSalvage(QClinic);
        PClinicFrameBuild();
        PClinicAttach();
        PClinicUpdate();
    }

    private void PClinicFrameBuild()
    {
        Border pHeader = PClinicHeaderBuild();
        ScrollViewer pScroll = PClinicBodyBuild();
        Border pPersistentRow = PClinicFooterBuild();

        var pRoot = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(pHeader, Dock.Top);
        pRoot.Children.Add(pHeader);
        DockPanel.SetDock(pPersistentRow, Dock.Bottom);
        pRoot.Children.Add(pPersistentRow);
        pRoot.Children.Add(pScroll);

        pClinicFullBody = pRoot;
        pClinicStripBody = PClinicStripBuild();
        pClinicStripBody.Visibility = Visibility.Collapsed;

        var pBodyHost = new Grid();
        pBodyHost.Children.Add(pClinicFullBody);
        pBodyHost.Children.Add(pClinicStripBody);

        FocusVisualStyle = null;
        Content = QPanel.QPanelBuild(pBodyHost);
    }

    private Border PClinicHeaderBuild()
    {
        pClinicTitleLabel = new TextBlock
        {
            Text = QLocalizationCatalog.QLocalizationTextRead("Clinic.Header.Title"),
            FontSize = 12,
            FontFamily = pClinicFontFamily,
            FontWeight = FontWeights.SemiBold,
            Foreground = pClinicTitleBrush,
            VerticalAlignment = VerticalAlignment.Center
        };

        Button pMinimizeButton = PClinicButtonBuild(
            "/PAsset/PPanel/PListMinimize.svg",
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Panel.HideTooltip"),
            () => QClinic.QClinicMinimizedSet(true));
        pMinimizeButton.HorizontalAlignment = HorizontalAlignment.Right;

        var pHeaderGrid = new Grid();
        pHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(pMinimizeButton, 1);
        pHeaderGrid.Children.Add(pClinicTitleLabel);
        pHeaderGrid.Children.Add(pMinimizeButton);

        return new Border
        {
            Padding = new Thickness(12, 5, 6, 5),
            BorderBrush = QPanel.QPanelLineRead(),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Background = Brushes.White,
            Child = pHeaderGrid
        };
    }

    private ScrollViewer PClinicBodyBuild()
    {
        pClinicApplyBox = PClinicSwitchBuild(
            QLocalizationCatalog.QLocalizationTextRead("Clinic.Apply"),
            QLocalizationCatalog.QLocalizationTextRead("Clinic.Apply.Tooltip"));

        pClinicToggleRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 4),
            IsEnabled = false
        };
        pClinicSalvage.PClinicSalvageActive.Visibility = Visibility.Collapsed;
        pClinicToggleRow.Children.Add(pClinicSalvage.PClinicSalvageActive);
        pClinicToggleRow.Children.Add(pClinicApplyBox);

        pClinicEmptyNotice = new TextBlock
        {
            Text = QLocalizationCatalog.QLocalizationTextRead("Clinic.Empty.Notice"),
            FontSize = 12,
            FontFamily = pClinicFontFamily,
            Foreground = pClinicMutedBrush,
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(16, 24, 16, 16)
        };

        pClinicItemBody = PClinicItemBuild();
        pClinicResultBody = PClinicResultBuild();

        var pBody = new StackPanel { Margin = new Thickness(12, 10, 12, 10) };
        pBody.Children.Add(pClinicToggleRow);
        pBody.Children.Add(PClinicSeparatorBuild());
        pBody.Children.Add(pClinicEmptyNotice);
        pBody.Children.Add(pClinicItemBody);
        pBody.Children.Add(pClinicResultBody);
        pBody.Children.Add(pClinicSalvage);

        return new ScrollViewer
        {
            Content = pBody,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
    }

    private UIElement PClinicItemBuild()
    {
        pClinicItemSimple = new TextBlock
        {
            FontSize = 12,
            FontFamily = pClinicFontFamily,
            Foreground = QPanel.QPanelTextRead(),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8)
        };
        pClinicItemTechnical = new TextBlock
        {
            FontSize = 11,
            FontFamily = pClinicFontFamily,
            Foreground = pClinicMutedBrush,
            TextWrapping = TextWrapping.Wrap
        };

        var pItemStack = new StackPanel();
        pItemStack.Children.Add(pClinicItemSimple);
        pItemStack.Children.Add(pClinicItemTechnical);
        pItemStack.Visibility = Visibility.Collapsed;
        return pItemStack;
    }

    private UIElement PClinicResultBuild()
    {
        var pResultHeader = new TextBlock
        {
            Text = QLocalizationCatalog.QLocalizationTextRead("Clinic.Result.Header"),
            FontSize = 12,
            FontFamily = pClinicFontFamily,
            FontWeight = FontWeights.SemiBold,
            Foreground = pClinicTitleBrush,
            Margin = new Thickness(0, 0, 0, 6)
        };
        pClinicResultText = new TextBlock
        {
            Text = QLocalizationCatalog.QLocalizationTextRead("Clinic.Result.Empty"),
            FontSize = 12,
            FontFamily = pClinicFontFamily,
            Foreground = pClinicMutedBrush,
            TextWrapping = TextWrapping.Wrap
        };

        var pResultStack = new StackPanel();
        pResultStack.Children.Add(PClinicSeparatorBuild());
        pResultStack.Children.Add(pResultHeader);
        pResultStack.Children.Add(pClinicResultText);
        pResultStack.Visibility = Visibility.Collapsed;
        return pResultStack;
    }

    private Border PClinicFooterBuild()
    {
        pClinicDiagnosisProgress = PSensorSweep.PSensorProgressBuild();
        pClinicPersistentBox = PClinicSwitchBuild(
            QLocalizationCatalog.QLocalizationTextRead("Clinic.Persistent"),
            QLocalizationCatalog.QLocalizationTextRead("Clinic.Persistent.Tooltip"));
        pClinicPersistentBox.IsEnabled = false;
        pClinicSalvage.PClinicSalvagePersistent.Visibility = Visibility.Collapsed;
        pClinicPersistentBox.VerticalAlignment = VerticalAlignment.Center;
        var pPersistentStack = new Grid { VerticalAlignment = VerticalAlignment.Center };
        pPersistentStack.Children.Add(pClinicPersistentBox);
        pPersistentStack.Children.Add(pClinicSalvage.PClinicSalvagePersistent);

        pClinicDiagnosisButton = new Button
        {
            Content = QLocalizationCatalog.QLocalizationTextRead("Clinic.Diagnosis.Run"),
            ToolTip = QLocalizationCatalog.QLocalizationTextRead("Clinic.Diagnosis.Run.Tooltip"),
            Height = 28,
            MinWidth = 90,
            FontSize = 12,
            FontFamily = pClinicFontFamily,
            VerticalAlignment = VerticalAlignment.Center
        };
        QButton.QButtonAttach(pClinicDiagnosisButton, QButtonLook.QButtonLookWhite);

        var pRunGrid = new Grid();
        pRunGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pRunGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(pClinicDiagnosisButton, 1);
        pRunGrid.Children.Add(pPersistentStack);
        pRunGrid.Children.Add(pClinicDiagnosisButton);

        var pRunStack = new StackPanel();
        pRunStack.Children.Add(pClinicDiagnosisProgress);
        pRunStack.Children.Add(pRunGrid);

        return new Border
        {
            Padding = new Thickness(12, 6, 12, 8),
            BorderBrush = QPanel.QPanelLineRead(),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Background = Brushes.White,
            Child = pRunStack
        };
    }

    private void PClinicAttach()
    {
        pClinicApplyBox.Checked += (_, _) => QClinic.QClinicActiveSet(true);
        pClinicApplyBox.Unchecked += (_, _) => QClinic.QClinicActiveSet(false);
        pClinicPersistentBox.Checked += (_, _) => QClinic.QClinicPersistentSet(true);
        pClinicPersistentBox.Unchecked += (_, _) => QClinic.QClinicPersistentSet(false);
        pClinicDiagnosisButton.Click += (_, _) => PClinicDiagnosisExecuted?.Invoke();
        QClinic.QClinicChanged += PClinicUpdate;
        QClinic.QClinicMinimizeChanged += PClinicMinimizeHandle;
    }

    public bool PClinicMinimizedCheck() => QClinic.QClinicMinimized;

    public void PClinicMinimizeSet(bool pClinicMinimizeRequest) => QClinic.QClinicMinimizedSet(pClinicMinimizeRequest);

    public void PClinicStepShow(string? pStepName) => QClinic.QClinicStepSet(pStepName);

    internal static CheckBox PClinicSwitchBuild(string pSwitchLabel, string pSwitchTip)
    {
        var pSwitch = new CheckBox
        {
            Content = pSwitchLabel,
            ToolTip = pSwitchTip,
            FontSize = 12,
            FontFamily = pClinicFontFamily,
            Foreground = QPanel.QPanelTextRead(),
            VerticalContentAlignment = VerticalAlignment.Center
        };
        QInteractionClick.QInteractionClickAttach(pSwitch);
        QCheckbox.QCheckboxAttach(pSwitch);
        return pSwitch;
    }

    private void PClinicMinimizeHandle(bool pClinicMinimized)
    {
        PClinicMinimizedApply(pClinicMinimized);
        PClinicMinimizeRaise(pClinicMinimized);
    }

    private void PClinicMinimizedApply(bool pClinicMinimized)
    {
        pClinicFullBody.Visibility = PLook.PLookVisible[!pClinicMinimized];
        pClinicStripBody.Visibility = PLook.PLookVisible[pClinicMinimized];
    }

    private void PClinicMinimizeRaise(bool pClinicMinimized) => PClinicMinimizeChanged?.Invoke(pClinicMinimized);

    private void PClinicUpdate()
    {
        pClinicSalvage.PClinicSalvageUpdate();
        pClinicApplyBox.Visibility = PLook.PLookVisible[!QClinic.QClinicSalvageShown];
        pClinicPersistentBox.Visibility = PLook.PLookVisible[!QClinic.QClinicSalvageShown];
        pClinicDiagnosisButton.Visibility = PLook.PLookVisible[!QClinic.QClinicSalvageShown];
        pClinicSalvage.PClinicSalvageActive.Visibility = PLook.PLookVisible[QClinic.QClinicSalvageShown];
        pClinicSalvage.PClinicSalvagePersistent.Visibility = PLook.PLookVisible[QClinic.QClinicSalvageShown];
        pClinicToggleRow.Visibility = Visibility.Visible;
        pClinicEmptyNotice.Visibility = PLook.PLookVisible[!QClinic.QClinicKnown];
        pClinicItemBody.Visibility = PLook.PLookVisible[QClinic.QClinicKnown];
        pClinicToggleRow.IsEnabled = QClinic.QClinicKnown;
        pClinicPersistentBox.IsEnabled = QClinic.QClinicPersistentAllowed;
        pClinicApplyBox.IsChecked = QClinic.QClinicRepairChecked;
        pClinicPersistentBox.IsChecked = QClinic.QClinicPersistentChecked;
        pClinicResultBody.Visibility = PLook.PLookVisible[QClinic.QClinicResultShown];
        pClinicDiagnosisProgress.Value = QClinic.QClinicProgressValue;
        pClinicDiagnosisProgress.Visibility = PLook.PLookVisible[QClinic.QClinicScanShown];
        pClinicResultText.Text = QClinic.QClinicResultFormat();
        pClinicTitleLabel.Text = QClinic.QClinicTitleRead();
        pClinicItemSimple.Text = QClinic.QClinicSimpleRead();
        pClinicItemTechnical.Text = QClinic.QClinicTechnicalRead();
    }

    private UIElement PClinicStripBuild()
    {
        Button pMaximizeButton = PClinicButtonBuild(
            "/PAsset/PPanel/PListMaximize.svg",
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Panel.ShowTooltip"),
            () => QClinic.QClinicMinimizedSet(false));
        pMaximizeButton.Margin = new Thickness(0, 6, 0, 0);
        pMaximizeButton.HorizontalAlignment = HorizontalAlignment.Center;

        var pStrip = new StackPanel { Background = Brushes.White };
        pStrip.Children.Add(pMaximizeButton);
        return pStrip;
    }

    private static UIElement PClinicSeparatorBuild() => new Border
    {
        Height = 1,
        Background = QPanel.QPanelLineRead(),
        Margin = new Thickness(0, 10, 0, 10)
    };

    private static Button PClinicButtonBuild(string pIconPath, string pTooltip, Action pClick)
    {
        Button pButton = PClinicIconBuild(pIconPath, pTooltip);
        PClinicClickAttach(pButton, pClick);
        return pButton;
    }

    private static Button PClinicIconBuild(string pIconPath, string pTooltip)
    {
        var pButton = new Button
        {
            Content = new Image
            {
                Width = 20,
                Height = 20,
                Source = QIcon.QIconRead(pIconPath, pClinicIconBrush),
                Stretch = Stretch.Uniform
            },
            ToolTip = pTooltip,
            Width = 28,
            Height = 26
        };
        QButton.QButtonAttach(pButton, QButtonLook.QButtonLookPanel);
        return pButton;
    }

    private static void PClinicClickAttach(Button pButton, Action pClick) => pButton.Click += (_, _) => pClick();
}
