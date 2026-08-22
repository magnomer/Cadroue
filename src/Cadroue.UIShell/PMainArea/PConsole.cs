using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.ShellEngine;
using Cadroue.UIShell.PAssets;
using Cadroue.UIShell.PMainWindow;
using Cadroue.UIShell.PSShared;

namespace Cadroue.UIShell.PMainArea;

public sealed partial class PConsole : UserControl
{
    private const double PConsoleProgressHeight = 14;
    private const double PConsoleStatusSize = 13;
    private const double PConsoleStationSize = 12;
    private const double PConsoleSwitchWidth = 34;
    private const double PConsoleSwitchSize = 18;

    private readonly LScheduleContract pConsoleSchedule = PProgram.LScheduleCurrent;
    private readonly Button pConsolePreviousButton;
    private readonly Button pConsoleNextButton;
    private readonly CheckBox pConsoleAutoBox;
    private readonly ComboBox pConsoleRelayCombo;
    private readonly Button pConsoleSaveButton;
    private readonly Button pConsoleExportButton;
    private readonly Button pConsoleImportButton;
    private readonly TextBlock pConsoleStationLabel;
    private readonly ProgressBar pConsoleProgress;
    private readonly TextBlock pConsoleStatus;
    private readonly Image pConsoleRestIcon;
    private readonly Path pConsoleSpinner;
    private readonly RotateTransform pConsoleSpinnerRotate = new(0);
    private bool pConsoleSpinning;
    private readonly Button pConsoleStartButton;
    private readonly Button pConsolePauseButton;
    private readonly Button pConsoleCancelButton;
    private readonly Button pConsoleStopButton;
    private readonly Button pConsoleRemoveButton;
    private readonly Button pConsoleClearButton;
    private readonly Button pConsoleEmptyButton;
    private readonly Button pConsoleTabsButton;
    private readonly StackPanel pConsoleSceneControls;
    private readonly ContentControl pConsoleSceneHost;
    private readonly Border pConsoleSceneSeparator;

    public PConsole()
    {
        FocusVisualStyle = null;
        pConsoleProgress = PConsoleProgressBuild();
        pConsoleStatus = PConsoleLabelBuild(PRosterTheme.PRosterTextBrush, PConsoleStatusSize);
        pConsoleRestIcon = PConsoleRestBuild();
        pConsoleSpinner = PConsoleSpinnerBuild();
        pConsoleStationLabel = PConsoleLabelBuild(PRosterTheme.PRosterMutedBrush, PConsoleStationSize);
        pConsoleStartButton = PConsoleButtonBuild(
            LLocalization.LLocalizationTextRead("Console.Button.Start"), "PRosterStart.svg", LLocalization.LLocalizationTextRead("Console.Button.StartTooltip"),
            PRosterTheme.PRosterDoneBrush, PConsoleStartHandle);
        pConsolePauseButton = PConsoleButtonBuild(
            LLocalization.LLocalizationTextRead("Console.Button.Pause"), "PRosterPause.svg", LLocalization.LLocalizationTextRead("Console.Button.PauseTooltip"),
            null, PConsolePauseHandle);
        pConsoleCancelButton = PConsoleButtonBuild(
            LLocalization.LLocalizationTextRead("Console.Button.Cancel"), "PRosterCancel.svg", LLocalization.LLocalizationTextRead("Console.Button.CancelTooltip"),
            null, PConsoleCancelHandle);
        pConsoleStopButton = PConsoleButtonBuild(
            LLocalization.LLocalizationTextRead("Console.Button.Stop"), "PRosterStop.svg", LLocalization.LLocalizationTextRead("Console.Button.StopTooltip"),
            PRosterTheme.PRosterFailBrush, PConsoleStopHandle);
        pConsoleRemoveButton = PConsoleButtonBuild(
            LLocalization.LLocalizationTextRead("Console.Button.Remove"), "PRosterRemove.svg", LLocalization.LLocalizationTextRead("Console.Button.RemoveTooltip"),
            null, PConsoleRemoveHandle);
        pConsoleClearButton = PConsoleButtonBuild(
            LLocalization.LLocalizationTextRead("Console.Button.ClearDone"), "PRosterClearDone.svg", LLocalization.LLocalizationTextRead("Console.Button.ClearDoneTooltip"),
            null, PConsoleDoneHandle);
        pConsoleEmptyButton = PConsoleButtonBuild(
            LLocalization.LLocalizationTextRead("Console.Button.ClearAll"), "PRosterClearAll.svg", LLocalization.LLocalizationTextRead("Console.Button.ClearAllTooltip"),
            null, PConsoleAllHandle);
        pConsoleTabsButton = PConsoleButtonBuild(
            LLocalization.LLocalizationTextRead("Console.Button.ClearTabs"), "PConsoleClearTabs.svg", LLocalization.LLocalizationTextRead("Console.Button.ClearTabsTooltip"),
            null, PConsoleTabsHandle);
        pConsoleAutoBox = PConsoleAutoBuild();
        pConsoleRelayCombo = PConsoleComboBuild();
        pConsoleSaveButton = PConsoleInlineBuild("PConsoleSave.svg");
        pConsoleExportButton = PConsoleInlineBuild("PExportExport.svg");
        pConsoleImportButton = PConsoleInlineBuild("PExportImport.svg");
        pConsoleSceneControls = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };
        pConsoleSceneControls.Children.Add(pConsoleRelayCombo);
        pConsoleSceneControls.Children.Add(pConsoleSaveButton);
        pConsoleSceneControls.Children.Add(pConsoleExportButton);
        pConsoleSceneControls.Children.Add(pConsoleImportButton);
        pConsoleSceneHost = new ContentControl { Content = pConsoleSceneControls };
        pConsoleSceneSeparator = PConsoleSeparatorBuild();
        pConsolePreviousButton = PConsoleSwitchBuild(
            "PConsolePrevious.svg", LLocalization.LLocalizationTextRead("Console.Previous.Tooltip"), PConsolePreviousHandle);
        pConsoleNextButton = PConsoleSwitchBuild(
            "PConsoleNext.svg", LLocalization.LLocalizationTextRead("Console.Next.Tooltip"), PConsoleNextHandle);

