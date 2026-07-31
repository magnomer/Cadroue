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
            Foreground = PSEncoderMutedBrush,
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
        psOutputContainerCombo.SelectionChanged += (_, _) => PSOutputContainerHandle();
        pPanel.Children.Add(PSFieldBuild(LLocalization.LLocalizationTextRead("Encoder.Field.Output.Container"), psOutputContainerCombo));
        pPanel.Children.Add(PSFieldBuild(LLocalization.LLocalizationTextRead("Encoder.Field.Output.Extension"), psOutputExtensionCombo));
        PSLocationFolderUpdate();
        return PSPlateBuild(pPanel);
    }

    private static LLocalizationChoice[] PSOutputExtensionRead(string pContainer)
    {
        IReadOnlyList<string> pExtensions = LPreset.LPresetExtensionsRead(pContainer);
        return pExtensions.Count == 0
            ? [new LLocalizationChoice(string.Empty, "Encoder.Location.Source")]
            : pExtensions.Select(pExtension => new LLocalizationChoice(pExtension)).ToArray();
    }

    private void PSOutputExtensionUpdate()
    {
        string pCurrent = PSComboTextRead(psOutputExtensionCombo);
        LLocalizationChoice[] pChoices = PSOutputExtensionRead(PSComboTextRead(psOutputContainerCombo));
        psOutputExtensionCombo.ItemsSource = pChoices;
        psOutputExtensionCombo.SelectedItem = pChoices.FirstOrDefault(
            pChoice => string.Equals(pChoice.LLocalizationChoiceToken, pCurrent, StringComparison.Ordinal))
            ?? pChoices.FirstOrDefault();
    }

    private void PSOutputContainerHandle()
    {
        PSOutputExtensionUpdate();
        PSCodecContainerHandle();
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
        pPanel.Children.Add(PSNameOperatorBuild(LLocalization.LLocalizationTextRead("Encoder.Field.Output.Backspace"), "Backspace"));
        pPanel.Children.Add(PSNameOperatorBuild(LLocalization.LLocalizationTextRead("Encoder.Field.Output.Delete"), "Delete"));
        Grid.SetColumn(pPanel, 1);
        pGrid.Children.Add(pPanel);
        return pGrid;
    }

    private UIElement PSNameTokenBuild(string pLabel, string pToken)
    {
        var pText = new TextBlock
        {
            Text = pLabel,
            Foreground = PSEncoderTextBrush,
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
        Point psNameGrabOffset = default;
        bool pDragStarted = false;
        pBorder.PreviewMouseLeftButtonDown += (_, e) =>
        {
            pDragStart = e.GetPosition(null);
            psNameGrabOffset = e.GetPosition(pBorder);
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
            PSNameDragRun(pBorder, pToken, psNameGrabOffset);
            pBorder.Background = Brushes.White;
        };
        return pBorder;
    }

    private UIElement PSNameOperatorBuild(string pWord, string pKind)
    {
        var pWordText = new TextBlock
        {
            Text = pWord,
            Foreground = PSEncoderTextBrush,
            VerticalAlignment = VerticalAlignment.Center
        };
        var psNumberBox = new TextBox
        {
            Text = "1",
            Width = 26,
            MaxLength = 3,
            TextAlignment = TextAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            BorderThickness = new Thickness(0),
            Background = Brushes.Transparent,
            Margin = new Thickness(4, 0, 0, 0)
        };
        psNumberBox.PreviewTextInput += (_, e) => e.Handled = !PSNameDigitsCheck(e.Text);
        psNumberBox.LostFocus += (_, _) => psNumberBox.Text = PSNameCountRead(psNumberBox.Text).ToString();

        var pContent = new StackPanel { Orientation = Orientation.Horizontal };
        pContent.Children.Add(pWordText);
        pContent.Children.Add(psNumberBox);

        var pBorder = new Border
        {
            MinHeight = PSFieldChipHeight,
            BorderBrush = PLineBrush,
            BorderThickness = new Thickness(1),
            Background = Brushes.White,
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10, 0, 6, 0),
            Margin = new Thickness(0, 0, 6, 6),
            Cursor = Cursors.Hand,
            Child = pContent
        };

        string PSNameOperatorToken() => $"{{{pKind}:{PSNameCountRead(psNumberBox.Text)}}}";

        Point? pDragStart = null;
        Point psOperatorGrabOffset = default;
        bool pDragStarted = false;
        pBorder.PreviewMouseLeftButtonDown += (_, e) =>
        {
            if (psNumberBox.IsMouseOver)
            {
                pDragStart = null;
                return;
            }

            pDragStart = e.GetPosition(null);
            psOperatorGrabOffset = e.GetPosition(pBorder);
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

            if (!psNumberBox.IsMouseOver)
            {
                PSNameTokenInsert(PSNameOperatorToken());
            }
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
            PSNameDragRun(pBorder, PSNameOperatorToken(), psOperatorGrabOffset);
            pBorder.Background = Brushes.White;
        };
        return pBorder;
    }

    private static bool PSNameDigitsCheck(string pText)
    {
        foreach (char pChar in pText)
        {
            if (!char.IsDigit(pChar))
            {
                return false;
            }
        }

        return true;
    }

    private static int PSNameCountRead(string pText)
    {
        return int.TryParse(pText, out int pCount) && pCount > 0 ? pCount : 1;
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

            pDragAdorner.PSNameDragMove(pRoot.PointFromScreen(new Point(pCursor.PSNameX, pCursor.PSNameY)));
            pEvent.UseDefaultCursors = true;
            pEvent.Handled = true;
        }

        pChip.GiveFeedback += PSNameDragFeedbackHandle;
        try
        {
            var pData = new DataObject();
            pData.SetData(PToken.PTokenDataKind, pToken);
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
        private readonly VisualCollection psNameVisuals;
        private readonly System.Windows.Shapes.Rectangle psNameImage;
        private readonly Point psNameGrabOffset;
        private Point psNameDragPoint;

        internal PSNameDragAdorner(UIElement pAdornedElement, FrameworkElement pChip, Point pGrabOffset)
            : base(pAdornedElement)
        {
            psNameGrabOffset = pGrabOffset;
            psNameImage = new System.Windows.Shapes.Rectangle
            {
                Width = pChip.ActualWidth,
                Height = pChip.ActualHeight,
                Fill = new VisualBrush(pChip),
                Opacity = 0.85,
                IsHitTestVisible = false
            };
            psNameVisuals = new VisualCollection(this) { psNameImage };

            IsHitTestVisible = false;
        }

        internal void PSNameDragMove(Point pPoint)
        {
            psNameDragPoint = pPoint;
            InvalidateArrange();
            (Parent as AdornerLayer)?.Update(AdornedElement);
        }

        protected override int VisualChildrenCount => psNameVisuals.Count;

        protected override Visual GetVisualChild(int pIndex) => psNameVisuals[pIndex];

        protected override Size MeasureOverride(Size pConstraint)
        {
            psNameImage.Measure(pConstraint);
            return psNameImage.DesiredSize;
        }

        protected override Size ArrangeOverride(Size pFinalSize)
        {
            psNameImage.Arrange(new Rect(
                psNameDragPoint.X - psNameGrabOffset.X,
                psNameDragPoint.Y - psNameGrabOffset.Y,
                psNameImage.Width,
                psNameImage.Height));
            return pFinalSize;
        }
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct PSNamePoint
    {
        public int PSNameX;
        public int PSNameY;
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

    private static bool PSLocationNamedCheck(string psLocation) =>
        string.Equals(psLocation, "Subfolder", StringComparison.Ordinal)
        || string.Equals(psLocation, "Sibling", StringComparison.Ordinal);

    private string PSLocationStatusRead()
    {
        string psLocation = PSComboTextRead(psLocationCombo);
        if (string.Equals(psLocation, "Subfolder", StringComparison.Ordinal))
        {
            return LLocalization.LLocalizationTextRead("Encoder.Location.SubfolderStatus");
        }

        if (string.Equals(psLocation, "Sibling", StringComparison.Ordinal))
        {
            return LLocalization.LLocalizationTextRead("Encoder.Location.SiblingStatus");
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

        psLocationFolderRow.Visibility = PSLocationNamedCheck(PSComboTextRead(psLocationCombo))
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

        if (PSComboTextRead(psLocationCombo) == "Sibling")
        {
            psEncoderFolderPath = null;
            psLocationStatus.Text = LLocalization.LLocalizationTextRead("Encoder.Location.SiblingStatus");
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
