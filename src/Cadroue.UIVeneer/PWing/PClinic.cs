using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.Application;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PAsset;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PWing;

public sealed class PClinic : PPanel
{
    private static readonly FontFamily pClinicFontFamily = new("Segoe UI");
    private static readonly Brush pClinicTitleBrush = new SolidColorBrush(Color.FromRgb(0x26, 0x36, 0x4A));
    private static readonly Brush pClinicMutedBrush = new SolidColorBrush(Color.FromRgb(0x8A, 0x93, 0x9E));
    private static readonly Brush pClinicIconBrush = new SolidColorBrush(Color.FromRgb(0x56, 0x62, 0x73));

    public const double PClinicStripWidth = 48;

    public event Action<bool>? PClinicMinimizeChange;
    public event Action? PClinicDiagnosisRun;

    private readonly UIElement pClinicFullBody;
    private readonly UIElement pClinicStripBody;
    private readonly TextBlock pClinicTitleLabel;
    private readonly TextBlock pClinicEmptyNotice;
    private readonly UIElement pClinicItemBody;
    private readonly StackPanel pClinicToggleRow;
    private readonly TextBlock pClinicItemSimple;
    private readonly TextBlock pClinicItemTechnical;
    private readonly UIElement pClinicResultBody;
    private readonly TextBlock pClinicResultText;
    private readonly ProgressBar pClinicDiagnosisProgress;
    private readonly CheckBox pClinicApplyBox;
    private readonly CheckBox pClinicPersistentBox;
    private readonly Button pClinicDiagnosisButton;
    private readonly PClinicSalvage pClinicSalvage;

    public LClinic LClinic { get; } = new();

    public PClinic() : base("")
    {
        pClinicSalvage = new PClinicSalvage(LClinic);
        pClinicTitleLabel = new TextBlock
        {
            Text = LLocalization.LLocalizationTextRead("Clinic.Header.Title"),
            FontSize = 12,
            FontFamily = pClinicFontFamily,
            FontWeight = FontWeights.SemiBold,
            Foreground = pClinicTitleBrush,
            VerticalAlignment = VerticalAlignment.Center
        };

        Button pMinimizeButton = PClinicButtonBuild(
            "/PAsset/PPanel/PListMinimize.svg",
            LLocalization.LLocalizationTextRead("Inspector.Panel.HideTooltip"),
            () => LClinic.LClinicMinimizedSet(true));
        pMinimizeButton.HorizontalAlignment = HorizontalAlignment.Right;

        var pHeaderGrid = new Grid();
        pHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(pMinimizeButton, 1);
        pHeaderGrid.Children.Add(pClinicTitleLabel);
        pHeaderGrid.Children.Add(pMinimizeButton);

        var pHeader = new Border
        {
            Padding = new Thickness(12, 5, 6, 5),
            BorderBrush = PPanelLineBrush,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Background = Brushes.White,
            Child = pHeaderGrid
        };

        pClinicApplyBox = PClinicSwitchBuild(
            LLocalization.LLocalizationTextRead("Clinic.Apply"),
            LLocalization.LLocalizationTextRead("Clinic.Apply.Tooltip"));
        pClinicApplyBox.Checked += (_, _) => LClinic.LClinicActiveSet(true);
        pClinicApplyBox.Unchecked += (_, _) => LClinic.LClinicActiveSet(false);

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
            Text = LLocalization.LLocalizationTextRead("Clinic.Empty.Notice"),
            FontSize = 12,
            FontFamily = pClinicFontFamily,
            Foreground = pClinicMutedBrush,
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(16, 24, 16, 16)
        };

