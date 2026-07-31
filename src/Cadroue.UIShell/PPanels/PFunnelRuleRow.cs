using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIShell.PControlBar;
using Cadroue.UIShell.PMainArea;

namespace Cadroue.UIShell.PPanels;

public sealed class PFunnelRuleRow : Border
{
    private static readonly FontFamily pFunnelFontFamily = new("Segoe UI");
    private static readonly Brush pFunnelLineBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7));
    private static readonly Brush pFunnelTitleBrush = new SolidColorBrush(Color.FromRgb(0x26, 0x36, 0x4A));
    private static readonly Brush pFunnelMutedBrush = new SolidColorBrush(Color.FromRgb(0x8A, 0x93, 0x9E));
    private static readonly Brush pFunnelAccentBrush = new SolidColorBrush(Color.FromRgb(0x2C, 0x6C, 0xCE));

    private const double PFunnelFieldHeight = 26;

    private readonly TextBox pFunnelStartField;
    private readonly TextBox pFunnelEndField;
    private readonly Button pFunnelJoinButton;
    private readonly Button pFunnelTargetButton;
    private readonly TextBlock pFunnelTargetLabel;
    private readonly Func<IReadOnlyList<LCourierOption>> pFunnelOptionsRead;

    private bool pFunnelAndMode = true;
    private Guid pFunnelTargetId;
    private int pFunnelTargetPending = -1;

    public event Action? PFunnelRowChange;
    public event Action<PFunnelRuleRow>? PFunnelRowRemove;

    public PFunnelRuleRow(Func<IReadOnlyList<LCourierOption>> pOptionsRead)
    {
        pFunnelOptionsRead = pOptionsRead;

        pFunnelStartField = PFunnelFieldBuild();
        pFunnelEndField = PFunnelFieldBuild();
        pFunnelJoinButton = PFunnelJoinBuild();
        pFunnelTargetLabel = new TextBlock
        {
            FontSize = 12,
            FontFamily = pFunnelFontFamily,
            Foreground = pFunnelTitleBrush,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        pFunnelTargetButton = PFunnelTargetBuild();

        var pBody = new StackPanel();
        pBody.Children.Add(PFunnelLabelRowBuild(
            LLocalization.LLocalizationTextRead("Inspector.Funnel.StartsWith"), pFunnelStartField));
        pBody.Children.Add(pFunnelJoinButton);
        pBody.Children.Add(PFunnelLabelRowBuild(
            LLocalization.LLocalizationTextRead("Inspector.Funnel.EndsWith"), pFunnelEndField));
        pBody.Children.Add(PFunnelRelayRowBuild());

        Margin = new Thickness(0, 0, 0, 10);
        Padding = new Thickness(10);
        Background = Brushes.White;
        BorderBrush = pFunnelLineBrush;
        BorderThickness = new Thickness(1);
        CornerRadius = new CornerRadius(8);
        SnapsToDevicePixels = true;
        Child = pBody;

        PFunnelTargetLabelUpdate();
    }

    public string PFunnelRowStart => pFunnelStartField.Text.Trim();

    public string PFunnelRowEnd => pFunnelEndField.Text.Trim();

    public bool PFunnelRowAnd => pFunnelAndMode;

    public Guid PFunnelRowTargetId => pFunnelTargetId;

    public int PFunnelRowTargetPending => pFunnelTargetPending;

    public void PFunnelRowSeed(string pStart, string pEnd, bool pAndMode, int pTargetIndex)
    {
        pFunnelStartField.Text = pStart;
        pFunnelEndField.Text = pEnd;
        pFunnelAndMode = pAndMode;
        pFunnelTargetPending = pTargetIndex;
        PFunnelJoinLabelUpdate();
    }

    public void PFunnelRowTargetSet(Guid pTargetId)
    {
        pFunnelTargetId = pTargetId;
        pFunnelTargetPending = -1;
        PFunnelTargetLabelUpdate();
    }

    public bool PFunnelRowMatch(string pFileName)
    {
        string pStart = PFunnelRowStart;
        string pEnd = PFunnelRowEnd;
        if (pStart.Length == 0 && pEnd.Length == 0)
        {
            return false;
        }

        bool pStartHas = pStart.Length > 0;
        bool pEndHas = pEnd.Length > 0;
        bool pStartOk = !pStartHas || pFileName.StartsWith(pStart, StringComparison.OrdinalIgnoreCase);
        bool pEndOk = !pEndHas || pFileName.EndsWith(pEnd, StringComparison.OrdinalIgnoreCase);

        if (pStartHas && pEndHas)
        {
            return pFunnelAndMode ? pStartOk && pEndOk : pStartOk || pEndOk;
        }

        return pStartOk && pEndOk;
    }

    private static TextBox PFunnelFieldBuild()
    {
        var pField = new TextBox
        {
            Height = PFunnelFieldHeight,
            FontSize = 12,
            FontFamily = pFunnelFontFamily,
            VerticalContentAlignment = VerticalAlignment.Center,
            Padding = new Thickness(6, 0, 6, 0)
        };
        return pField;
    }

    private Button PFunnelJoinBuild()
    {
        var pButton = new Button
        {
            Height = 24,
            MinWidth = 64,
            Margin = new Thickness(0, 6, 0, 6),
            HorizontalAlignment = HorizontalAlignment.Center,
            FontSize = 11,
            FontFamily = pFunnelFontFamily,
            Foreground = pFunnelAccentBrush,
            Background = Brushes.White,
            BorderBrush = pFunnelLineBrush,
            BorderThickness = new Thickness(1),
            Cursor = System.Windows.Input.Cursors.Hand,
            FocusVisualStyle = null,
            ToolTip = LLocalization.LLocalizationTextRead("Inspector.Funnel.And")
        };
        pButton.Click += (_, _) =>
        {
            pFunnelAndMode = !pFunnelAndMode;
            PFunnelJoinLabelUpdate();
            PFunnelRowChange?.Invoke();
        };
        return pButton;
    }

    private void PFunnelJoinLabelUpdate()
    {
        pFunnelJoinButton.Content = LLocalization.LLocalizationTextRead(
            pFunnelAndMode ? "Inspector.Funnel.And" : "Inspector.Funnel.Or");
    }

    private Button PFunnelTargetBuild()
    {
        var pButton = new Button
        {
            Height = PFunnelFieldHeight,
            MinWidth = 120,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Left,
            Padding = new Thickness(8, 0, 8, 0),
            Background = Brushes.White,
            BorderBrush = pFunnelLineBrush,
            BorderThickness = new Thickness(1),
            Cursor = System.Windows.Input.Cursors.Hand,
            FocusVisualStyle = null,
            Content = pFunnelTargetLabel
        };
        pButton.Click += PFunnelTargetOpen;
        return pButton;
    }

    private void PFunnelTargetOpen(object pSender, RoutedEventArgs pArgs)
    {
        ContextMenu pMenu = PMenu.PMenuCreate(pFunnelTargetButton);
        MenuItem pNoneItem = PMenu.PMenuItemCreate(
            LLocalization.LLocalizationTextRead("Inspector.Funnel.RelayNone"), null);
        pNoneItem.Click += (_, _) =>
        {
            PFunnelRowTargetSet(Guid.Empty);
            PFunnelRowChange?.Invoke();
        };
        pMenu.Items.Add(pNoneItem);

        foreach (LCourierOption pOption in pFunnelOptionsRead())
        {
            MenuItem pItem = PMenu.PMenuItemCreate(pOption.LCourierTabTitle, pOption.LCourierTabIcon);
            Guid pOptionId = pOption.LCourierTabId;
            pItem.Click += (_, _) =>
            {
                PFunnelRowTargetSet(pOptionId);
                PFunnelRowChange?.Invoke();
            };
            pMenu.Items.Add(pItem);
        }

        pMenu.IsOpen = true;
        pArgs.Handled = true;
    }

    private void PFunnelTargetLabelUpdate()
    {
        LCourierOption? pOption = pFunnelTargetId == Guid.Empty
            ? null
            : pFunnelOptionsRead().FirstOrDefault(pRow => pRow.LCourierTabId == pFunnelTargetId);

        if (pFunnelTargetId != Guid.Empty && pOption is null)
        {
            pFunnelTargetId = Guid.Empty;
        }

        bool pChosen = pOption is not null;
        pFunnelTargetLabel.Text = pOption?.LCourierTabTitle
            ?? LLocalization.LLocalizationTextRead("Inspector.Funnel.RelayNone");
        pFunnelTargetLabel.Foreground = pChosen ? pFunnelTitleBrush : pFunnelMutedBrush;
    }

    private Grid PFunnelLabelRowBuild(string pLabel, UIElement pField)
    {
        var pRow = new Grid { Margin = new Thickness(0, 0, 0, 2) };
        pRow.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        pRow.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var pLabelBlock = new TextBlock
        {
            Text = pLabel,
            FontSize = 11,
            FontFamily = pFunnelFontFamily,
            Foreground = pFunnelMutedBrush,
            Margin = new Thickness(2, 0, 0, 3)
        };
        Grid.SetRow(pLabelBlock, 0);
        Grid.SetRow(pField, 1);
        pRow.Children.Add(pLabelBlock);
        pRow.Children.Add(pField);
        return pRow;
    }

    private Grid PFunnelRelayRowBuild()
    {
        var pRow = new Grid { Margin = new Thickness(0, 8, 0, 0) };
        pRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var pRelayLabel = new TextBlock
        {
            Text = LLocalization.LLocalizationTextRead("Inspector.Funnel.Relay"),
            FontSize = 12,
            FontFamily = pFunnelFontFamily,
            Foreground = pFunnelTitleBrush,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(2, 0, 8, 0)
        };

        Button pRemoveButton = PFunnelRemoveBuild();

        Grid.SetColumn(pRelayLabel, 0);
        Grid.SetColumn(pFunnelTargetButton, 1);
        Grid.SetColumn(pRemoveButton, 2);
        pRow.Children.Add(pRelayLabel);
        pRow.Children.Add(pFunnelTargetButton);
        pRow.Children.Add(pRemoveButton);
        return pRow;
    }

    private Button PFunnelRemoveBuild()
    {
        var pButton = new Button
        {
            Width = 26,
            Height = PFunnelFieldHeight,
            Margin = new Thickness(8, 0, 0, 0),
            Content = "×",
            FontSize = 15,
            Foreground = pFunnelMutedBrush,
            Background = Brushes.White,
            BorderBrush = pFunnelLineBrush,
            BorderThickness = new Thickness(1),
            Cursor = System.Windows.Input.Cursors.Hand,
            FocusVisualStyle = null,
            ToolTip = LLocalization.LLocalizationTextRead("Inspector.Funnel.Remove")
        };
        pButton.Click += (_, _) => PFunnelRowRemove?.Invoke(this);
        return pButton;
    }
}
