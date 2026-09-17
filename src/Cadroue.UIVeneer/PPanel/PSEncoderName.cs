using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIVeneer.PHouse;
using Cadroue.Application;

using static Cadroue.UIVeneer.PSCasement.PSField;
using static Cadroue.UIVeneer.PSCasement.PSCombo;
using static Cadroue.UIVeneer.PSCasement.PSInline;
using static Cadroue.UIVeneer.PSCasement.PSPlate;

namespace Cadroue.UIVeneer.PPanel;

internal sealed partial class PSEncoder
{
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
        pPanel.Children.Add(
            PSNameTokenBuild(
                LLocalization.LLocalizationTextRead("Encoder.Field.Output.Prefix"),
                "{Prefix}"));
        pPanel.Children.Add(
            PSNameTokenBuild(
                LLocalization.LLocalizationTextRead("Encoder.Field.Output.OriginalName"),
                "{OriginalName}"));
        pPanel.Children.Add(
            PSNameTokenBuild(
                LLocalization.LLocalizationTextRead("Encoder.Field.Output.SectionNumber"),
                "{SectionNumber}"));
        pPanel.Children.Add(
            PSNameTokenBuild(
                LLocalization.LLocalizationTextRead("Encoder.Field.Output.SectionName"),
                "{SectionName}"));
        pPanel.Children.Add(
            PSNameTokenBuild(
                LLocalization.LLocalizationTextRead("Encoder.Field.Output.SectionStart"),
                "{SectionStart}"));
        pPanel.Children.Add(
            PSNameTokenBuild(
                LLocalization.LLocalizationTextRead("Encoder.Field.Output.SectionEnd"),
                "{SectionEnd}"));
        pPanel.Children.Add(
            PSNameTokenBuild(
                LLocalization.LLocalizationTextRead("Encoder.Field.Output.SectionDuration"),
                "{SectionDuration}"));
        pPanel.Children.Add(
            PSNameTokenBuild(
                LLocalization.LLocalizationTextRead("Encoder.Field.Output.Date"),
                "{Date}"));
        pPanel.Children.Add(
            PSNameTokenBuild(
                LLocalization.LLocalizationTextRead("Encoder.Field.Output.Time"),
                "{Time}"));
        pPanel.Children.Add(
            PSNameTokenBuild(
                LLocalization.LLocalizationTextRead("Encoder.Field.Output.Suffix"),
                "{Suffix}"));
        pPanel.Children.Add(
            PSNameOperatorBuild(
                LLocalization.LLocalizationTextRead("Encoder.Field.Output.Backspace"),
                "Backspace"));
        pPanel.Children.Add(
            PSNameOperatorBuild(
                LLocalization.LLocalizationTextRead("Encoder.Field.Output.Delete"),
                "Delete"));
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

        string PSNameOperatorFormat() => $"{{{pKind}:{PSNameCountRead(psNumberBox.Text)}}}";

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
                PSNameTokenInsert(PSNameOperatorFormat());
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
            PSNameDragRun(pBorder, PSNameOperatorFormat(), psOperatorGrabOffset);
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

    private void PSNameTokenInsert(string pToken)
    {
        psNameBox.PTokenInsert(pToken);
    }
}
