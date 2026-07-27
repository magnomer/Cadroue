using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PExport : UserControl
{
    private static readonly Brush PLineBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7));
    private static readonly Brush PSoftBrush = new SolidColorBrush(Color.FromRgb(0xF7, 0xF9, 0xFC));
    private static readonly Brush PTextBrush = new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x3D));
    private static readonly Brush PMutedBrush = new SolidColorBrush(Color.FromRgb(0x62, 0x6F, 0x83));

    private readonly LExportSpecificState lExportSpecificState;
    private readonly TextBlock pSummaryContainer;
    private readonly TextBlock pSummaryMode;
    private readonly TextBlock pSummaryVideo;
    private readonly TextBlock pSummaryAudio;
    private readonly TextBlock pSummaryOutput;
    private readonly ComboBox pPresetCombo;

    /// <summary>
    /// Set while this panel writes to <see cref="pPresetCombo"/> itself. The preset
    /// library is shared by every tab, so an unguarded programmatic write raises
    /// SelectionChanged, reloads the preset over this tab's own settings, and makes
    /// tabs appear to share one export configuration.
    /// </summary>
    private bool pExportPresetBusy;

    public PExport(LExportSpecificState lExportSpecificState)
    {
        this.lExportSpecificState = lExportSpecificState;
        FocusVisualStyle = null;
        pPresetCombo = PExportComboBuild();

        var pPanel = new StackPanel { Margin = new Thickness(12) };
        pPanel.Children.Add(PHeaderBuild());
        pPanel.Children.Add(PCardBuild("Summary",
            PSummaryRowBuild("Container", out pSummaryContainer),
            PSummaryRowBuild("Mode", out pSummaryMode),
            PSummaryRowBuild("Video", out pSummaryVideo),
            PSummaryRowBuild("Audio", out pSummaryAudio),
            PSummaryRowBuild("Output", out pSummaryOutput)));
        pPanel.Children.Add(PCardBuild("Preset",
            PExportTopBuild(),
            PSeparatorBuild(),
            PExportActionBuild()));

        PExportSummaryUpdate();
        Content = PExportFrameBuild(new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            FocusVisualStyle = null,
            Content = pPanel
        });
    }

    private static Border PExportFrameBuild(UIElement pContent) => new()
    {
        Margin = new Thickness(8),
        BorderBrush = PLineBrush,
        BorderThickness = new Thickness(1),
        Background = PSoftBrush,
        CornerRadius = new CornerRadius(10),
        Child = new Border
        {
            Background = PSoftBrush,
            CornerRadius = new CornerRadius(9),
            Child = pContent,
            SnapsToDevicePixels = true
        },
        SnapsToDevicePixels = true
    };

    private void PExportSummaryUpdate()
    {
        pExportPresetBusy = true;
        pPresetCombo.Text = lExportSpecificState.PresetName;
        pExportPresetBusy = false;

        pSummaryContainer.Text = lExportSpecificState.Container;
        pSummaryMode.Text = lExportSpecificState.ExportMode;
        pSummaryVideo.Text = lExportSpecificState.VideoSummary;
        pSummaryAudio.Text = lExportSpecificState.AudioSummary;
        pSummaryOutput.Text = lExportSpecificState.OutputSummary;
    }

    private static UIElement PHeaderBuild() => new TextBlock
    {
        Text = "Export",
        FontSize = 19,
        FontWeight = FontWeights.SemiBold,
        Foreground = PTextBrush,
        Margin = new Thickness(0, 0, 0, 10)
    };

    private static Border PCardBuild(string pTitle, params UIElement[] pChildren)
    {
        var pPanel = new StackPanel();
        pPanel.Children.Add(new TextBlock
        {
            Text = pTitle,
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            Foreground = PTextBrush,
            Margin = new Thickness(0, 0, 0, 10)
        });

        foreach (UIElement pChild in pChildren)
        {
            pPanel.Children.Add(pChild);
        }

        return new Border
        {
            BorderBrush = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Background = Brushes.Transparent,
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 0, 10),
            Child = pPanel
        };
    }

    private static UIElement PSeparatorBuild() => new Border
    {
        Height = 1,
        Background = PLineBrush,
        Margin = new Thickness(0, 12, 0, 12)
    };

    private static UIElement PSummaryRowBuild(string pName, out TextBlock pValueBlock)
    {
        var pGrid = new Grid { Margin = new Thickness(0, 0, 0, 6) };
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(86) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.Children.Add(new TextBlock { Text = pName, Foreground = PMutedBrush, FontSize = 12, VerticalAlignment = VerticalAlignment.Top });

        pValueBlock = new TextBlock { Foreground = PTextBrush, FontSize = 12, TextWrapping = TextWrapping.Wrap };
        Grid.SetColumn(pValueBlock, 1);
        pGrid.Children.Add(pValueBlock);
        return pGrid;
    }
}
