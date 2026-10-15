using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIVeneer.PBench;
using Cadroue.UIVeneer.PHouse;
using Cadroue.Core;
using Cadroue.Application;
using Cadroue.Infrastructure;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed partial class PSOptions
{
    private static readonly LLocalizationChoice[] PSOptionsOrderItems =
    {
        new("MapFirst", "Options.Timeline.MapTop"),
        new("ViewfinderFirst", "Options.Timeline.ViewfinderTop")
    };

    private readonly Border psOptionsOrderMode;
    private readonly Slider psKeyframeSlider;
    private readonly Slider psKeyframeDelaySlider;
    private readonly CheckBox psOptionsOverlapBox;
    private readonly CheckBox psWaveformBox;

    private const double PSSpectrumSwatchSize = 20;
    private const double PSSpectrumSwatchGap = 5;
    private const double PSSpectrumNameWidth = 92;

    private const string PSSpectrumLoadIcon = "/PAsset/PPanel/PSSpectrumLoad.svg";
    private const string PSSpectrumSaveIcon = "/PAsset/PPanel/PSSpectrumSave.svg";
    private const string PSSpectrumRemoveIcon = "/PAsset/PPanel/PSSpectrumRemove.svg";
    private const double PSSpectrumRemoveSize = 26;

    private readonly Dictionary<string, Border> psSpectrumRows = new(StringComparer.Ordinal);
    private readonly StackPanel psSpectrumList = new() { HorizontalAlignment = HorizontalAlignment.Left };

    private UIElement PSTimelineBuild()
    {
        var pPanel = new StackPanel();
        pPanel.Children.Add(PSPlate.PSPlateBuild(QLocalizationCatalog.QLocalizationTextRead("Options.Timeline.Order"),
            PSField.PSFieldBuild(QLocalizationCatalog.QLocalizationTextRead("Options.Timeline.StripOrder"),
                psOptionsOrderMode)));
        pPanel.Children.Add(
            PSPlate.PSPlateBuild(QLocalizationCatalog.QLocalizationTextRead("Options.Timeline.KeyframeSpacing"),
            PSOptionsFieldBuild(
                QLocalizationCatalog.QLocalizationTextRead("Options.Timeline.Minimum"),
                psKeyframeSlider,
                " px")));
        pPanel.Children.Add(PSPlate.PSPlateBuild(QLocalizationCatalog.QLocalizationTextRead(
            "Options.Timeline.KeyframeDelay"),
            PSOptionsFieldBuild(
                QLocalizationCatalog.QLocalizationTextRead("Options.Timeline.KeyframeDelayLabel"),
                psKeyframeDelaySlider,
                " ms"),
            PSNotice.PSNoticeBuild(QLocalizationCatalog.QLocalizationTextRead(
                "Options.Timeline.KeyframeDelayNotice"))));
        pPanel.Children.Add(PSPlate.PSPlateBuild(QLocalizationCatalog.QLocalizationTextRead(
            "Options.Timeline.Overlapping"),
            PSField.PSFieldBuild(
                QLocalizationCatalog.QLocalizationTextRead("Options.Timeline.OverlappingSections"),
                psOptionsOverlapBox)));
        pPanel.Children.Add(PSPlate.PSPlateBuild(QLocalizationCatalog.QLocalizationTextRead(
            "Options.Timeline.Waveform"),
            PSField.PSFieldBuild(QLocalizationCatalog.QLocalizationTextRead("Options.Timeline.ShowWaveforms"),
                psWaveformBox),
            PSNotice.PSNoticeBuild(QLocalizationCatalog.QLocalizationTextRead("Options.Timeline.WaveformNotice"))));
        pPanel.Children.Add(PSPlate.PSPlateBuild(QLocalizationCatalog.QLocalizationTextRead(
            "Options.Timeline.SectionPalette"),
            PSSpectrumFieldBuild()));
        return pPanel;
    }

    private UIElement PSSpectrumFieldBuild()
    {
        PSSpectrumListBuild();

        var pSide = new StackPanel { VerticalAlignment = VerticalAlignment.Top, Width = PSField.PSFieldLabelWidth };
        pSide.Children.Add(new TextBlock
        {
            Text = QLocalizationCatalog.QLocalizationTextRead("Options.Timeline.Palette"),
            Foreground = PSField.PSFieldMuted,
            Margin = new Thickness(0, 8, 0, 8)
        });

        var pButtons = new StackPanel { Orientation = Orientation.Horizontal };
        pButtons.Children.Add(PSSpectrumButtonBuild(
            PSSpectrumLoadIcon,
            QLocalizationCatalog.QLocalizationTextRead("Options.Timeline.LoadTooltip"),
            PSSpectrumLoad));
        pButtons.Children.Add(PSSpectrumButtonBuild(
            PSSpectrumSaveIcon,
            QLocalizationCatalog.QLocalizationTextRead("Options.Timeline.SaveTooltip"),
            PSSpectrumSave));
        pSide.Children.Add(pButtons);

        var pGrid = new Grid { Margin = new Thickness(0, 0, 0, 9) };
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(PSField.PSFieldLabelWidth) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.Children.Add(pSide);
        Grid.SetColumn(psSpectrumList, 1);
        pGrid.Children.Add(psSpectrumList);
        return pGrid;
    }

    private void PSSpectrumListBuild()
    {
        LSectionPalette.LSectionRegistryLoad();
        psSpectrumRows.Clear();
        psSpectrumList.Children.Clear();
        IReadOnlyList<string> pNames = LSectionPalette.LSectionNamesRead();
        foreach (string pName in pNames)
        {
            Border pRow = PSSpectrumRowBuild(pName);
            psSpectrumRows[pName] = pRow;
            psSpectrumList.Children.Add(PSSpectrumLineBuild(pName, pRow));
        }

        lsSpectrum.QSSpectrumNamesSet(pNames);
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
                Source = QIcon.QIconRead(pIconPath, PSField.PSFieldText),
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
            Title = QLocalizationCatalog.QLocalizationTextRead("Options.Timeline.LoadTitle"),
            Filter = QLocalizationCatalog.QLocalizationTextRead("Options.Timeline.JsonFilter"),
            InitialDirectory = lsSpectrum.QSSpectrumFolderRead()
        };
        if (pDialog.ShowDialog() != true)
        {
            return;
        }

        QSSpectrumResult pResult = lsSpectrum.QSSpectrumImport(pDialog.FileName, out string pTargetPath);
        string? pLoadedName = pResult == QSSpectrumResult.QSSpectrumResultLoaded
            ? LSectionPalette.LSectionNameFind(pTargetPath)
            : null;
        if (pLoadedName is null)
        {
            string pMessageKey = pResult switch
            {
                QSSpectrumResult.QSSpectrumResultReserved => "Options.Timeline.ReservedPalette",
                QSSpectrumResult.QSSpectrumResultFailed => "Options.Timeline.PaletteCopyFailed",
                _ => "Options.Timeline.InvalidPalette"
            };
            PSWarning.PSWarningShow(
                this,
                QLocalizationCatalog.QLocalizationTextRead("Options.Timeline.LoadTitle"),
                QLocalizationCatalog.QLocalizationTextRead(pMessageKey));
            return;
        }

        PSSpectrumListBuild();
        lsSpectrum.QSSpectrumSelect(pLoadedName);
    }

    private void PSSpectrumSave()
    {
        var pDialog = new SaveFileDialog
        {
            Title = QLocalizationCatalog.QLocalizationTextRead("Options.Timeline.SaveTitle"),
            Filter = QLocalizationCatalog.QLocalizationTextRead("Options.Timeline.JsonFilter"),
            FileName = $"{lsSpectrum.QSSpectrumName}.json",
            InitialDirectory = lsSpectrum.QSSpectrumFolderRead()
        };
        if (pDialog.ShowDialog() == true)
        {
            string pName = lsSpectrum.QSSpectrumName;
            lsSpectrum.QSSpectrumSave(pDialog.FileName, pName, LSectionPalette.LSectionHexRead(pName));
        }
    }

    private Border PSSpectrumRowBuild(string pName)
    {
        var pLabel = new TextBlock
        {
            Text = pName,
            Width = PSSpectrumNameWidth,
            Foreground = PSField.PSFieldText,
            VerticalAlignment = VerticalAlignment.Center
        };

        var pSwatches = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };
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
            BorderBrush = PSField.PSFieldLine,
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
        if (!QSSpectrum.QSSpectrumFixedCheck(pName))
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
            ToolTip = QLocalizationCatalog.QLocalizationFormat("Options.Timeline.RemoveTooltip", pName),
            Content = new Image
            {
                Source = QIcon.QIconRead(PSSpectrumRemoveIcon, PSField.PSFieldText),
                Width = 18,
                Height = 18,
                Stretch = Stretch.Uniform
            }
        };
        pButton.Click += (_, pEvent) => { pEvent.Handled = true; PSSpectrumRemove(pName); };
        return pButton;
    }

    private void PSSpectrumRemove(string pName)
    {
        bool pConfirmed = PSAlert.PSAlertConfirm(
            this,
            QLocalizationCatalog.QLocalizationTextRead("Options.Timeline.RemoveTitle"),
            LSectionPalette.LSectionNativeCheck(pName)
                ? QLocalizationCatalog.QLocalizationFormat("Options.Timeline.RemoveBuiltInConfirm", pName)
                : QLocalizationCatalog.QLocalizationFormat("Options.Timeline.RemoveWorkspaceConfirm", pName),
            QLocalizationCatalog.QLocalizationTextRead("Terms.Remove"));
        if (!pConfirmed
            || !lsSpectrum.QSSpectrumRemove(
                pName,
                LSectionPalette.LSectionNativeCheck(pName),
                LSectionPalette.LSectionPathFind(pName)))
        {
            return;
        }

        PSSpectrumListBuild();
    }

    private void PSSpectrumSelect(string pName) => lsSpectrum.QSSpectrumSelect(pName);

    private void PSSpectrumHoverApply(string pName, bool pHovered)
    {
        if (pName == lsSpectrum.QSSpectrumName || !psSpectrumRows.TryGetValue(pName, out Border? pRow))
        {
            return;
        }

        pRow.BorderBrush = pHovered ? PSSpectrumAccent : PSField.PSFieldLine;
        pRow.Background = pHovered ? PSSpectrumSoft : Brushes.White;
    }

    private void PSSpectrumActiveApply()
    {
        foreach ((string pName, Border pRow) in psSpectrumRows)
        {
            bool pChosen = pName == lsSpectrum.QSSpectrumName;
            pRow.BorderBrush = pChosen ? PSSpectrumAccent : PSField.PSFieldLine;
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
