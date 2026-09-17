using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.Core;
using Cadroue.Application;
using Cadroue.UIVeneer.PFlow;

namespace Cadroue.UIVeneer.PPanel;

public sealed partial class PSection
{
    private Border PSectionRowBuild(int pSectionIndex, LPiece pSectionEntry, bool pSectionSelected)
    {
        int capturedIndex = pSectionIndex;

        Border? pRowBorderHost = null;

        var pBadgeText = new TextBlock
        {
            Text = (pSectionIndex + 1).ToString(),
            FontSize = PSectionNameSize,
            FontFamily = pSectionFontFamily,
            FontWeight = FontWeights.SemiBold,
            Foreground = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = TextAlignment.Center
        };

        var pColorDot = new Border
        {
            MinWidth = PSectionBadgeSize,
            Height = PSectionBadgeSize,
            CornerRadius = new CornerRadius(PSectionBadgeSize / 2),
            Background = PSectionPalette.PSectionBadgeRead(pSectionEntry.LPieceColorIndex),
            Padding = new Thickness(PSectionBadgePadding, 0, PSectionBadgePadding, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0),
            Cursor = Cursors.Hand,
            ToolTip = LLocalization.LLocalizationTextRead("Section.Toggle.Tooltip"),
            Child = pBadgeText
        };
        pColorDot.MouseLeftButtonDown += (_, pEvent) =>
        {
            pSectionRowPanel.ReleaseMouseCapture();
            PSectionDragClear();
            PSectionEditCommit();
            if (LSection.LSectionEditable && pRowBorderHost is { } pToggleRow)
            {
                pFlowAttached?.PFlowSectionToggle(pSectionRowPanel.Children.IndexOf(pToggleRow));
            }

            pEvent.Handled = true;
        };

        void PSectionSeekHandle(bool pSeekEnd, MouseButtonEventArgs pEvent)
        {
            if (pEvent.ClickCount < 2)
            {
                return;
            }

            pSectionRowPanel.ReleaseMouseCapture();
            PSectionDragClear();
            if (pRowBorderHost is not { } pSeekRow)
            {
                return;
            }

            int pSeekIndex = pSectionRowPanel.Children.IndexOf(pSeekRow);
            if (pSeekIndex < 0)
            {
                return;
            }

            PSectionEditCommit();
            pFlowAttached?.PFlowSectionSeek(pSeekIndex, pSeekEnd);
            pEvent.Handled = true;
        }

        UIElement pNameHost;
        if (pSectionIndex == LSection.LSectionEditIndex)
        {
            pNameHost = PSectionEditorBuild(pSectionEntry);
        }
        else
        {
            TextBlock pNameText = PSectionTextBuild(pSectionIndex, pSectionEntry);
            pNameText.MouseLeftButtonDown += (_, pEvent) =>
            {
                if (pEvent.ClickCount < 2 || !LSection.LSectionEditable)
                {
                    return;
                }

                pSectionRowPanel.ReleaseMouseCapture();
                PSectionDragClear();
                if (pRowBorderHost is not { } pRenameRow)
                {
                    return;
                }

                int pRenameIndex = pSectionRowPanel.Children.IndexOf(pRenameRow);
                if (pRenameIndex < 0)
                {
                    return;
                }

                pFlowAttached?.PFlowSectionSelect(pRenameIndex);
                LSection.LSectionEditSet(pRenameIndex);
                PSectionRebuild();
                pEvent.Handled = true;
            };
            pNameHost = pNameText;
        }

        var pBeginLabel = PSectionTimeBuild(PSectionTimeFormat(pSectionEntry.LPieceOrigin));
        pBeginLabel.Margin = new Thickness(8, 0, 0, 0);
        pBeginLabel.MouseLeftButtonDown += (_, pEvent) => PSectionSeekHandle(false, pEvent);

        var pArrowLabel = PSectionTimeBuild(" → ");
        var pEndLabel = PSectionTimeBuild(PSectionTimeFormat(pSectionEntry.LPieceEnd));
        pEndLabel.MouseLeftButtonDown += (_, pEvent) => PSectionSeekHandle(true, pEvent);

        var pSpanLabel = PSectionTimeBuild($"  ({PSectionTimeFormat(PSectionSpanRead(pSectionEntry))})");

        var pTimeLabel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };
        pTimeLabel.Children.Add(pBeginLabel);
        pTimeLabel.Children.Add(pArrowLabel);
        pTimeLabel.Children.Add(pEndLabel);
        pTimeLabel.Children.Add(pSpanLabel);

        var pRowContent = new Grid
        {
            Opacity = pSectionEntry.LPieceHidden ? PSectionDisabledOpacity : 1
        };
        pRowContent.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pRowContent.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pRowContent.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(pColorDot, 0);
        Grid.SetColumn(pNameHost, 1);
        Grid.SetColumn(pTimeLabel, 2);
        pRowContent.Children.Add(pColorDot);
        pRowContent.Children.Add(pNameHost);
        pRowContent.Children.Add(pTimeLabel);

        var pRowBorder = new Border
        {
            Padding = new Thickness(12, 7, 12, 7),
            Background = pSectionSelected
                ? new SolidColorBrush(Color.FromRgb(0xEE, 0xF4, 0xFB))
                : Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7)),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Cursor = Cursors.Hand,
            Child = pRowContent,
            Tag = pBadgeText
        };
        pRowBorderHost = pRowBorder;
        pRowBorder.PreviewMouseLeftButtonDown += (_, pEvent) =>
        {
            if (pEvent.ClickCount >= 2)
            {
                return;
            }

            pSectionRowDragging = pRowBorder;
            LSection.LSectionDragSet(pSectionRowPanel.Children.IndexOf(pRowBorder), false);
            pSectionDragOrigin = pEvent.GetPosition(pSectionRowPanel);
            pSectionGrabOffset = pEvent.GetPosition(pRowBorder);
            pSectionRowPanel.CaptureMouse();
        };
        pRowBorder.MouseLeftButtonDown += (_, pEvent) => PSectionSeekHandle(false, pEvent);
        return pRowBorder;
    }
}
