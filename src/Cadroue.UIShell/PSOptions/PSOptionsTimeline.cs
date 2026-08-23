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
    private const double PSSpectrumSwatchSize = 20;
    private const double PSSpectrumSwatchGap = 5;
    private const double PSSpectrumNameWidth = 92;

    private const string PSSpectrumLoadIcon = "/PAssets/PPanels/PSSpectrumLoad.svg";
    private const string PSSpectrumSaveIcon = "/PAssets/PPanels/PSSpectrumSave.svg";
    private const string PSSpectrumRemoveIcon = "/PAssets/PPanels/PSSpectrumRemove.svg";
    private const double PSSpectrumRemoveSize = 26;

    private readonly Dictionary<string, Border> psSpectrumRows = new(StringComparer.Ordinal);
    private readonly StackPanel psSpectrumList = new() { HorizontalAlignment = HorizontalAlignment.Left };

    private string psSpectrumName = PSectionPalette.PSectionPaletteDefault;

    private UIElement PSTimelineBuild()
    {
        var pPanel = new StackPanel();
        pPanel.Children.Add(PSPlateBuild(LLocalization.LLocalizationTextRead("Options.Timeline.Order"),
            PSFieldBuild(LLocalization.LLocalizationTextRead("Options.Timeline.StripOrder"), psOptionsOrderMode)));
        pPanel.Children.Add(PSPlateBuild(LLocalization.LLocalizationTextRead("Options.Timeline.KeyframeSpacing"),
            PSOptionsFieldBuild(LLocalization.LLocalizationTextRead("Options.Timeline.Minimum"), psKeyframeSlider, " px")));
        pPanel.Children.Add(PSPlateBuild(LLocalization.LLocalizationTextRead("Options.Timeline.KeyframeDelay"),
            PSOptionsFieldBuild(LLocalization.LLocalizationTextRead("Options.Timeline.KeyframeDelayLabel"), psKeyframeDelaySlider, " ms"),
            PSNoticeBuild(LLocalization.LLocalizationTextRead("Options.Timeline.KeyframeDelayNotice"))));
        pPanel.Children.Add(PSPlateBuild(LLocalization.LLocalizationTextRead("Options.Timeline.Overlapping"),
            PSFieldBuild(LLocalization.LLocalizationTextRead("Options.Timeline.OverlappingSections"), psOptionsOverlapBox)));
        pPanel.Children.Add(PSPlateBuild(LLocalization.LLocalizationTextRead("Options.Timeline.Waveform"),
            PSFieldBuild(LLocalization.LLocalizationTextRead("Options.Timeline.ShowWaveforms"), psWaveformBox),
            PSNoticeBuild(LLocalization.LLocalizationTextRead("Options.Timeline.WaveformNotice"))));
        pPanel.Children.Add(PSPlateBuild(LLocalization.LLocalizationTextRead("Options.Timeline.SectionPalette"),
            PSSpectrumFieldBuild()));
        return pPanel;
    }

    private UIElement PSSpectrumFieldBuild()
    {
        psSpectrumName = lsOptionsDraft.LPreferenceSectionPalette;
        PSectionPalette.PSectionPaletteLoad();
        PSSpectrumListBuild();

        var pSide = new StackPanel { VerticalAlignment = VerticalAlignment.Top, Width = PSFieldLabelWidth };
        pSide.Children.Add(new TextBlock
        {
            Text = LLocalization.LLocalizationTextRead("Options.Timeline.Palette"),
            Foreground = PSFieldMuted,
            Margin = new Thickness(0, 8, 0, 8)
        });

        var pButtons = new StackPanel { Orientation = Orientation.Horizontal };
        pButtons.Children.Add(PSSpectrumButtonBuild(PSSpectrumLoadIcon, LLocalization.LLocalizationTextRead("Options.Timeline.LoadTooltip"), PSSpectrumLoad));
        pButtons.Children.Add(PSSpectrumButtonBuild(PSSpectrumSaveIcon, LLocalization.LLocalizationTextRead("Options.Timeline.SaveTooltip"), PSSpectrumSave));
        pSide.Children.Add(pButtons);

        var pGrid = new Grid { Margin = new Thickness(0, 0, 0, 9) };
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(PSFieldLabelWidth) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.Children.Add(pSide);
        Grid.SetColumn(psSpectrumList, 1);
        pGrid.Children.Add(psSpectrumList);
        return pGrid;
    }

    private void PSSpectrumListBuild()
    {
        psSpectrumRows.Clear();
        psSpectrumList.Children.Clear();
        foreach (string pName in PSectionPalette.PSectionPaletteNames)
        {
            Border pRow = PSSpectrumRowBuild(pName);
            psSpectrumRows[pName] = pRow;
            psSpectrumList.Children.Add(PSSpectrumLineBuild(pName, pRow));
        }

        if (!psSpectrumRows.ContainsKey(psSpectrumName))
        {
            psSpectrumName = PSectionPalette.PSectionPaletteDefault;
        }

        PSSpectrumActiveApply();
    }

    private static Button PSSpectrumButtonBuild(string pIconPath, string pTip, Action pClick)
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

    private void PSSpectrumLoad()
    {
        var pDialog = new OpenFileDialog
        {
            Title = LLocalization.LLocalizationTextRead("Options.Timeline.LoadTitle"),
            Filter = LLocalization.LLocalizationTextRead("Options.Timeline.JsonFilter"),
            InitialDirectory = Cadroue.Infrastructure.LDepot.LDepotPaletteRead()
        };
        if (pDialog.ShowDialog() != true)
        {
            return;
        }

        string? pLoadedName = PSectionPalette.PSectionPaletteImport(pDialog.FileName);
        if (pLoadedName is null)
        {
            MessageBox.Show(this, LLocalization.LLocalizationTextRead("Options.Timeline.InvalidPalette"), LLocalization.LLocalizationTextRead("Options.Timeline.LoadTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        psSpectrumName = pLoadedName;
        PSSpectrumListBuild();
    }

    private void PSSpectrumSave()
    {
        var pDialog = new SaveFileDialog
        {
            Title = LLocalization.LLocalizationTextRead("Options.Timeline.SaveTitle"),
            Filter = LLocalization.LLocalizationTextRead("Options.Timeline.JsonFilter"),
            FileName = $"{psSpectrumName}.json",
            InitialDirectory = Cadroue.Infrastructure.LDepot.LDepotPaletteRead()
        };
        if (pDialog.ShowDialog() == true)
        {
            PSectionPalette.PSectionPaletteSave(psSpectrumName, pDialog.FileName);
        }
    }

    private Border PSSpectrumRowBuild(string pName)
    {
        var pLabel = new TextBlock
        {
            Text = pName,
            Width = PSSpectrumNameWidth,
            Foreground = PSFieldText,
            VerticalAlignment = VerticalAlignment.Center
        };

        var pSwatches = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        foreach (Brush pBadge in PSectionPalette.PSectionBadgesRead(pName))
        {
            pSwatches.Children.Add(new Border
            {
                Width = PSSpectrumSwatchSize,
                Height = PSSpectrumSwatchSize,
                CornerRadius = new CornerRadius(5),
                Background = pBadge,
                Margin = new Thickness(0, 0, PSSpectrumSwatchGap, 0)
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
        pRow.MouseLeftButtonDown += (_, _) => PSSpectrumSelect(pName);
        pRow.MouseEnter += (_, _) => PSSpectrumHoverApply(pName, true);
        pRow.MouseLeave += (_, _) => PSSpectrumHoverApply(pName, false);
        return pRow;
    }

    private UIElement PSSpectrumLineBuild(string pName, Border pRow)
    {
        var pLine = new StackPanel { Orientation = Orientation.Horizontal };
        pLine.Children.Add(pRow);
        if (!PSectionPalette.PSectionFixedCheck(pName))
        {
            pLine.Children.Add(PSSpectrumRemoveBuild(pName));
        }

        return pLine;
    }

    private Button PSSpectrumRemoveBuild(string pName)
    {
        var pButton = new Button
        {
            Style = PButton.PButtonIconCreate(),
            Width = PSSpectrumRemoveSize,
            MinWidth = PSSpectrumRemoveSize,
            Height = PSSpectrumRemoveSize,
            Margin = new Thickness(8, 0, 0, 6),
            VerticalAlignment = VerticalAlignment.Center,
            ToolTip = LLocalization.LLocalizationFormat("Options.Timeline.RemoveTooltip", pName),
            Content = new Image
            {
                Source = PIcon.PIconRead(PSSpectrumRemoveIcon, PSFieldText),
                Width = 12,
                Height = 12,
                Stretch = Stretch.Uniform
            }
        };
        pButton.Click += (_, pEvent) => { pEvent.Handled = true; PSSpectrumRemove(pName); };
        return pButton;
    }

    private void PSSpectrumRemove(string pName)
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

        if (psSpectrumName == pName)
        {
            psSpectrumName = PSectionPalette.PSectionPaletteDefault;
        }

        PSSpectrumListBuild();
    }

    private void PSSpectrumSelect(string pName)
    {
        psSpectrumName = pName;
        PSSpectrumActiveApply();
    }

    private void PSSpectrumHoverApply(string pName, bool pHovered)
    {
        if (pName == psSpectrumName || !psSpectrumRows.TryGetValue(pName, out Border? pRow))
        {
            return;
        }

        pRow.BorderBrush = pHovered ? PSSpectrumAccent : PSFieldLine;
        pRow.Background = pHovered ? PSSpectrumSoft : Brushes.White;
    }

    private void PSSpectrumActiveApply()
    {
        foreach ((string pName, Border pRow) in psSpectrumRows)
        {
            bool pChosen = pName == psSpectrumName;
            pRow.BorderBrush = pChosen ? PSSpectrumAccent : PSFieldLine;
            pRow.BorderThickness = new Thickness(pChosen ? 2 : 1);
            pRow.Background = pChosen ? PSSpectrumSoft : Brushes.White;
            pRow.Padding = new Thickness(pChosen ? 11 : 12, pChosen ? 7 : 8, pChosen ? 13 : 14, pChosen ? 7 : 8);
        }
    }

    private static readonly Brush PSSpectrumAccent = PSSpectrumBrushCreate(0x4C, 0x86, 0xF7);
    private static readonly Brush PSSpectrumSoft = PSSpectrumBrushCreate(0xF7, 0xF9, 0xFC);

    private static Brush PSSpectrumBrushCreate(byte pRed, byte pGreen, byte pBlue)
    {
        var pBrush = new SolidColorBrush(Color.FromRgb(pRed, pGreen, pBlue));
        pBrush.Freeze();
        return pBrush;
    }
}
