using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIShell.PAssets;
using Cadroue.UIShell.PMainWindow;

namespace Cadroue.UIShell.PPanels;

public sealed class PProcessing : PPanel
{
    private static readonly FontFamily pProcessingFontFamily = new("Segoe UI");
    private static readonly Brush pProcessingSelectBrush = new SolidColorBrush(Color.FromRgb(0xEE, 0xF4, 0xFB));
    private static readonly Brush pProcessingIconBrush = new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x3D));
    private static readonly Brush pProcessingTextBrush = new SolidColorBrush(Color.FromRgb(0x11, 0x18, 0x27));
    private static readonly Brush pProcessingActiveBrush = new SolidColorBrush(Color.FromRgb(0x2C, 0x6C, 0xCE));

    private const string PProcessingUpIcon = "/PAssets/PPanels/PProcessingUp.svg";
    private const string PProcessingDownIcon = "/PAssets/PPanels/PProcessingDown.svg";
    private const string PProcessingSkipIcon = "/PAssets/PPanels/PProcessingSkip.svg";
    private const string PProcessingSkipStep = "No Processing";

    public const double PProcessingStripWidth = 48;

    public event Action<string?>? PProcessingStepChange;
    public event Action<string>? PProcessingStepOpen;
    public event Action<bool>? PProcessingMinimizeChange;
    public event Action? PProcessingOrderChange;

    private readonly StackPanel pProcessingRowPanel;
    private readonly UIElement pProcessingFullBody;
    private readonly UIElement pProcessingStripBody;
    private readonly UIElement pProcessingActionBar;
    private readonly Border pProcessingSkipRow;
    private bool pProcessingSkipActive;
    private string? pProcessingStepCurrent;
    private bool pProcessingMinimized;
    private int? pProcessingIndexDragging;
    private Point? pProcessingDragOrigin;
    private bool pProcessingDragActive;
    private Border? pProcessingRowDragging;
    private bool pProcessingOrdered;
    private readonly HashSet<string> pProcessingActiveSteps = new(StringComparer.Ordinal);

    public void PProcessingOrderedSet(bool pOrderedRequest)
    {
        pProcessingOrdered = pOrderedRequest;
        pProcessingActionBar.Visibility = pOrderedRequest ? Visibility.Visible : Visibility.Collapsed;
    }

    public void PProcessingActiveSet(string pStepName, bool pActive)
    {
        if (pActive)
        {
            pProcessingActiveSteps.Add(pStepName);
        }
        else
        {
            pProcessingActiveSteps.Remove(pStepName);
        }

        foreach (UIElement pRow in pProcessingRowPanel.Children)
        {
            if (pRow is not Border { Tag: string pRowName, Child: StackPanel pRowContent } || pRowName != pStepName)
            {
                continue;
            }

            PProcessingRowApply(pRowContent, pActive);

            return;
        }
    }

    private static void PProcessingRowApply(StackPanel pRowContent, bool pActive)
    {
        Brush pTextBrush = pActive ? pProcessingActiveBrush : pProcessingTextBrush;
        Brush pIconBrush = pActive ? pProcessingActiveBrush : pProcessingIconBrush;
        foreach (UIElement pPiece in pRowContent.Children)
        {
            switch (pPiece)
            {
                case Image { Tag: string pIconPath } pIcon:
                    pIcon.Source = PIcon.PIconRead(pIconPath, pIconBrush);
                    break;
                case TextBlock { Tag: "Label" } pText:
                    pText.Foreground = pTextBrush;
                    pText.FontWeight = pActive ? FontWeights.SemiBold : FontWeights.Normal;
                    break;
            }
        }
    }

    private void PProcessingNumbersUpdate()
    {
        if (!pProcessingOrdered)
        {
            return;
        }

        for (int pIndex = 0; pIndex < pProcessingRowPanel.Children.Count; pIndex++)
        {
            if (pProcessingRowPanel.Children[pIndex] is Border { Child: StackPanel pRowContent }
                && pRowContent.Children.Count > 0
                && pRowContent.Children[0] is Border { Child: TextBlock pNumber })
            {
                pNumber.Text = (pIndex + 1).ToString();
            }
        }
    }

    public IReadOnlyList<string> PProcessingStepsRead()
    {
        var pStepNames = new List<string>();
        foreach (UIElement pRow in pProcessingRowPanel.Children)
        {
            if (pRow is Border { Tag: string pRowName })
            {
                pStepNames.Add(pRowName);
            }
        }

        return pStepNames;
    }

    public PProcessing() : base("")
    {
        UIElement pHeader = PProcessingHeaderBuild();

        pProcessingRowPanel = new StackPanel();
        pProcessingRowPanel.PreviewMouseMove += PProcessingMoveHandle;
        pProcessingRowPanel.MouseLeftButtonUp += PProcessingUpHandle;
        pProcessingRowPanel.LostMouseCapture += PProcessingLostHandle;

        var pScroll = new ScrollViewer
        {
            Content = pProcessingRowPanel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };

        pProcessingActionBar = PProcessingActionBuild();
        pProcessingActionBar.Visibility = pProcessingOrdered ? Visibility.Visible : Visibility.Collapsed;
        pProcessingSkipRow = PProcessingSkipBuild();

        var pRoot = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(pHeader, Dock.Top);
        pRoot.Children.Add(pHeader);
        DockPanel.SetDock(pProcessingActionBar, Dock.Bottom);
        pRoot.Children.Add(pProcessingActionBar);
        DockPanel.SetDock(pProcessingSkipRow, Dock.Bottom);
        pRoot.Children.Add(pProcessingSkipRow);
        pRoot.Children.Add(pScroll);

        pProcessingFullBody = pRoot;
        pProcessingStripBody = PProcessingStripBuild();
        pProcessingStripBody.Visibility = Visibility.Collapsed;

        var pBodyHost = new Grid();
        pBodyHost.Children.Add(pProcessingFullBody);
        pBodyHost.Children.Add(pProcessingStripBody);

        FocusVisualStyle = null;
        Content = PPanelBorderBuild(pBodyHost);
    }

    public void PProcessingSkipSet(bool pProcessingSkipApplied)
    {
        pProcessingSkipActive = pProcessingSkipApplied;
        if (pProcessingSkipRow.Child is StackPanel pProcessingSkipContent)
        {
            PProcessingRowApply(pProcessingSkipContent, pProcessingSkipApplied);
        }

        pProcessingRowPanel.Opacity = pProcessingSkipApplied ? 0.4 : 1;
        pProcessingActionBar.IsEnabled = !pProcessingSkipApplied;
        pProcessingActionBar.Opacity = pProcessingSkipApplied ? 0.4 : 1;
    }

    private Border PProcessingSkipBuild()
    {
        var pRowContent = new StackPanel { Orientation = Orientation.Horizontal };
        pRowContent.Children.Add(new Image
        {
            Width = 14,
            Height = 14,
            Source = PIcon.PIconRead(PProcessingSkipIcon, pProcessingIconBrush),
            Stretch = Stretch.Uniform,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0),
            Tag = PProcessingSkipIcon
        });
        pRowContent.Children.Add(new TextBlock
        {
            Text = LLocalization.LLocalizationTextRead("Processing.Skip.Label"),
            FontSize = 12,
            FontFamily = pProcessingFontFamily,
            Foreground = pProcessingTextBrush,
            VerticalAlignment = VerticalAlignment.Center,
            Tag = "Label"
        });

        var pRowBorder = new Border
        {
            Padding = new Thickness(12, 9, 12, 9),
            Background = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7)),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Cursor = Cursors.Hand,
            ToolTip = LLocalization.LLocalizationTextRead("Processing.Skip.Tooltip"),
            Child = pRowContent
        };
        pRowBorder.MouseLeftButtonUp += (_, _) =>
        {
            pProcessingStepCurrent = PProcessingSkipStep;
            PProcessingSelectApply();
            PProcessingStepChange?.Invoke(PProcessingSkipStep);
            PProcessingStepOpen?.Invoke(PProcessingSkipStep);
        };
        return pRowBorder;
    }

    public bool PProcessingMinimizedCheck() => pProcessingMinimized;

    public void PProcessingMinimizeSet(bool pProcessingMinimizeRequest)
    {
        if (pProcessingMinimized == pProcessingMinimizeRequest)
        {
            return;
        }

        pProcessingMinimized = pProcessingMinimizeRequest;
        pProcessingFullBody.Visibility = pProcessingMinimized ? Visibility.Collapsed : Visibility.Visible;
        pProcessingStripBody.Visibility = pProcessingMinimized ? Visibility.Visible : Visibility.Collapsed;
        PProcessingMinimizeChange?.Invoke(pProcessingMinimized);
    }

    private UIElement PProcessingHeaderBuild()
    {
        var pTitleLabel = new TextBlock
        {
            Text = LLocalization.LLocalizationTextRead("Processing.Header.Title"),
            FontSize = 12,
            FontFamily = pProcessingFontFamily,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(0x26, 0x36, 0x4A)),
            VerticalAlignment = VerticalAlignment.Center
        };

        Button pMinimizeButton = PProcessingButtonBuild(
            "/PAssets/PPanels/PListMinimize.svg", LLocalization.LLocalizationTextRead("Processing.Hide.Tooltip"), () => PProcessingMinimizeSet(true));
        pMinimizeButton.HorizontalAlignment = HorizontalAlignment.Right;

        var pHeaderGrid = new Grid();
        pHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(pMinimizeButton, 1);
        pHeaderGrid.Children.Add(pTitleLabel);
        pHeaderGrid.Children.Add(pMinimizeButton);

        return new Border
        {
            Padding = new Thickness(12, 5, 6, 5),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7)),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Background = Brushes.White,
            Child = pHeaderGrid
        };
    }

    private UIElement PProcessingStripBuild()
    {
        Button pMaximizeButton = PProcessingButtonBuild(
            "/PAssets/PPanels/PListMaximize.svg", LLocalization.LLocalizationTextRead("Processing.Show.Tooltip"), () => PProcessingMinimizeSet(false));
        pMaximizeButton.Margin = new Thickness(0, 6, 0, 0);
        pMaximizeButton.HorizontalAlignment = HorizontalAlignment.Center;

        var pStrip = new StackPanel { Background = Brushes.White };
        pStrip.Children.Add(pMaximizeButton);
        return pStrip;
    }

    private UIElement PProcessingActionBuild()
    {
        Button pUpButton = PProcessingButtonBuild(
            PProcessingUpIcon, LLocalization.LLocalizationTextRead("Processing.MoveUp.Tooltip"), () => PProcessingStepMove(-1));
        pUpButton.Margin = new Thickness(0, 0, 2, 0);
        Button pDownButton = PProcessingButtonBuild(
            PProcessingDownIcon, LLocalization.LLocalizationTextRead("Processing.MoveDown.Tooltip"), () => PProcessingStepMove(1));

        var pLeftPanel = new StackPanel { Orientation = Orientation.Horizontal };
        pLeftPanel.Children.Add(pUpButton);
        pLeftPanel.Children.Add(pDownButton);

        var pActionGrid = new Grid { Margin = new Thickness(10, 4, 10, 6) };
        pActionGrid.Children.Add(pLeftPanel);

        return new Border
        {
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7)),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Background = Brushes.White,
            Child = pActionGrid
        };
    }

    private void PProcessingStepMove(int pStepDelta)
    {
        if (pProcessingStepCurrent is null)
        {
            return;
        }

        int pStepIndex = -1;
        for (int pIndex = 0; pIndex < pProcessingRowPanel.Children.Count; pIndex++)
        {
            if (pProcessingRowPanel.Children[pIndex] is Border { Tag: string pRowName } && pRowName == pProcessingStepCurrent)
            {
                pStepIndex = pIndex;
                break;
            }
        }

        if (pStepIndex < 0)
        {
            return;
        }

        int pTargetIndex = pStepIndex + pStepDelta;
        if (pTargetIndex < 0 || pTargetIndex >= pProcessingRowPanel.Children.Count)
        {
            return;
        }

        UIElement pStepRow = pProcessingRowPanel.Children[pStepIndex];
        pProcessingRowPanel.Children.RemoveAt(pStepIndex);
        pProcessingRowPanel.Children.Insert(pTargetIndex, pStepRow);
        PProcessingNumbersUpdate();
        PProcessingOrderChange?.Invoke();
    }

    private static Button PProcessingButtonBuild(string pIconPath, string pTooltip, Action pClick)
    {
        var pButton = new Button
        {
            Content = new Image
            {
                Width = 14,
                Height = 14,
                Source = PIcon.PIconRead(pIconPath, pProcessingIconBrush),
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

    public void PProcessingStepAdd(string pStepName, string pStepIconPath, string pStepLabelKey)
    {
        pProcessingRowPanel.Children.Add(PProcessingRowBuild(pStepName, pStepIconPath, pStepLabelKey));
        PProcessingNumbersUpdate();
    }

    private static Border PProcessingBadgeBuild()
    {
        var pNumber = new TextBlock
        {
            FontSize = 10,
            FontFamily = pProcessingFontFamily,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(0x26, 0x36, 0x4A)),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        return new Border
        {
            Width = 18,
            Height = 18,
            CornerRadius = new CornerRadius(9),
            Background = new SolidColorBrush(Color.FromRgb(0xE8, 0xEE, 0xF6)),
            Margin = new Thickness(0, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Child = pNumber
        };
    }

    private Border PProcessingRowBuild(string pStepName, string pStepIconPath, string pStepLabelKey)
    {
        var pRowContent = new StackPanel { Orientation = Orientation.Horizontal };
        if (pProcessingOrdered)
        {
            pRowContent.Children.Add(PProcessingBadgeBuild());
        }

        pRowContent.Children.Add(new Image
        {
            Width = 14,
            Height = 14,
            Source = PIcon.PIconRead(pStepIconPath, pProcessingIconBrush),
            Stretch = Stretch.Uniform,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0),
            Tag = pStepIconPath
        });
        pRowContent.Children.Add(new TextBlock
        {
            Text = LLocalization.LLocalizationTextRead(pStepLabelKey),
            FontSize = 12,
            FontFamily = pProcessingFontFamily,
            Foreground = pProcessingTextBrush,
            VerticalAlignment = VerticalAlignment.Center,
            Tag = "Label"
        });

        var pRowBorder = new Border
        {
            Padding = new Thickness(12, 7, 12, 7),
            Background = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7)),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Cursor = Cursors.Hand,
            Child = pRowContent,
            Tag = pStepName
        };
        PProcessingRowApply(pRowContent, pProcessingActiveSteps.Contains(pStepName));
        pRowBorder.MouseLeftButtonDown += (_, pRowEvent) =>
        {
            pProcessingStepCurrent = pStepName;
            PProcessingSelectApply();
            PProcessingStepChange?.Invoke(pStepName);

            pProcessingRowDragging = pRowBorder;
            pProcessingIndexDragging = pProcessingRowPanel.Children.IndexOf(pRowBorder);
            pProcessingDragOrigin = pRowEvent.GetPosition(pProcessingRowPanel);
            pProcessingDragActive = false;

            PProcessingStepOpen?.Invoke(pStepName);
            pRowEvent.Handled = true;
        };
        return pRowBorder;
    }

    private void PProcessingMoveHandle(object pSender, MouseEventArgs pEvent)
    {
        if (!pProcessingOrdered)
        {
            return;
        }

        if (pProcessingRowDragging is not { } pDragRow
            || pProcessingIndexDragging is not int pDragIndex
            || pProcessingDragOrigin is not Point pStart
            || pEvent.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        Point pCurrent = pEvent.GetPosition(pProcessingRowPanel);
        if (!pProcessingDragActive
            && Math.Abs(pCurrent.X - pStart.X) < SystemParameters.MinimumHorizontalDragDistance
            && Math.Abs(pCurrent.Y - pStart.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        pProcessingDragActive = true;
        pDragRow.Opacity = 0.72;

        int pTargetIndex = PProcessingIndexResolve(pCurrent);
        if (pTargetIndex != pDragIndex)
        {
            pProcessingRowPanel.Children.Remove(pDragRow);
            pTargetIndex = Math.Clamp(pTargetIndex, 0, pProcessingRowPanel.Children.Count);
            pProcessingRowPanel.Children.Insert(pTargetIndex, pDragRow);
            pProcessingIndexDragging = pTargetIndex;
            PProcessingNumbersUpdate();
        }
    }

    private void PProcessingUpHandle(object pSender, MouseButtonEventArgs pEvent)
    {
        bool pReordered = pProcessingDragActive;
        if (pProcessingRowDragging is { } pDragRow)
        {
            pDragRow.Opacity = 1;
        }

        PProcessingDragClear();
        if (pReordered)
        {
            PProcessingOrderChange?.Invoke();
        }
    }

    private void PProcessingLostHandle(object pSender, MouseEventArgs pEvent)
    {
        if (pProcessingRowDragging is { } pDragRow)
        {
            pDragRow.Opacity = 1;
        }

        PProcessingDragClear();
    }

    private void PProcessingDragClear()
    {
        pProcessingIndexDragging = null;
        pProcessingDragOrigin = null;
        pProcessingDragActive = false;
        pProcessingRowDragging = null;
    }

    private int PProcessingIndexResolve(Point pPoint)
    {
        for (int pIndex = 0; pIndex < pProcessingRowPanel.Children.Count; pIndex++)
        {
            if (pProcessingRowPanel.Children[pIndex] is not Border pRow)
            {
                continue;
            }

            Point pTopLeft = pRow.TranslatePoint(new Point(0, 0), pProcessingRowPanel);
            if (pPoint.Y < pTopLeft.Y + (pRow.ActualHeight / 2))
            {
                return pIndex;
            }
        }

        return Math.Max(0, pProcessingRowPanel.Children.Count - 1);
    }

    private void PProcessingSelectApply()
    {
        foreach (UIElement pRow in pProcessingRowPanel.Children)
        {
            if (pRow is Border { Tag: string pRowName } pRowBorder)
            {
                pRowBorder.Background = pRowName == pProcessingStepCurrent
                    ? pProcessingSelectBrush
                    : Brushes.White;
            }
        }

        pProcessingSkipRow.Background = pProcessingStepCurrent == PProcessingSkipStep
            ? pProcessingSelectBrush
            : Brushes.White;
    }

}
