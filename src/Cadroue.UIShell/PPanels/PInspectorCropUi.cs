using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Cadroue.UIShell.PAssets;
using Cadroue.UIShell.PMainWindow;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PInspector
{
    private StackPanel PCropBodyBuild()
    {
        pInspectorInsetLeft = PInspectorInsetBuild();
        pInspectorInsetRight = PInspectorInsetBuild();
        pInspectorInsetTop = PInspectorInsetBuild();
        pInspectorInsetBottom = PInspectorInsetBuild();
        pInspectorRatioWidth = PCropFieldBuild();
        pInspectorRatioHeight = PCropFieldBuild();
        pInspectorRatioPreset = PInspectorRatioPresetBuild();
        pInspectorResolution = new TextBlock
        {
            Text = "—",
            FontSize = 11,
            FontFamily = pInspectorFontFamily,
            Foreground = pInspectorMutedBrush,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        pInspectorRatioFixed = new CheckBox
        {
            Content = LLocalization.LLocalizationTextRead("Inspector.Crop.FixedRatio"),
            FontSize = 12,
            FontFamily = pInspectorFontFamily,
            Foreground = PPanelTextBrush,
            VerticalContentAlignment = VerticalAlignment.Center,
            Margin = new Thickness(PInspectorLabelWidth, 8, 0, 0)
        };
        PCheckbox.PCheckboxApply(pInspectorRatioFixed);
        pInspectorRatioFixed.Checked += (_, _) => PInspectorRatioCommit();
        pInspectorRatioFixed.Unchecked += (_, _) => PInspectorRatioCommit();

        pInspectorRatioNotice = new TextBlock
        {
            FontSize = 11,
            FontFamily = pInspectorFontFamily,
            Foreground = pInspectorWarnBrush,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(PInspectorLabelWidth, 6, 0, 0),
            Visibility = Visibility.Collapsed
        };

        pInspectorFlipHorizontal = PCropCheckBuild(LLocalization.LLocalizationTextRead("Inspector.Crop.Horizontal"));
        pInspectorFlipVertical = PCropCheckBuild(LLocalization.LLocalizationTextRead("Inspector.Crop.Vertical"));
        pInspectorFlipHorizontal.Checked += (_, _) => PInspectorRotateRaise();
        pInspectorFlipHorizontal.Unchecked += (_, _) => PInspectorRotateRaise();
        pInspectorFlipVertical.Checked += (_, _) => PInspectorRotateRaise();
        pInspectorFlipVertical.Unchecked += (_, _) => PInspectorRotateRaise();
        pInspectorRotateCombo = PInspectorRotateBuild();
        pInspectorCropTool = PInspectorToolBuild();

        pInspectorApplyBox = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Apply"),
            LLocalization.LLocalizationTextRead("Inspector.Crop.ApplyTooltip"));
        pInspectorApplyBox.Checked += (_, _) => PInspectorApplyUpdate();
        pInspectorApplyBox.Unchecked += (_, _) => PInspectorApplyUpdate();

        pInspectorCropStack = new StackPanel();
        pInspectorCropStack.Children.Add(PInspectorFieldBuild(LLocalization.LLocalizationTextRead("Inspector.Crop.Tool"), pInspectorCropTool));
        pInspectorCropStack.Children.Add(PInspectorFieldBuild(LLocalization.LLocalizationTextRead("Inspector.Crop.Flip"), PCropFlipBuild()));
        pInspectorCropStack.Children.Add(PInspectorFieldBuild(LLocalization.LLocalizationTextRead("Inspector.Crop.Rotate"), pInspectorRotateCombo));
        pInspectorCropStack.Children.Add(PInspectorEdgeBuild());
        pInspectorCropStack.Children.Add(PCropRatioBuild());
        pInspectorCropStack.Children.Add(pInspectorRatioFixed);
        pInspectorCropStack.Children.Add(pInspectorRatioNotice);

        pInspectorCropBody = new StackPanel
        {
            Margin = new Thickness(12, 12, 12, 12),
            Visibility = Visibility.Collapsed
        };
        pInspectorCropBody.Children.Add(pInspectorApplyBox);
        pInspectorCropBody.Children.Add(PInspectorSeparatorBuild());
        pInspectorCropBody.Children.Add(pInspectorCropStack);
        PInspectorToolUpdate();
        PInspectorApplyUpdate();
        return pInspectorCropBody;
    }

    public void PCropPersistentApply(bool pCropPersistent)
    {
        pInspectorPersistentBox.IsChecked = pCropPersistent;
    }

    private UIElement PInspectorPersistentBuild()
    {
        pInspectorPersistentBox = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Persistent"),
            LLocalization.LLocalizationTextRead("Inspector.Crop.PersistentTooltip"));
        pInspectorPersistentBox.Checked += (_, _) => PInspectorPersistentRaise();
        pInspectorPersistentBox.Unchecked += (_, _) => PInspectorPersistentRaise();

        var pPersistentPanel = new StackPanel { Visibility = Visibility.Collapsed };
        pPersistentPanel.Children.Add(new Border
        {
            Height = 1,
            Background = PPanelLineBrush,
            Margin = new Thickness(12, 0, 12, 12)
        });
        pPersistentPanel.Children.Add(PInspectorPersistentPrepare(pInspectorPersistentBox));
        pPersistentPanel.Children.Add(PInspectorPersistentPrepare(pInspectorBrightnessPersistent));
        pPersistentPanel.Children.Add(PInspectorPersistentPrepare(pInspectorContrastPersistent));
        pPersistentPanel.Children.Add(PInspectorPersistentPrepare(pInspectorHighPass.PInspectorPassPersistent));
        pPersistentPanel.Children.Add(PInspectorPersistentPrepare(pInspectorLowPass.PInspectorPassPersistent));
        pPersistentPanel.Children.Add(PInspectorPersistentPrepare(pNoisePersistent));
        pPersistentPanel.Children.Add(PInspectorPersistentPrepare(pInspectorVolumePersistent));
        pPersistentPanel.Children.Add(PInspectorPersistentPrepare(pLoudnessPersistent));
        pPersistentPanel.Children.Add(PInspectorPersistentPrepare(pEqualizerPersistent));
        pPersistentPanel.Children.Add(PInspectorPersistentPrepare(pSkipPersistentBox));
        return pPersistentPanel;
    }

    private CheckBox PInspectorPersistentPrepare(CheckBox pPersistentBox)
    {
        pPersistentBox.Margin = new Thickness(12, 0, 12, 12);
        pPersistentBox.Visibility = Visibility.Collapsed;
        pPersistentBox.Checked += (_, _) => PInspectorPlanChange?.Invoke();
        pPersistentBox.Unchecked += (_, _) => PInspectorPlanChange?.Invoke();
        return pPersistentBox;
    }

    private static CheckBox PInspectorSwitchBuild(string pSwitchLabel, string pSwitchTip)
    {
        var pSwitch = new CheckBox
        {
            Content = pSwitchLabel,
            ToolTip = pSwitchTip,
            FontSize = 12,
            FontFamily = pInspectorFontFamily,
            Foreground = PPanelTextBrush,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        PCheckbox.PCheckboxApply(pSwitch);
        return pSwitch;
    }

    private static UIElement PInspectorSeparatorBuild() => new Border
    {
        Height = 1,
        Background = PPanelLineBrush,
        Margin = new Thickness(0, 12, 0, 12)
    };

    private ToggleButton PInspectorToolBuild()
    {
        pInspectorToolIcon = new Image
        {
            Width = 18,
            Height = 18,
            Source = PIcon.PIconRead(PCropIcon, pInspectorIconBrush),
            Stretch = Stretch.Uniform
        };

        var pToolButton = new ToggleButton
        {
            Content = pInspectorToolIcon,
            ToolTip = LLocalization.LLocalizationTextRead("Inspector.Crop.DrawTooltip"),
            Width = 32,
            Height = 32,
            VerticalAlignment = VerticalAlignment.Center,
            Style = PInspectorToolCreate()
        };
        pToolButton.Checked += (_, _) =>
        {
            PInspectorToolChange?.Invoke(true);
            PInspectorToolUpdate();
        };
        pToolButton.Unchecked += (_, _) =>
        {
            PInspectorToolChange?.Invoke(false);
            PInspectorCropClear();
            PInspectorToolUpdate();
        };
        return pToolButton;
    }

    private static Style PInspectorToolCreate() => PInspectorToolCreate(typeof(ToggleButton));

    private static Style PInspectorToolCreate(Type pControlType)
    {
        var pTemplate = new ControlTemplate(pControlType);
        var pBorder = new FrameworkElementFactory(typeof(Border));
        pBorder.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
        pBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));

        var pContent = new FrameworkElementFactory(typeof(ContentPresenter));
        pContent.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        pContent.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        pBorder.AppendChild(pContent);
        pTemplate.VisualTree = pBorder;

        var pStyle = new Style(pControlType);
        pStyle.Setters.Add(new Setter(FrameworkElement.FocusVisualStyleProperty, null));
        pStyle.Setters.Add(new Setter(FrameworkElement.CursorProperty, System.Windows.Input.Cursors.Hand));
        pStyle.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
        pStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
        pStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(0)));
        pStyle.Setters.Add(new Setter(Control.TemplateProperty, pTemplate));
        return pStyle;
    }

    private ComboBox PInspectorRotateBuild()
    {
        var pRotateCombo = new ComboBox
        {
            Height = PInspectorFieldHeight,
            Width = 140,
            HorizontalAlignment = HorizontalAlignment.Left,
            FontSize = 12,
            FontFamily = pInspectorFontFamily
        };
        PDropdown.PDropdownApply(pRotateCombo);
        pRotateCombo.Items.Add(new LLocalizationChoice("None", "Inspector.Crop.None"));
        pRotateCombo.Items.Add(new LLocalizationChoice("Clockwise90", "Inspector.Crop.Clockwise90"));
        pRotateCombo.Items.Add(new LLocalizationChoice("Degrees180", "Inspector.Crop.Degrees180"));
        pRotateCombo.Items.Add(new LLocalizationChoice("Clockwise270", "Inspector.Crop.Clockwise270"));
        pRotateCombo.SelectedIndex = 0;
        pRotateCombo.SelectionChanged += (_, _) =>
        {
            PInspectorRatioUpdate();
            PInspectorRotateRaise();
        };
        return pRotateCombo;
    }

    private UIElement PInspectorEdgeBuild()
    {
        var pCropGrid = new Grid { Margin = new Thickness(0, 14, 0, 4) };
        for (int pColumn = 0; pColumn < 3; pColumn++)
        {
            pCropGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        }

        for (int pRow = 0; pRow < 3; pRow++)
        {
            pCropGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

        PInspectorCellAdd(pCropGrid, PInspectorCellBuild(LLocalization.LLocalizationTextRead("Inspector.Crop.Top"), pInspectorInsetTop), 0, 1);
        PInspectorCellAdd(pCropGrid, PInspectorCellBuild(LLocalization.LLocalizationTextRead("Inspector.Crop.Left"), pInspectorInsetLeft), 1, 0);
        PInspectorCellAdd(pCropGrid, PInspectorCellBuild(LLocalization.LLocalizationTextRead("Inspector.Crop.Right"), pInspectorInsetRight), 1, 2);
        PInspectorCellAdd(pCropGrid, PInspectorCellBuild(LLocalization.LLocalizationTextRead("Inspector.Crop.Bottom"), pInspectorInsetBottom), 2, 1);
        PInspectorCellAdd(pCropGrid, PInspectorResolutionBuild(), 1, 1);
        PInspectorCellAdd(pCropGrid, PInspectorResetBuild(), 2, 2);
        return pCropGrid;
    }

    private UIElement PInspectorResolutionBuild() => new Border
    {
        MinWidth = 84,
        Height = PInspectorFieldHeight,
        Padding = new Thickness(7, 0, 7, 0),
        Background = new SolidColorBrush(Color.FromRgb(0xF7, 0xF9, 0xFC)),
        BorderBrush = PPanelLineBrush,
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(8),
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Bottom,
        Margin = new Thickness(3),
        ToolTip = LLocalization.LLocalizationTextRead("Inspector.Crop.Resolution"),
        Child = pInspectorResolution
    };

    private UIElement PInspectorResetBuild()
    {
        var pResetButton = new Button
        {
            Content = LLocalization.LLocalizationTextRead("Inspector.Crop.Reset"),
            ToolTip = LLocalization.LLocalizationTextRead("Inspector.Crop.ResetTooltip"),
            Height = PInspectorFieldHeight,
            MinWidth = 64,
            Padding = new Thickness(8, 0, 8, 0),
            FontSize = 11,
            FontFamily = pInspectorFontFamily,
            Style = PButton.PButtonPanelCreate(),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(3)
        };
        pResetButton.Click += (_, _) => PInspectorEdgesReset();
        return pResetButton;
    }

    private static void PInspectorCellAdd(Grid pCropGrid, UIElement pCell, int pRow, int pColumn)
    {
        Grid.SetRow(pCell, pRow);
        Grid.SetColumn(pCell, pColumn);
        pCropGrid.Children.Add(pCell);
    }

    private static UIElement PInspectorCellBuild(string pCellLabel, TextBox pCellBox)
    {
        var pCellPanel = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 3, 0, 3)
        };
        pCellPanel.Children.Add(new TextBlock
        {
            Text = pCellLabel,
            FontSize = 11,
            FontFamily = pInspectorFontFamily,
            Foreground = pInspectorMutedBrush,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 3)
        });
        pCellPanel.Children.Add(pCellBox);
        return pCellPanel;
    }

    private UIElement PCropRatioBuild()
    {
        var pRatioRoot = new StackPanel
        {
            Margin = new Thickness(0, 14, 0, 0)
        };
        var pPresetPanel = new StackPanel { Orientation = Orientation.Horizontal };
        pPresetPanel.Children.Add(PInspectorLabelBuild(LLocalization.LLocalizationTextRead("Inspector.Crop.Ratio")));
        pPresetPanel.Children.Add(pInspectorRatioPreset);

        pInspectorRatioCustomPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(PInspectorLabelWidth, 6, 0, 0)
        };
        pInspectorRatioCustomPanel.Children.Add(pInspectorRatioWidth);
        pInspectorRatioCustomPanel.Children.Add(new TextBlock
        {
            Text = "×",
            FontSize = 12,
            FontFamily = pInspectorFontFamily,
            Foreground = pInspectorMutedBrush,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(7, 0, 7, 0)
        });
        pInspectorRatioCustomPanel.Children.Add(pInspectorRatioHeight);
        pRatioRoot.Children.Add(pPresetPanel);
        pRatioRoot.Children.Add(pInspectorRatioCustomPanel);
        return pRatioRoot;
    }

    private ComboBox PInspectorRatioPresetBuild()
    {
        var pPresetCombo = new ComboBox
        {
            Height = PInspectorFieldHeight,
            Width = 140,
            HorizontalAlignment = HorizontalAlignment.Left,
            FontSize = 12,
            FontFamily = pInspectorFontFamily
        };
        PDropdown.PDropdownApply(pPresetCombo);
        pPresetCombo.Items.Add(new LLocalizationChoice("Custom", "Inspector.Crop.RatioCustom"));
        pPresetCombo.Items.Add("16:9");
        pPresetCombo.Items.Add("9:16");
        pPresetCombo.Items.Add("4:3");
        pPresetCombo.Items.Add("3:4");
        pPresetCombo.Items.Add("1:1");
        pPresetCombo.Items.Add("21:9");
        pPresetCombo.SelectedIndex = 0;
        pPresetCombo.SelectionChanged += (_, _) => PInspectorRatioPresetHandle();
        return pPresetCombo;
    }

    private UIElement PCropFlipBuild()
    {
        var pFlipPanel = new StackPanel { Orientation = Orientation.Horizontal };
        pFlipPanel.Children.Add(pInspectorFlipHorizontal);
        pFlipPanel.Children.Add(pInspectorFlipVertical);
        return pFlipPanel;
    }

    private static CheckBox PCropCheckBuild(string pFlipLabel)
    {
        var pFlip = new CheckBox
        {
            Content = pFlipLabel,
            FontSize = 12,
            FontFamily = pInspectorFontFamily,
            Foreground = PPanelTextBrush,
            VerticalAlignment = VerticalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 14, 0)
        };
        PCheckbox.PCheckboxApply(pFlip);
        return pFlip;
    }

    private TextBox PCropFieldBuild()
    {
        TextBox pRatioBox = PInspectorNumberBuild();
        pRatioBox.TextChanged += (_, _) => PCropRatioHandle();
        return pRatioBox;
    }

    private TextBox PInspectorInsetBuild()
    {
        TextBox pInsetBox = PInspectorNumberBuild();
        pInsetBox.TextChanged += (_, _) =>
        {
            PInspectorRatioUpdate();
            PInspectorCropRaise();
        };
        return pInsetBox;
    }

    private static TextBox PInspectorNumberBuild()
    {
        var pNumberBox = new TextBox
        {
            Text = "0",
            Width = PInspectorInsetWidth,
            Height = PInspectorFieldHeight,
            FontSize = 12,
            FontFamily = pInspectorFontFamily
        };
        PTextbox.PTextboxApply(pNumberBox);
        pNumberBox.TextAlignment = TextAlignment.Center;
        pNumberBox.Padding = new Thickness(4, 0, 4, 0);
        pNumberBox.PreviewTextInput += (_, pNumberEvent) =>
            pNumberEvent.Handled = !pNumberEvent.Text.All(char.IsDigit);
        return pNumberBox;
    }
}