        pClinicItemSimple = new TextBlock
        {
            FontSize = 12,
            FontFamily = pClinicFontFamily,
            Foreground = PPanelTextBrush,
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
        pClinicItemBody = pItemStack;
        pClinicItemBody.Visibility = Visibility.Collapsed;

        var pResultHeader = new TextBlock
        {
            Text = LLocalization.LLocalizationTextRead("Clinic.Result.Header"),
            FontSize = 12,
            FontFamily = pClinicFontFamily,
            FontWeight = FontWeights.SemiBold,
            Foreground = pClinicTitleBrush,
            Margin = new Thickness(0, 0, 0, 6)
        };
        pClinicResultText = new TextBlock
        {
            Text = LLocalization.LLocalizationTextRead("Clinic.Result.Empty"),
            FontSize = 12,
            FontFamily = pClinicFontFamily,
            Foreground = pClinicMutedBrush,
            TextWrapping = TextWrapping.Wrap
        };
        pClinicDiagnosisProgress = PInspector.PSensorProgressBuild();

        var pResultStack = new StackPanel();
        pResultStack.Children.Add(PClinicSeparatorBuild());
        pResultStack.Children.Add(pResultHeader);
        pResultStack.Children.Add(pClinicResultText);
        pClinicResultBody = pResultStack;
        pClinicResultBody.Visibility = Visibility.Collapsed;

        var pBody = new StackPanel { Margin = new Thickness(12, 10, 12, 10) };
        pBody.Children.Add(pClinicToggleRow);
        pBody.Children.Add(PClinicSeparatorBuild());
        pBody.Children.Add(pClinicEmptyNotice);
        pBody.Children.Add(pClinicItemBody);
        pBody.Children.Add(pClinicResultBody);
        pBody.Children.Add(pClinicSalvage);

        var pScroll = new ScrollViewer
        {
            Content = pBody,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };

        pClinicPersistentBox = PClinicSwitchBuild(
            LLocalization.LLocalizationTextRead("Clinic.Persistent"),
            LLocalization.LLocalizationTextRead("Clinic.Persistent.Tooltip"));
        pClinicPersistentBox.IsEnabled = false;
        pClinicPersistentBox.Checked += (_, _) => LClinic.LClinicPersistentSet(true);
        pClinicPersistentBox.Unchecked += (_, _) => LClinic.LClinicPersistentSet(false);
        pClinicSalvage.PClinicSalvagePersistent.Visibility = Visibility.Collapsed;
        pClinicPersistentBox.VerticalAlignment = VerticalAlignment.Center;
        var pPersistentStack = new Grid { VerticalAlignment = VerticalAlignment.Center };
        pPersistentStack.Children.Add(pClinicPersistentBox);
        pPersistentStack.Children.Add(pClinicSalvage.PClinicSalvagePersistent);

        pClinicDiagnosisButton = new Button
        {
            Content = LLocalization.LLocalizationTextRead("Clinic.Diagnosis.Run"),
            ToolTip = LLocalization.LLocalizationTextRead("Clinic.Diagnosis.Run.Tooltip"),
            Height = 28,
            MinWidth = 90,
            FontSize = 12,
            FontFamily = pClinicFontFamily,
            VerticalAlignment = VerticalAlignment.Center,
            Style = PButton.PButtonWhiteCreate()
        };
        pClinicDiagnosisButton.Click += (_, _) => PClinicDiagnosisRun?.Invoke();

        var pRunGrid = new Grid();
        pRunGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pRunGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(pClinicDiagnosisButton, 1);
        pRunGrid.Children.Add(pPersistentStack);
        pRunGrid.Children.Add(pClinicDiagnosisButton);

        var pRunStack = new StackPanel();
        pRunStack.Children.Add(pClinicDiagnosisProgress);
        pRunStack.Children.Add(pRunGrid);

        var pPersistentRow = new Border
        {
            Padding = new Thickness(12, 6, 12, 8),
            BorderBrush = PPanelLineBrush,
            BorderThickness = new Thickness(0, 1, 0, 0),
            Background = Brushes.White,
            Child = pRunStack
        };

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
        Content = PPanelBorderBuild(pBodyHost);
        LClinic.LClinicChange += PClinicUpdate;
        LClinic.LClinicMinimizeChange += PClinicMinimizeHandle;
        PClinicUpdate();
    }

    public bool PClinicMinimizedCheck() => LClinic.LClinicMinimized;

    public void PClinicMinimizeSet(bool pClinicMinimizeRequest) => LClinic.LClinicMinimizedSet(pClinicMinimizeRequest);

    public void PClinicStepShow(string? pStepName) => LClinic.LClinicStepSet(pStepName);