        Content = PConsoleBuild();
        PConsoleCurrent = this;
        PConsoleSceneAttach();

        pConsoleSchedule.LScheduleChange += PConsoleScheduleHandle;
        pConsoleSchedule.LScheduleItemChange += PConsoleItemHandle;
        LStation.LStationChange += PConsoleStationHandle;
        PConsoleDepotAttach();
        Unloaded += PConsoleUnloadHandle;

        PConsoleScheduleHandle(pConsoleSchedule);
        Dispatcher.BeginInvoke(new Action(() => pConsoleSchedule.LScheduleLoad()));
    }

    public static PConsole? PConsoleCurrent { get; private set; }

    public void PConsoleUpdate() => PConsoleProgressUpdate();

    public UIElement PConsoleSceneRead() => pConsoleSceneControls;

    public void PConsoleSceneSet(UIElement? pSceneControls)
    {
        pConsoleSceneHost.Content = pSceneControls;
        pConsoleSceneSeparator.Visibility = pSceneControls is null ? Visibility.Collapsed : Visibility.Visible;
    }

    private UIElement PConsoleBuild()
    {
        var pButtons = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        pButtons.Children.Add(pConsoleStartButton);
        pButtons.Children.Add(pConsolePauseButton);
        pButtons.Children.Add(pConsoleCancelButton);
        pButtons.Children.Add(pConsoleStopButton);
        pButtons.Children.Add(new Border { Width = 10 });
        pButtons.Children.Add(pConsoleRemoveButton);
        pButtons.Children.Add(pConsoleClearButton);
        pButtons.Children.Add(pConsoleEmptyButton);
        pButtons.Children.Add(new Border { Width = 10 });
        pButtons.Children.Add(pConsoleTabsButton);

        var pRow = new Grid();
        pRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var pAutoRow = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        pAutoRow.Children.Add(pConsoleAutoBox);
        pAutoRow.Children.Add(pConsoleSceneSeparator);
        pAutoRow.Children.Add(pConsoleSceneHost);

        var pStatusRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(28, 0, 12, 0)
        };
        pStatusRow.Children.Add(pConsoleRestIcon);
        pStatusRow.Children.Add(pConsoleSpinner);
        pStatusRow.Children.Add(pConsoleStatus);

        Grid.SetColumn(pButtons, 0);
        Grid.SetColumn(pStatusRow, 1);
        Grid.SetColumn(pConsoleStationLabel, 2);
        Grid.SetColumn(pAutoRow, 3);
        pConsoleStationLabel.Margin = new Thickness(0, 0, 4, 0);
        pRow.Children.Add(pButtons);
        pRow.Children.Add(pStatusRow);
        pRow.Children.Add(pConsoleStationLabel);
        pRow.Children.Add(pAutoRow);

        var pStack = new StackPanel();
        pStack.Children.Add(pRow);
        pStack.Children.Add(new Border { Height = 8 });
        pStack.Children.Add(pConsoleProgress);

        var pCard = new Border
        {
            Padding = new Thickness(8, 10, 8, 10),
            Background = Brushes.White,
            SnapsToDevicePixels = true,
            Child = pStack
        };

        Background = Brushes.White;
        var pRoot = new Grid();
        pRoot.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(PConsoleSwitchWidth) });
        pRoot.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pRoot.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(PConsoleSwitchWidth) });
        Grid.SetColumn(pConsolePreviousButton, 0);
        Grid.SetColumn(pCard, 1);
        Grid.SetColumn(pConsoleNextButton, 2);
        pRoot.Children.Add(pConsolePreviousButton);
        pRoot.Children.Add(pCard);
        pRoot.Children.Add(pConsoleNextButton);
        return pRoot;
    }

    private CheckBox PConsoleAutoBuild()
    {
        var pAutoBox = new CheckBox
        {
            Content = LLocalization.LLocalizationTextRead("Console.AutoResume.Label"),
            FontSize = PConsoleStationSize,
            Foreground = PRosterTheme.PRosterTextBrush,
            VerticalAlignment = VerticalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            Margin = new Thickness(14, 0, 4, 0),
            FocusVisualStyle = null,
            ToolTip = LLocalization.LLocalizationTextRead("Console.AutoResume.Tooltip")
        };
        PCheckbox.PCheckboxApply(pAutoBox);
        pAutoBox.Checked += PConsoleAutoHandle;
        pAutoBox.Unchecked += PConsoleAutoHandle;
        return pAutoBox;
    }

    private static ComboBox PConsoleComboBuild()
    {
        var pRelayCombo = new ComboBox
        {
            Width = 180,
            Height = PSField.PSFieldControlHeight,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        };
        PDropdown.PDropdownEditableApply(pRelayCombo);
        return pRelayCombo;
    }

    private static Border PConsoleSeparatorBuild() => new()
    {
        Width = 1,
        Margin = new Thickness(6, 2, 12, 2),
        VerticalAlignment = VerticalAlignment.Stretch,
        Background = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7))
    };

    private static Button PConsoleInlineBuild(string pIconName)
    {
        return new Button
        {
            Content = new Image
            {
                Source = PIcon.PIconRead($"/PAssets/PPanels/{pIconName}", PRosterTheme.PRosterTextBrush),
                Width = PConsoleSwitchSize,
                Height = PConsoleSwitchSize,
                Stretch = Stretch.Uniform
            },
            Width = 34,
            Height = PSField.PSFieldControlHeight,
            Padding = new Thickness(0),
            Margin = new Thickness(0, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Style = PButton.PButtonWhiteCreate()
        };
    }

    private static Button PConsoleSwitchBuild(string pIconName, string pTooltip, RoutedEventHandler pClick)
    {
        var pButton = new Button
        {
            Content = new Image
            {
                Source = PIcon.PIconRead($"/PAssets/PPanels/{pIconName}", PRosterTheme.PRosterTextBrush),
                Width = PConsoleSwitchSize,
                Height = PConsoleSwitchSize,
                Stretch = Stretch.Uniform
            },
            Width = PConsoleSwitchWidth,
            VerticalAlignment = VerticalAlignment.Stretch,
            Visibility = Visibility.Collapsed,
            Style = PConsoleButtonCreate(),
            ToolTip = pTooltip
        };
        pButton.Click += pClick;
        return pButton;
    }

    private static Button PConsoleButtonBuild(
        string pLabel,
        string pIconName,
        string pTooltip,
        Brush? pAccentBrush,
        RoutedEventHandler pClick)
    {
        var pStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        pStack.Children.Add(new Image
        {
            Source = PIcon.PIconRead(
                $"/PAssets/PPanels/{pIconName}",
                pAccentBrush ?? PRosterTheme.PRosterTextBrush),
            Width = PRosterTheme.PRosterIconSize,
            Height = PRosterTheme.PRosterIconSize,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center
        });
        pStack.Children.Add(new Border { Height = 2 });
        pStack.Children.Add(new TextBlock
        {
            Text = pLabel,
            FontSize = PRosterTheme.PRosterRowSize,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        });

        var pButton = new Button
        {
            Width = PRosterTheme.PRosterButtonSize,
            Height = PRosterTheme.PRosterButtonSize,
            Margin = new Thickness(0, 0, 4, 0),
            Content = pStack,
            Style = PConsoleButtonCreate(),
            ToolTip = pTooltip
        };
        pButton.Click += pClick;
        return pButton;
    }

    private static Style PConsoleButtonCreate()
    {
        Style pStyle = PButton.PButtonCommandCreate();
        var pDisabled = new Trigger { Property = UIElement.IsEnabledProperty, Value = false };
        pDisabled.Setters.Add(new Setter(UIElement.OpacityProperty, PRosterTheme.PRosterDisabledOpacity));
        pStyle.Triggers.Add(pDisabled);
        return pStyle;
    }

    private static TextBlock PConsoleLabelBuild(Brush pBrush, double pFontSize) => new()
    {
        FontSize = pFontSize,
        Foreground = pBrush,
        VerticalAlignment = VerticalAlignment.Center,
        TextTrimming = TextTrimming.CharacterEllipsis
    };

    private const double PConsoleIndicatorSize = PRosterTheme.PRosterIconSize;

    private static Image PConsoleRestBuild() => new()
    {
        Source = PIcon.PIconRead("/PAssets/PPanels/PConsoleRest.svg", PRosterTheme.PRosterTextBrush),
        Width = PConsoleIndicatorSize,
        Height = PConsoleIndicatorSize,
        Stretch = Stretch.Uniform,
        VerticalAlignment = VerticalAlignment.Center,
        Margin = new Thickness(0, 0, 10, 0),
        Visibility = Visibility.Collapsed
    };

    private static readonly Brush pConsoleSpinnerBrush = PConsoleSpinnerCreate();

    private static Brush PConsoleSpinnerCreate()
    {
        var pBrush = new LinearGradientBrush
        {
            StartPoint = new System.Windows.Point(0, 0),
            EndPoint = new System.Windows.Point(1, 1)
        };
        pBrush.GradientStops.Add(new GradientStop(Color.FromRgb(0x1E, 0x59, 0xBE), 0));
        pBrush.GradientStops.Add(new GradientStop(Color.FromRgb(0x3E, 0x92, 0xE4), 0.5));
        pBrush.GradientStops.Add(new GradientStop(Color.FromRgb(0x74, 0xCB, 0xF7), 1));
        pBrush.Freeze();
        return pBrush;
    }

    private Path PConsoleSpinnerBuild()
    {
        const double pStroke = 3;
        double pRadius = (PConsoleIndicatorSize - pStroke) / 2;
        double pCenter = PConsoleIndicatorSize / 2;
        var pStart = new System.Windows.Point(pCenter, pCenter - pRadius);
        var pEnd = new System.Windows.Point(pCenter - pRadius, pCenter);

        var pFigure = new PathFigure { StartPoint = pStart };
        pFigure.Segments.Add(new ArcSegment(
            pEnd, new System.Windows.Size(pRadius, pRadius), 0, true, SweepDirection.Clockwise, true));
        var pGeometry = new PathGeometry();
        pGeometry.Figures.Add(pFigure);
        pGeometry.Freeze();

        return new Path
        {
            Data = pGeometry,
            Stroke = pConsoleSpinnerBrush,
            StrokeThickness = pStroke,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            Width = PConsoleIndicatorSize,
            Height = PConsoleIndicatorSize,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 10, 0),
            RenderTransformOrigin = new System.Windows.Point(0.5, 0.5),
            RenderTransform = pConsoleSpinnerRotate,
            Visibility = Visibility.Collapsed
        };
    }

    private void PConsoleSpinnerSet(bool pActive)
    {
        if (pActive == pConsoleSpinning)
        {
            return;
        }

        pConsoleSpinning = pActive;
        if (pActive)
        {
            var pSpin = new DoubleAnimation
            {
                From = 0,
                To = 360,
                Duration = new Duration(TimeSpan.FromSeconds(1.1)),
                RepeatBehavior = RepeatBehavior.Forever
            };
            pConsoleSpinnerRotate.BeginAnimation(RotateTransform.AngleProperty, pSpin);
        }
        else
        {
            pConsoleSpinnerRotate.BeginAnimation(RotateTransform.AngleProperty, null);
        }
    }

    private static readonly Brush pConsoleProgressBrush = PConsoleProgressCreate();
    private static readonly Brush pConsoleProgressGloss = PConsoleGlossCreate();

    private static Brush PConsoleProgressCreate()
    {
        var pBrush = new LinearGradientBrush
        {
            StartPoint = new System.Windows.Point(0, 1),
            EndPoint = new System.Windows.Point(1, 0)
        };
        pBrush.GradientStops.Add(new GradientStop(Color.FromRgb(0x1E, 0x59, 0xBE), 0));
        pBrush.GradientStops.Add(new GradientStop(Color.FromRgb(0x3E, 0x92, 0xE4), 0.55));
        pBrush.GradientStops.Add(new GradientStop(Color.FromRgb(0x74, 0xCB, 0xF7), 1));
        pBrush.Freeze();
        return pBrush;
    }

    private static Brush PConsoleGlossCreate()
    {
        var pGloss = new LinearGradientBrush
        {
            StartPoint = new System.Windows.Point(0, 0),
            EndPoint = new System.Windows.Point(0, 1)
        };
        pGloss.GradientStops.Add(new GradientStop(Color.FromArgb(0x59, 0xFF, 0xFF, 0xFF), 0));
        pGloss.GradientStops.Add(new GradientStop(Color.FromArgb(0x14, 0xFF, 0xFF, 0xFF), 0.45));
        pGloss.GradientStops.Add(new GradientStop(Color.FromArgb(0x00, 0xFF, 0xFF, 0xFF), 0.46));
        pGloss.Freeze();
        return pGloss;
    }

    private static ProgressBar PConsoleProgressBuild() => new()
    {
        Height = PConsoleProgressHeight,
        Minimum = 0,
        Maximum = 1,
        Value = 0,
        Background = PRosterTheme.PRosterTrackBrush,
        Foreground = pConsoleProgressBrush,
        BorderThickness = new Thickness(0),
        Template = PConsoleTrackBuild()
    };

    private static ControlTemplate PConsoleTrackBuild()
    {
        var pTrack = new FrameworkElementFactory(typeof(Border));
        pTrack.Name = "PART_Track";
        pTrack.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
        pTrack.SetValue(Border.CornerRadiusProperty, new CornerRadius(PConsoleProgressHeight / 2));

        pTrack.SetValue(Border.BorderBrushProperty, PRosterTheme.PRosterLineBrush);
        pTrack.SetValue(Border.BorderThicknessProperty, new Thickness(1));

        var pIndicator = new FrameworkElementFactory(typeof(Border));
        pIndicator.Name = "PART_Indicator";
        pIndicator.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Left);
        pIndicator.SetValue(Border.BackgroundProperty, Brushes.Transparent);
        pIndicator.SetValue(Border.CornerRadiusProperty, new CornerRadius(PConsoleProgressHeight / 2));
        pIndicator.SetValue(UIElement.SnapsToDevicePixelsProperty, true);
        pIndicator.SetValue(UIElement.ClipToBoundsProperty, true);

        var pFill = new FrameworkElementFactory(typeof(Border));
        pFill.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.ForegroundProperty));
        pFill.SetValue(Border.CornerRadiusProperty, new CornerRadius(PConsoleProgressHeight / 2));
        pFill.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Left);
        pFill.SetValue(UIElement.IsHitTestVisibleProperty, false);
        pFill.SetBinding(FrameworkElement.WidthProperty, new System.Windows.Data.Binding("ActualWidth")
        {
            RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent)
        });

        var pGloss = new FrameworkElementFactory(typeof(Border));
        pGloss.SetValue(Border.BackgroundProperty, pConsoleProgressGloss);
        pGloss.SetValue(Border.CornerRadiusProperty, new CornerRadius(PConsoleProgressHeight / 2));
        pGloss.SetValue(UIElement.IsHitTestVisibleProperty, false);
        pFill.AppendChild(pGloss);
        pIndicator.AppendChild(pFill);

        var pRoot = new FrameworkElementFactory(typeof(Grid));
        pRoot.AppendChild(pTrack);
        pRoot.AppendChild(pIndicator);
        return new ControlTemplate(typeof(ProgressBar)) { VisualTree = pRoot };
    }
}
