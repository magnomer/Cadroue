using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIShell.PMainWindow;
using Cadroue.Application;

using static Cadroue.UIShell.PSShared.PSField;

namespace Cadroue.UIShell.PPanels;

internal sealed partial class PSEncoder
{
    private UIElement PSOutputPlateBuild()
    {
        var pPanel = new StackPanel();
        psLocationStatus = new TextBlock
        {
            Foreground = PSEncoderMutedBrush,
            Margin = new Thickness(12, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        PSNameBoxPrepare();
        psLocationFolderLabel = PSFieldLabelBuild(string.Empty);
        psLocationFolderRow = PSFieldLabelledBuild(psLocationFolderLabel, psLocationFolderBox);
        pPanel.Children.Add(PSFieldBuild(LLocalization.LLocalizationTextRead("Encoder.Field.Output.Name"), psNameBox));
        pPanel.Children.Add(PSNameRowBuild());
        pPanel.Children.Add(PSLocationFieldBuild(psLocationStatus));
        pPanel.Children.Add(psLocationFolderRow);
        psOutputContainerCombo.SelectionChanged += (_, _) => PSOutputContainerHandle();
        pPanel.Children.Add(PSFieldBuild(LLocalization.LLocalizationTextRead("Encoder.Field.Output.Container"), psOutputContainerCombo));
        pPanel.Children.Add(PSFieldBuild(LLocalization.LLocalizationTextRead("Encoder.Field.Output.Extension"), psOutputExtensionCombo));

        psOutputSuffixRow = PSFieldBuild(LLocalization.LLocalizationTextRead("Encoder.Field.Output.Suffix"), psOutputSuffixBox);
        psOutputCollisionCombo.SelectionChanged += (_, _) => PSOutputSuffixUpdate();
        pPanel.Children.Add(PSFieldBuild(LLocalization.LLocalizationTextRead("Encoder.Field.Output.Collision"), psOutputCollisionCombo));
        pPanel.Children.Add(psOutputSuffixRow);
        PSOutputSuffixUpdate();

        PSLocationModeUpdate();
        return PSPlateBuild(pPanel);
    }

    private static bool PSOutputSuffixCheck(string pPolicy) =>
        string.Equals(pPolicy, "Rename output", StringComparison.Ordinal)
        || string.Equals(pPolicy, "Rename existing", StringComparison.Ordinal);

    private void PSOutputSuffixUpdate()
    {
        if (psOutputSuffixRow is null)
        {
            return;
        }

        psOutputSuffixRow.Visibility = PSOutputSuffixCheck(PSComboTextRead(psOutputCollisionCombo))
            ? Visibility.Visible
            : Visibility.Collapsed;
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
        PSAudioContainerHandle();
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

    private UIElement PSLocationFieldBuild(TextBlock psLocationStatus)
    {
        var pGrid = new Grid { Margin = new Thickness(0, 0, 0, 9) };
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.Children.Add(PSFieldLabelBuild(LLocalization.LLocalizationTextRead("Encoder.Field.Output.Location")));

        var pValueGrid = new Grid();
        pValueGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pValueGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        Grid.SetColumn(psLocationMode, 0);
        Grid.SetColumn(psLocationStatus, 1);
        pValueGrid.Children.Add(psLocationMode);
        pValueGrid.Children.Add(psLocationStatus);

        Grid.SetColumn(pValueGrid, 1);
        pGrid.Children.Add(pValueGrid);
        return pGrid;
    }

    private static UIElement PSFieldLabelledBuild(TextBlock pLabel, TextBox pBox)
    {
        var pGrid = new Grid { Margin = new Thickness(0, 0, 0, 9), MinHeight = PSFieldControlHeight };
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.Children.Add(pLabel);
        pBox.MinHeight = PSFieldControlHeight;
        Grid.SetColumn(pBox, 1);
        pGrid.Children.Add(pBox);
        return pGrid;
    }

    private void PSLocationModeUpdate()
    {
        string pMode = PSModeTextRead(psLocationMode);
        bool pFolder = !string.Equals(pMode, "Same as source", StringComparison.Ordinal);

        if (psLocationFolderRow is not null)
        {
            psLocationFolderRow.Visibility = pFolder ? Visibility.Visible : Visibility.Collapsed;
        }

        if (psLocationFolderLabel is not null)
        {
            psLocationFolderLabel.Text = LLocalization.LLocalizationTextRead(PSLocationFolderKeyRead(pMode));
        }

        if (psLocationStatus is not null)
        {
            psLocationStatus.Text = LLocalization.LLocalizationTextRead(PSLocationStatusKeyRead(pMode));
        }
    }

    private static string PSLocationFolderKeyRead(string pMode) => pMode switch
    {
        "Sibling" => "Encoder.Location.Sibling",
        "Custom location" => "Encoder.Location.Custom",
        _ => "Encoder.Location.Subfolder"
    };

    private static string PSLocationStatusKeyRead(string pMode) => pMode switch
    {
        "Subfolder" => "Encoder.Location.SubfolderStatus",
        "Sibling" => "Encoder.Location.SiblingStatus",
        "Custom location" => "Encoder.Location.CustomStatus",
        _ => "Encoder.Location.Source"
    };
}
