using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIShell.PAssets;
using Cadroue.UIShell.PFlow;
using Cadroue.UIShell.PMainWindow;

using static Cadroue.UIShell.PSShared.PSField;

namespace Cadroue.UIShell;

internal sealed partial class PSOptions
{
    private const double PSPaletteSwatchSize = 20;
    private const double PSPaletteSwatchGap = 5;
    private const double PSPaletteNameWidth = 92;

    private const string PSPaletteLoadIconPath = "/PAssets/PPanels/PSPaletteLoad.svg";
    private const string PSPaletteSaveIconPath = "/PAssets/PPanels/PSPaletteSave.svg";
    private const string PSPaletteRemoveIconPath = "/PAssets/PPanels/PSPaletteRemove.svg";
    private const double PSPaletteRemoveSize = 26;

    private readonly Dictionary<string, Border> psPaletteRows = new(StringComparer.Ordinal);
    private readonly StackPanel psPaletteList = new() { HorizontalAlignment = HorizontalAlignment.Left };

    private string psPaletteName = PSectionPalette.PSectionPaletteDefaultName;

    private UIElement PSTimelineBuild()
    {
        var pPanel = new StackPanel();
        pPanel.Children.Add(PSPlateBuild(LLocalization.LLocalizationTextRead("Options.Timeline.Order"),
            PSFieldBuild(LLocalization.LLocalizationTextRead("Options.Timeline.StripOrder"), psOrderCombo)));
        pPanel.Children.Add(PSPlateBuild(LLocalization.LLocalizationTextRead("Options.Timeline.KeyframeSpacing"),
            PSOptionsSliderFieldBuild(LLocalization.LLocalizationTextRead("Options.Timeline.Minimum"), psKeyframeSlider, " px")));
        pPanel.Children.Add(PSPlateBuild(LLocalization.LLocalizationTextRead("Options.Timeline.Overlapping"),
            PSFieldBuild(LLocalization.LLocalizationTextRead("Options.Timeline.OverlappingSections"), psOverlapBox)));
        pPanel.Children.Add(PSPlateBuild(LLocalization.LLocalizationTextRead("Options.Timeline.Waveform"),
            PSFieldBuild(LLocalization.LLocalizationTextRead("Options.Timeline.ShowWaveforms"), psWaveformBox),
            PSNoticeBuild(LLocalization.LLocalizationTextRead("Options.Timeline.WaveformNotice"))));
        pPanel.Children.Add(PSPlateBuild(LLocalization.LLocalizationTextRead("Options.Timeline.SectionPalette"),
            PSPaletteFieldBuild()));
        return pPanel;
    }

    private UIElement PSPaletteFieldBuild()
    {
        psPaletteName = lsOptionsDraft.LPreferenceSectionPalette;
        PSectionPalette.PSectionPaletteReload();
        PSPaletteListFill();

        var pSide = new StackPanel { VerticalAlignment = VerticalAlignment.Top, Width = PSFieldLabelWidth };
        pSide.Children.Add(new TextBlock
        {
            Text = LLocalization.LLocalizationTextRead("Options.Timeline.Palette"),
            Foreground = PSFieldMuted,
            Margin = new Thickness(0, 8, 0, 8)
        });

        var pButtons = new StackPanel { Orientation = Orientation.Horizontal };
        pButtons.Children.Add(PSPaletteButtonBuild(PSPaletteLoadIconPath, LLocalization.LLocalizationTextRead("Options.Timeline.LoadTooltip"), PSPaletteLoad));
        pButtons.Children.Add(PSPaletteButtonBuild(PSPaletteSaveIconPath, LLocalization.LLocalizationTextRead("Options.Timeline.SaveTooltip"), PSPaletteSave));
        pSide.Children.Add(pButtons);

        var pGrid = new Grid { Margin = new Thickness(0, 0, 0, 9) };
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(PSFieldLabelWidth) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.Children.Add(pSide);
        Grid.SetColumn(psPaletteList, 1);
        pGrid.Children.Add(psPaletteList);
        return pGrid;
    }