    internal static CheckBox PClinicSwitchBuild(string pSwitchLabel, string pSwitchTip)
    {
        var pSwitch = new CheckBox
        {
            Content = pSwitchLabel,
            ToolTip = pSwitchTip,
            FontSize = 12,
            FontFamily = pClinicFontFamily,
            Foreground = PPanelTextBrush,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        PCheckbox.PCheckboxApply(pSwitch);
        return pSwitch;
    }

    private void PClinicMinimizeHandle(bool pClinicMinimized)
    {
        pClinicFullBody.Visibility = PLook.PLookVisible[!pClinicMinimized];
        pClinicStripBody.Visibility = PLook.PLookVisible[pClinicMinimized];
        PClinicMinimizeChange?.Invoke(pClinicMinimized);
    }

    private void PClinicUpdate()
    {
        pClinicSalvage.PClinicSalvageUpdate();
        pClinicApplyBox.Visibility = PLook.PLookVisible[!LClinic.LClinicSalvageShown];
        pClinicPersistentBox.Visibility = PLook.PLookVisible[!LClinic.LClinicSalvageShown];
        pClinicDiagnosisButton.Visibility = PLook.PLookVisible[!LClinic.LClinicSalvageShown];
        pClinicSalvage.PClinicSalvageActive.Visibility = PLook.PLookVisible[LClinic.LClinicSalvageShown];
        pClinicSalvage.PClinicSalvagePersistent.Visibility = PLook.PLookVisible[LClinic.LClinicSalvageShown];
        pClinicToggleRow.Visibility = Visibility.Visible;
        pClinicEmptyNotice.Visibility = PLook.PLookVisible[!LClinic.LClinicKnown];
        pClinicItemBody.Visibility = PLook.PLookVisible[LClinic.LClinicKnown];
        pClinicToggleRow.IsEnabled = LClinic.LClinicKnown;
        pClinicPersistentBox.IsEnabled = LClinic.LClinicPersistentAllowed;
        pClinicApplyBox.IsChecked = LClinic.LClinicRepairChecked;
        pClinicPersistentBox.IsChecked = LClinic.LClinicPersistentChecked;
        pClinicResultBody.Visibility = PLook.PLookVisible[LClinic.LClinicResultShown];
        pClinicDiagnosisProgress.Value = LClinic.LClinicProgressValue;
        pClinicDiagnosisProgress.Visibility = PLook.PLookVisible[LClinic.LClinicScanShown];
        pClinicResultText.Text = LClinic.LClinicResultFormat();
        pClinicTitleLabel.Text = LClinic.LClinicTitleRead();
        pClinicItemSimple.Text = LClinic.LClinicSimpleRead();
        pClinicItemTechnical.Text = LClinic.LClinicTechnicalRead();
    }

    private UIElement PClinicStripBuild()
    {
        Button pMaximizeButton = PClinicButtonBuild(
            "/PAsset/PPanel/PListMaximize.svg",
            LLocalization.LLocalizationTextRead("Inspector.Panel.ShowTooltip"),
            () => LClinic.LClinicMinimizedSet(false));
        pMaximizeButton.Margin = new Thickness(0, 6, 0, 0);
        pMaximizeButton.HorizontalAlignment = HorizontalAlignment.Center;

        var pStrip = new StackPanel { Background = Brushes.White };
        pStrip.Children.Add(pMaximizeButton);
        return pStrip;
    }

    private static UIElement PClinicSeparatorBuild() => new Border
    {
        Height = 1,
        Background = PPanelLineBrush,
        Margin = new Thickness(0, 10, 0, 10)
    };

    private static Button PClinicButtonBuild(string pIconPath, string pTooltip, Action pClick)
    {
        var pButton = new Button
        {
            Content = new Image
            {
                Width = 14,
                Height = 14,
                Source = PIcon.PIconRead(pIconPath, pClinicIconBrush),
                Stretch = Stretch.Uniform
            },
            ToolTip = pTooltip,
            Width = 28,
            Height = 26,
            Style = PButton.PButtonPanelCreate()
        };
        pButton.Click += (_, _) => pClick();
        return pButton;
    }
}
