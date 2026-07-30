using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIShell.PMainWindow;

using static Cadroue.UIShell.PSShared.PSField;

namespace Cadroue.UIShell.PPanels;

internal sealed partial class PSEncoder
{
    private UIElement PSOutputPlateBuild()
    {
        var pPanel = new StackPanel();
        var psLocationStatus = new TextBlock
        {
            Text = PSLocationStatusRead(),
            Foreground = PMutedBrush,
            Margin = new Thickness(12, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        PSNameBoxPrepare();
        psLocationFolderRow = PSFieldBuild(LLocalization.LLocalizationTextRead("Encoder.Field.Output.Subfolder"), psLocationFolderBox);
        pPanel.Children.Add(PSFieldBuild(LLocalization.LLocalizationTextRead("Encoder.Field.Output.Name"), psNameBox));
        pPanel.Children.Add(PSNameRowBuild());
        pPanel.Children.Add(PSLocationFieldBuild(psLocationStatus));
        pPanel.Children.Add(psLocationFolderRow);
        pPanel.Children.Add(PSFieldBuild(LLocalization.LLocalizationTextRead("Encoder.Field.Output.Container"), psContainerCombo));
        PSLocationFolderUpdate();
        return PSPlateBuild(pPanel);
    }

    private void PSNameBoxPrepare()
    {
        psNameBox.MinWidth = 320;
        psNameBox.Height = PSFieldControlHeight;
        psNameBox.HorizontalAlignment = HorizontalAlignment.Stretch;
    }

    private UIElement PSNameRowBuild()
    {
        var pGrid = new Grid { Margin = new Thickness(0, 8, 0, 9) };
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.Children.Add(PSFieldLabelBuild(LLocalization.LLocalizationTextRead("Encoder.Field.Output.Elements")));

        var pPanel = new WrapPanel();
        pPanel.Children.Add(PSNameTokenBuild(LLocalization.LLocalizationTextRead("Encoder.Field.Output.Prefix"), "{Prefix}"));
        pPanel.Children.Add(PSNameTokenBuild(LLocalization.LLocalizationTextRead("Encoder.Field.Output.OriginalName"), "{OriginalName}"));
        pPanel.Children.Add(PSNameTokenBuild(LLocalization.LLocalizationTextRead("Encoder.Field.Output.SectionNumber"), "{SectionNumber}"));
        pPanel.Children.Add(PSNameTokenBuild(LLocalization.LLocalizationTextRead("Encoder.Field.Output.SectionName"), "{SectionName}"));
        pPanel.Children.Add(PSNameTokenBuild(LLocalization.LLocalizationTextRead("Encoder.Field.Output.Date"), "{Date}"));
        pPanel.Children.Add(PSNameTokenBuild(LLocalization.LLocalizationTextRead("Encoder.Field.Output.Time"), "{Time}"));
        pPanel.Children.Add(PSNameTokenBuild(LLocalization.LLocalizationTextRead("Encoder.Field.Output.Suffix"), "{Suffix}"));
        Grid.SetColumn(pPanel, 1);
        pGrid.Children.Add(pPanel);
        return pGrid;
    }

    private UIElement PSNameTokenBuild(string pLabel, string pToken)
    {
        var pText = new TextBlock
        {
            Text = pLabel,
            Foreground = PTextBrush,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        var pBorder = new Border
        {
            MinHeight = PSFieldChipHeight,
            BorderBrush = PLineBrush,
            BorderThickness = new Thickness(1),
            Background = Brushes.White,
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10, 0, 10, 0),
            Margin = new Thickness(0, 0, 6, 6),
            Cursor = Cursors.Hand,
            Child = pText
        };

        Point? pDragStart = null;
        Point pDragGrabOffset = default;
        bool pDragStarted = false;
        pBorder.PreviewMouseLeftButtonDown += (_, e) =>
        {
            pDragStart = e.GetPosition(null);
            pDragGrabOffset = e.GetPosition(pBorder);
            pDragStarted = false;
            pBorder.Background = new SolidColorBrush(Color.FromRgb(0xF0, 0xF4, 0xFA));
        };
        pBorder.MouseEnter += (_, _) =>
        {
            if (!pDragStarted)
            {
                pBorder.Background = new SolidColorBrush(Color.FromRgb(0xF8, 0xFA, 0xFC));
            }
        };
        pBorder.MouseLeave += (_, _) => pBorder.Background = Brushes.White;
        pBorder.MouseLeftButtonUp += (_, _) =>
        {
            pBorder.Background = new SolidColorBrush(Color.FromRgb(0xF8, 0xFA, 0xFC));
            if (pDragStarted)
            {
                pDragStarted = false;
                return;
            }

            PSNameTokenInsert(pToken);
        };
        pBorder.PreviewMouseMove += (_, e) =>
        {
            if (e.LeftButton != MouseButtonState.Pressed || pDragStart is null || pDragStarted)
            {
                return;
            }

            Point pCurrent = e.GetPosition(null);
            if (Math.Abs(pCurrent.X - pDragStart.Value.X) < 4 && Math.Abs(pCurrent.Y - pDragStart.Value.Y) < 4)
            {
                return;
            }

            pDragStarted = true;
            PSNameDragRun(pBorder, pToken, pDragGrabOffset);
            pBorder.Background = Brushes.White;
        };
        return pBorder;
    }

    private void PSNameDragRun(FrameworkElement pChip, string pToken, Point pGrabOffset)
    {
        var pRoot = Content as UIElement;
        AdornerLayer? pLayer = pRoot is null ? null : AdornerLayer.GetAdornerLayer(pRoot);
        PSNameDragAdorner? pDragAdorner = null;
        if (pRoot is not null && pLayer is not null)
        {
            pDragAdorner = new PSNameDragAdorner(pRoot, pChip, pGrabOffset);
            pLayer.Add(pDragAdorner);
        }

        void PSNameDragFeedbackHandle(object pSender, GiveFeedbackEventArgs pEvent)
        {
            if (pDragAdorner is null || pRoot is null || !PSNameCursorRead(out PSNamePoint pCursor))
            {
                return;
            }

            pDragAdorner.PSNameDragMove(pRoot.PointFromScreen(new Point(pCursor.X, pCursor.Y)));
            pEvent.UseDefaultCursors = true;
            pEvent.Handled = true;
        }

        pChip.GiveFeedback += PSNameDragFeedbackHandle;
        try
        {
            var pData = new DataObject();
            pData.SetData(PToken.PTokenDataFormat, pToken);
            pData.SetData(DataFormats.Text, pToken);
            _ = DragDrop.DoDragDrop(pChip, pData, DragDropEffects.Copy);
        }
        finally
        {
            pChip.GiveFeedback -= PSNameDragFeedbackHandle;
            if (pDragAdorner is not null)
            {
                pLayer?.Remove(pDragAdorner);
            }
        }
    }

    private sealed class PSNameDragAdorner : Adorner
    {
        private readonly VisualCollection pDragVisuals;
        private readonly System.Windows.Shapes.Rectangle pDragImage;
        private readonly Point pDragGrabOffset;
        private Point pDragPoint;

        internal PSNameDragAdorner(UIElement pAdornedElement, FrameworkElement pChip, Point pGrabOffset)
            : base(pAdornedElement)
        {
            pDragGrabOffset = pGrabOffset;
            pDragImage = new System.Windows.Shapes.Rectangle
            {
                Width = pChip.ActualWidth,
                Height = pChip.ActualHeight,
                Fill = new VisualBrush(pChip),
                Opacity = 0.85,
                IsHitTestVisible = false
            };
            pDragVisuals = new VisualCollection(this) { pDragImage };

            IsHitTestVisible = false;
        }

        internal void PSNameDragMove(Point pPoint)
        {
            pDragPoint = pPoint;
            InvalidateArrange();
            (Parent as AdornerLayer)?.Update(AdornedElement);
        }

        protected override int VisualChildrenCount => pDragVisuals.Count;

        protected override Visual GetVisualChild(int pIndex) => pDragVisuals[pIndex];

        protected override Size MeasureOverride(Size pConstraint)
        {
            pDragImage.Measure(pConstraint);
            return pDragImage.DesiredSize;
        }

        protected override Size ArrangeOverride(Size pFinalSize)
        {
            pDragImage.Arrange(new Rect(
                pDragPoint.X - pDragGrabOffset.X,
                pDragPoint.Y - pDragGrabOffset.Y,
                pDragImage.Width,
                pDragImage.Height));
            return pFinalSize;
        }
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct PSNamePoint
    {
        public int X;
        public int Y;
    }

    [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "GetCursorPos")]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    private static extern bool PSNameCursorRead(out PSNamePoint pointScreen);

    private void PSNameTokenInsert(string pToken)
    {
        psNameBox.PTokenInsert(pToken);
    }

    private UIElement PSLocationFieldBuild(TextBlock psLocationStatus)
    {
        var pGrid = new Grid { Margin = new Thickness(0, 0, 0, 9) };
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.Children.Add(PSFieldLabelBuild(LLocalization.LLocalizationTextRead("Encoder.Field.Output.Location")));

        var pValueGrid = new Grid();
        pValueGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pValueGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        psLocationCombo.SelectionChanged += (_, _) => PSLocationChangeHandle(psLocationCombo, psLocationStatus);
        Grid.SetColumn(psLocationCombo, 0);
        Grid.SetColumn(psLocationStatus, 1);
        pValueGrid.Children.Add(psLocationCombo);
        pValueGrid.Children.Add(psLocationStatus);

        Grid.SetColumn(pValueGrid, 1);
        pGrid.Children.Add(pValueGrid);
        return pGrid;
    }

    private string PSLocationStatusRead()
    {
        if (string.Equals(PSComboTextRead(psLocationCombo), "Subfolder", StringComparison.Ordinal))
        {
            return LLocalization.LLocalizationTextRead("Encoder.Location.SubfolderStatus");
        }

        return string.IsNullOrWhiteSpace(psEncoderFolderPath)
            ? LLocalization.LLocalizationTextRead("Encoder.Location.Source")
            : psEncoderFolderPath;
    }

    private void PSLocationFolderUpdate()
    {
        if (psLocationFolderRow is null)
        {
            return;
        }

        psLocationFolderRow.Visibility = string.Equals(PSComboTextRead(psLocationCombo), "Subfolder", StringComparison.Ordinal)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void PSLocationChangeHandle(ComboBox psLocationCombo, TextBlock psLocationStatus)
    {
        PSLocationFolderUpdate();

        if (PSComboTextRead(psLocationCombo) == "Subfolder")
        {
            psEncoderFolderPath = null;
            psLocationStatus.Text = LLocalization.LLocalizationTextRead("Encoder.Location.SubfolderStatus");
            return;
        }

        if (PSComboTextRead(psLocationCombo) != "Custom location")
        {
            psEncoderFolderPath = null;
            psLocationStatus.Text = LLocalization.LLocalizationTextRead("Encoder.Location.Source");
            return;
        }

        var psFolderDialog = new OpenFolderDialog
        {
            Title = LLocalization.LLocalizationTextRead("Encoder.Location.ChooseFolder"),
            Multiselect = false
        };
        if (!string.IsNullOrWhiteSpace(psEncoderFolderPath))
        {
            psFolderDialog.InitialDirectory = psEncoderFolderPath;
        }

        bool? psFolderResult = psFolderDialog.ShowDialog(this);
        if (psFolderResult == true && !string.IsNullOrWhiteSpace(psFolderDialog.FolderName))
        {
            psEncoderFolderPath = psFolderDialog.FolderName;
            psLocationStatus.Text = psEncoderFolderPath;
            return;
        }

        if (string.IsNullOrWhiteSpace(psEncoderFolderPath))
        {
            psLocationCombo.SelectedIndex = 0;
            psLocationStatus.Text = LLocalization.LLocalizationTextRead("Encoder.Location.Source");
            return;
        }

        psLocationStatus.Text = psEncoderFolderPath;
    }
}
