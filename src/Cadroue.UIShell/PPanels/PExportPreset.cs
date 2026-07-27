using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIShell.PAssets;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PExport
{
    private const string PExportPlusIconPath = "/PAssets/PPanels/PExportPlus.svg";
    private const string PExportMinusIconPath = "/PAssets/PPanels/PExportMinus.svg";
    private const string PExportSettingIconPath = "/PAssets/PPanels/PExportSetting.svg";
    private const string PExportImportIconPath = "/PAssets/PPanels/PExportImport.svg";
    private const string PExportExportIconPath = "/PAssets/PPanels/PExportExport.svg";
    private static Style? pExportButtonStyle;

    private UIElement PExportPresetBuild()
    {
        var pScroll = new ScrollViewer
        {
            Content = pPresetRowPanel,
            Background = Brushes.White,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            FocusVisualStyle = null
        };

        PExportPresetRebuild();
        return pScroll;
    }

    private void PExportPresetRebuild()
    {
        pPresetRowPanel.Children.Clear();
        foreach (string lPresetName in LExportSpecificState.LPresetNames)
        {
            pPresetRowPanel.Children.Add(PExportPresetRowBuild(lPresetName));
        }
    }

    private Border PExportPresetRowBuild(string lPresetName)
    {
        bool pPresetSelected = string.Equals(lPresetName, pPresetNameSelected, StringComparison.OrdinalIgnoreCase);
        bool pPresetEditing = string.Equals(lPresetName, pPresetNameEditing, StringComparison.OrdinalIgnoreCase);
        UIElement pNameElement = pPresetEditing
            ? PExportPresetNameBoxBuild(lPresetName)
            : PExportPresetNameTextBuild(lPresetName);

        var pRowBorder = new Border
        {
            Padding = new Thickness(12, 7, 12, 7),
            Background = pPresetSelected ? new SolidColorBrush(Color.FromRgb(0xEE, 0xF4, 0xFB)) : Brushes.White,
            BorderBrush = PLineBrush,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Cursor = Cursors.Hand,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Child = pNameElement
        };
        pRowBorder.MouseLeftButtonDown += (_, pEvent) =>
        {
            pPresetNameSelected = lPresetName;
            PExportPresetApply();
            if (pEvent.ClickCount >= 2)
            {
                pPresetNameEditing = lPresetName;
                PExportPresetRebuild();
            }

            pEvent.Handled = true;
        };

        return pRowBorder;
    }

    private TextBlock PExportPresetNameTextBuild(string lPresetName) => new()
    {
        Text = lPresetName,
        FontSize = 12,
        Foreground = PTextBrush,
        Padding = new Thickness(2, 0, 2, 1),
        VerticalAlignment = VerticalAlignment.Center
    };

    private TextBox PExportPresetNameBoxBuild(string lPresetName)
    {
        var pNameBox = new TextBox
        {
            Text = lPresetName,
            FontSize = 12,
            Foreground = PTextBrush,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0, 0, 0, 1),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x4A, 0x90, 0xD9)),
            Padding = new Thickness(2, 0, 2, 1),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center,
            FocusVisualStyle = null
        };
        pNameBox.Loaded += (_, _) =>
        {
            pNameBox.Focus();
            pNameBox.SelectAll();
        };
        pNameBox.LostFocus += (_, _) => PExportPresetNameCommit(lPresetName, pNameBox.Text);
        pNameBox.KeyDown += (_, pEvent) =>
        {
            if (pEvent.Key == Key.Return)
            {
                PExportPresetNameCommit(lPresetName, pNameBox.Text);
                pEvent.Handled = true;
            }
            else if (pEvent.Key == Key.Escape)
            {
                pPresetNameEditing = null;
                PExportPresetRebuild();
                pEvent.Handled = true;
            }
        };
        return pNameBox;
    }

    private UIElement PExportActionBuild()
    {
        var pGrid = new Grid { Margin = new Thickness(10, 4, 10, 0) };
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var pLeftPanel = new StackPanel { Orientation = Orientation.Horizontal };
        pLeftPanel.Children.Add(PExportButtonBuild(PExportPlusIconPath, "Add a new preset", PExportPresetAdd));
        pLeftPanel.Children.Add(PExportButtonBuild(PExportMinusIconPath, "Delete the selected preset", PExportPresetDelete));
        Grid.SetColumn(pLeftPanel, 0);
        pGrid.Children.Add(pLeftPanel);

        var pRightPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        pRightPanel.Children.Add(PExportButtonBuild(PExportSettingIconPath, "Settings", PExportDialogShow));
        pRightPanel.Children.Add(PExportButtonBuild(PExportExportIconPath, "Export", null));
        pRightPanel.Children.Add(PExportButtonBuild(PExportImportIconPath, "Import", null));
        Grid.SetColumn(pRightPanel, 2);
        pGrid.Children.Add(pRightPanel);
        return pGrid;
    }

    private Button PExportButtonBuild(string pIconPath, string pTooltip, RoutedEventHandler? pClick)
    {
        bool pEnabled = pClick is not null;
        var pButton = new Button
        {
            Content = new Image
            {
                Width = 14,
                Height = 14,
                Source = PIcon.PIconRead(pIconPath, pEnabled ? PTextBrush : PMutedBrush),
                Stretch = Stretch.Uniform
            },
            ToolTip = pTooltip,
            Width = 28,
            Height = 26,
            Margin = new Thickness(0, 0, 2, 0),
            Style = PExportButtonStyleRead(),
            IsEnabled = pEnabled
        };
        if (pClick is not null)
        {
            pButton.Click += pClick;
        }

        return pButton;
    }

    private static Style PExportButtonStyleRead()
    {
        pExportButtonStyle ??= PExportButtonStyleCreate();
        return pExportButtonStyle;
    }

    private static Style PExportButtonStyleCreate()
    {
        var pStyle = new Style(typeof(Button));
        pStyle.Setters.Add(new Setter(FrameworkElement.FocusVisualStyleProperty, null));
        pStyle.Setters.Add(new Setter(FrameworkElement.CursorProperty, Cursors.Hand));
        pStyle.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
        pStyle.Setters.Add(new Setter(Control.ForegroundProperty, PTextBrush));
        pStyle.Setters.Add(new Setter(Control.BorderBrushProperty, Brushes.Transparent));
        pStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
        pStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(0)));
        pStyle.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Center));
        pStyle.Setters.Add(new Setter(Control.VerticalContentAlignmentProperty, VerticalAlignment.Center));
        pStyle.Setters.Add(new Setter(Control.TemplateProperty, PExportButtonTemplateBuild()));
        return pStyle;
    }

    private static ControlTemplate PExportButtonTemplateBuild()
    {
        var pTemplate = new ControlTemplate(typeof(Button));
        var pBorder = new FrameworkElementFactory(typeof(Border));
        pBorder.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
        pBorder.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Control.PaddingProperty));

        var pContent = new FrameworkElementFactory(typeof(ContentPresenter));
        pContent.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        pContent.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        pBorder.AppendChild(pContent);
        pTemplate.VisualTree = pBorder;

        var pHover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
        pHover.Setters.Add(new Setter(Control.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0xF1, 0xF5, 0xF9))));
        pTemplate.Triggers.Add(pHover);

        var pDisabled = new Trigger { Property = UIElement.IsEnabledProperty, Value = false };
        pDisabled.Setters.Add(new Setter(Control.ForegroundProperty, PMutedBrush));
        pTemplate.Triggers.Add(pDisabled);
        return pTemplate;
    }

    private void PExportPresetApply()
    {
        // Only a real user pick may pull settings out of the shared preset library.
        // Programmatic writes must not, or one tab's save silently overwrites another's.
        if (pExportPresetBusy || pPresetNameSelected is not string lPresetName)
        {
            return;
        }

        if (LExportSpecificState.LPresetTryLoad(lPresetName, lExportSpecificState))
        {
            PExportSummaryUpdate();
        }
    }

    private void PExportPresetAdd(object sender, RoutedEventArgs e)
    {
        string lPresetName = PExportPresetNameCreate();
        lExportSpecificState.PresetName = lPresetName;
        LExportSpecificState.LPresetSave(lPresetName, lExportSpecificState);
        pExportPresetBusy = true;
        pPresetNameSelected = lPresetName;
        pExportPresetBusy = false;
        PExportSummaryUpdate();
    }

    private void PExportPresetDelete(object sender, RoutedEventArgs e)
    {
        if (pPresetNameSelected is not string lPresetName)
        {
            return;
        }

        if (!LExportSpecificState.LPresetDelete(lPresetName))
        {
            return;
        }

        string? lNextPresetName = LExportSpecificState.LPresetFirstName;
        pExportPresetBusy = true;
        if (lNextPresetName is not null && LExportSpecificState.LPresetTryLoad(lNextPresetName, lExportSpecificState))
        {
            pPresetNameSelected = lNextPresetName;
        }
        else
        {
            lExportSpecificState.PresetName = string.Empty;
            pPresetNameSelected = null;
        }

        pExportPresetBusy = false;
        PExportSummaryUpdate();
    }

    private void PExportPresetNameCommit(string lOldPresetName, string lNewPresetName)
    {
        if (!string.Equals(pPresetNameEditing, lOldPresetName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        pPresetNameEditing = null;
        string lName = lNewPresetName.Trim();
        if (string.IsNullOrWhiteSpace(lName) || string.Equals(lOldPresetName, lName, StringComparison.OrdinalIgnoreCase))
        {
            PExportPresetRebuild();
            return;
        }

        if (LExportSpecificState.LPresetNames.Any(lExisting => string.Equals(lExisting, lName, StringComparison.OrdinalIgnoreCase)))
        {
            PExportPresetRebuild();
            return;
        }

        bool lCurrentPresetRename = string.Equals(pPresetNameSelected, lOldPresetName, StringComparison.OrdinalIgnoreCase);
        var lPresetState = new LExportSpecificState();
        if (lCurrentPresetRename)
        {
            lPresetState.LPresetCopy(lExportSpecificState);
        }
        else if (!LExportSpecificState.LPresetTryLoad(lOldPresetName, lPresetState))
        {
            PExportPresetRebuild();
            return;
        }

        lPresetState.PresetName = lName;
        LExportSpecificState.LPresetSave(lName, lPresetState);
        LExportSpecificState.LPresetDelete(lOldPresetName);
        if (lCurrentPresetRename)
        {
            lExportSpecificState.PresetName = lName;
            pPresetNameSelected = lName;
            PExportSummaryUpdate();
        }
        else
        {
            PExportPresetRebuild();
        }
    }

    private void PExportDialogShow(object sender, RoutedEventArgs e)
    {
        var pButton = (Button)sender;
        var psEncoder = new PSEncoder(lExportSpecificState, PExportSummaryUpdate)
        {
            Owner = Window.GetWindow(pButton)
        };

        if (psEncoder.ShowDialog() == true)
        {
            PExportSummaryUpdate();
        }
    }

    private static string PExportPresetNameCreate()
    {
        const string pBaseName = "New Preset";
        if (!LExportSpecificState.LPresetNames.Any(lName => string.Equals(lName, pBaseName, StringComparison.OrdinalIgnoreCase)))
        {
            return pBaseName;
        }

        for (int lIndex = 2; ; lIndex++)
        {
            string lCandidate = $"{pBaseName} {lIndex}";
            if (!LExportSpecificState.LPresetNames.Any(lName => string.Equals(lName, lCandidate, StringComparison.OrdinalIgnoreCase)))
            {
                return lCandidate;
            }
        }
    }
}