    private void PSPaletteListFill()
    {
        psPaletteRows.Clear();
        psPaletteList.Children.Clear();
        foreach (string pName in PSectionPalette.PSectionPaletteNames)
        {
            Border pRow = PSPaletteRowBuild(pName);
            psPaletteRows[pName] = pRow;
            psPaletteList.Children.Add(PSPaletteLineBuild(pName, pRow));
        }

        if (!psPaletteRows.ContainsKey(psPaletteName))
        {
            psPaletteName = PSectionPalette.PSectionPaletteDefaultName;
        }

        PSPaletteSelectApply();
    }

    private static Button PSPaletteButtonBuild(string pIconPath, string pTip, Action pClick)
    {
        var pButton = new Button
        {
            Style = PButton.PButtonIconCreate(),
            Margin = new Thickness(0, 0, 6, 0),
            ToolTip = pTip,
            Content = new Image
            {
                Source = PIcon.PIconRead(pIconPath, PSFieldText),
                Width = 18,
                Height = 18,
                Stretch = Stretch.Uniform
            }
        };
        pButton.Click += (_, _) => pClick();
        return pButton;
    }

    private void PSPaletteLoad()
    {
        var pDialog = new OpenFileDialog
        {
            Title = LLocalization.LLocalizationTextRead("Options.Timeline.LoadTitle"),
            Filter = LLocalization.LLocalizationTextRead("Options.Timeline.JsonFilter"),
            InitialDirectory = Cadroue.Core.LDepot.LDepotPaletteRead()
        };
        if (pDialog.ShowDialog() != true)
        {
            return;
        }

        string? pLoadedName = PSectionPalette.PSectionPaletteLoad(pDialog.FileName);
        if (pLoadedName is null)
        {
            MessageBox.Show(this, LLocalization.LLocalizationTextRead("Options.Timeline.InvalidPalette"), LLocalization.LLocalizationTextRead("Options.Timeline.LoadTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        psPaletteName = pLoadedName;
        PSPaletteListFill();
    }

    private void PSPaletteSave()
    {
        var pDialog = new SaveFileDialog
        {
            Title = LLocalization.LLocalizationTextRead("Options.Timeline.SaveTitle"),
            Filter = LLocalization.LLocalizationTextRead("Options.Timeline.JsonFilter"),
            FileName = $"{psPaletteName}.json",
            InitialDirectory = Cadroue.Core.LDepot.LDepotPaletteRead()
        };
        if (pDialog.ShowDialog() == true)
        {
            PSectionPalette.PSectionPaletteSave(psPaletteName, pDialog.FileName);
        }
    }

    private Border PSPaletteRowBuild(string pName)
    {
        var pLabel = new TextBlock
        {
            Text = pName,
            Width = PSPaletteNameWidth,
            Foreground = PSFieldText,
            VerticalAlignment = VerticalAlignment.Center
        };

        var pSwatches = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        foreach (Brush pBadge in PSectionPalette.PSectionBadgesRead(pName))
        {
            pSwatches.Children.Add(new Border
            {
                Width = PSPaletteSwatchSize,
                Height = PSPaletteSwatchSize,
                CornerRadius = new CornerRadius(5),
                Background = pBadge,
                Margin = new Thickness(0, 0, PSPaletteSwatchGap, 0)
            });
        }

        var pContent = new StackPanel { Orientation = Orientation.Horizontal };
        pContent.Children.Add(pLabel);
        pContent.Children.Add(pSwatches);

        var pRow = new Border
        {
            Background = Brushes.White,
            BorderBrush = PSFieldLine,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(12, 8, 14, 8),
            Margin = new Thickness(0, 0, 0, 6),
            Cursor = Cursors.Hand,
            Child = pContent
        };
        pRow.MouseLeftButtonDown += (_, _) => PSPaletteSelectSet(pName);
        pRow.MouseEnter += (_, _) => PSPaletteHoverApply(pName, true);
        pRow.MouseLeave += (_, _) => PSPaletteHoverApply(pName, false);
        return pRow;
    }

    private UIElement PSPaletteLineBuild(string pName, Border pRow)
    {
        var pLine = new StackPanel { Orientation = Orientation.Horizontal };
        pLine.Children.Add(pRow);
        if (!PSectionPalette.PSectionFixedCheck(pName))
        {
            pLine.Children.Add(PSPaletteRemoveBuild(pName));
        }

        return pLine;
    }

    private Button PSPaletteRemoveBuild(string pName)
    {
        var pButton = new Button
        {
            Style = PButton.PButtonIconCreate(),
            Width = PSPaletteRemoveSize,
            MinWidth = PSPaletteRemoveSize,
            Height = PSPaletteRemoveSize,
            Margin = new Thickness(8, 0, 0, 6),
            VerticalAlignment = VerticalAlignment.Center,
            ToolTip = LLocalization.LLocalizationFormat("Options.Timeline.RemoveTooltip", pName),
            Content = new Image
            {
                Source = PIcon.PIconRead(PSPaletteRemoveIconPath, PSFieldText),
                Width = 12,
                Height = 12,
                Stretch = Stretch.Uniform
            }
        };
        pButton.Click += (_, pEvent) => { pEvent.Handled = true; PSPaletteRemove(pName); };
        return pButton;
    }

    private void PSPaletteRemove(string pName)
    {
        MessageBoxResult pAnswer = MessageBox.Show(
            this,
            PSectionPalette.PSectionNativeCheck(pName)
                ? LLocalization.LLocalizationFormat("Options.Timeline.RemoveBuiltInConfirm", pName)
                : LLocalization.LLocalizationFormat("Options.Timeline.RemoveWorkspaceConfirm", pName),
            LLocalization.LLocalizationTextRead("Options.Timeline.RemoveTitle"),
            MessageBoxButton.OKCancel,
            MessageBoxImage.Warning);
        if (pAnswer != MessageBoxResult.OK || !PSectionPalette.PSectionPaletteRemove(pName))
        {
            return;
        }

        if (psPaletteName == pName)
        {
            psPaletteName = PSectionPalette.PSectionPaletteDefaultName;
        }

        PSPaletteListFill();
    }

    private void PSPaletteSelectSet(string pName)
    {
        psPaletteName = pName;
        PSPaletteSelectApply();
    }

    private void PSPaletteHoverApply(string pName, bool pHovered)
    {
        if (pName == psPaletteName || !psPaletteRows.TryGetValue(pName, out Border? pRow))
        {
            return;
        }

        pRow.BorderBrush = pHovered ? PSPaletteAccent : PSFieldLine;
        pRow.Background = pHovered ? PSPaletteSoft : Brushes.White;
    }

    private void PSPaletteSelectApply()
    {
        foreach ((string pName, Border pRow) in psPaletteRows)
        {
            bool pChosen = pName == psPaletteName;
            pRow.BorderBrush = pChosen ? PSPaletteAccent : PSFieldLine;
            pRow.BorderThickness = new Thickness(pChosen ? 2 : 1);
            pRow.Background = pChosen ? PSPaletteSoft : Brushes.White;
            pRow.Padding = new Thickness(pChosen ? 11 : 12, pChosen ? 7 : 8, pChosen ? 13 : 14, pChosen ? 7 : 8);
        }
    }

    private static readonly Brush PSPaletteAccent = PSPaletteBrushCreate(0x4C, 0x86, 0xF7);
    private static readonly Brush PSPaletteSoft = PSPaletteBrushCreate(0xF7, 0xF9, 0xFC);

    private static Brush PSPaletteBrushCreate(byte pRed, byte pGreen, byte pBlue)
    {
        var pBrush = new SolidColorBrush(Color.FromRgb(pRed, pGreen, pBlue));
        pBrush.Freeze();
        return pBrush;
    }
}
