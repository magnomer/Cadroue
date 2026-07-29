using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.Core;
using Cadroue.UIShell.PMainWindow;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PInspector
{
    private CheckBox pInspectorNormalizeApply = null!;
    private CheckBox pInspectorNormalizePersistent = null!;
    private ComboBox pInspectorNormalizePreset = null!;
    private ComboBox pInspectorNormalizeMode = null!;
    private TextBox pInspectorNormalizeTarget = null!;
    private TextBox pInspectorNormalizePeak = null!;
    private TextBox pInspectorNormalizeRange = null!;
    private CheckBox pInspectorNormalizeTwoPass = null!;
    private StackPanel pInspectorNormalizeStack = null!;
    private StackPanel pInspectorNormalizeLoudnessStack = null!;
    private StackPanel pInspectorNormalizeBody = null!;

    private LWorkAudioNormalizeMode PInspectorNormalizeModeRead() =>
        pInspectorNormalizeMode.SelectedIndex == 1
            ? LWorkAudioNormalizeMode.LWorkAudioNormalizeDynamic
            : LWorkAudioNormalizeMode.LWorkAudioNormalizeLoudness;

    private StackPanel PInspectorNormalizeBodyBuild()
    {
        pInspectorNormalizeApply = PInspectorSwitchBuild("Apply", "Apply loudness normalization to queued jobs");
        pInspectorNormalizeApply.Checked += (_, _) => PInspectorNormalizeApplyUpdate();
        pInspectorNormalizeApply.Unchecked += (_, _) => PInspectorNormalizeApplyUpdate();

        pInspectorNormalizePersistent = PInspectorSwitchBuild(
            "Persistent",
            "Apply the current normalize setup to every loaded file");

        pInspectorNormalizePreset = new ComboBox
        {
            Height = PInspectorFieldHeight,
            Width = 140,
            HorizontalAlignment = HorizontalAlignment.Left,
            FontSize = 12,
            FontFamily = pInspectorFontFamily
        };
        PDropdown.PDropdownApply(pInspectorNormalizePreset);
        pInspectorNormalizePreset.Items.Add("Streaming");
        pInspectorNormalizePreset.Items.Add("Podcast");
        pInspectorNormalizePreset.Items.Add("Medium");
        pInspectorNormalizePreset.Items.Add("Broadcast");
        pInspectorNormalizePreset.Items.Add("TV");
        pInspectorNormalizePreset.Items.Add("Custom");
        pInspectorNormalizePreset.SelectedItem = "Medium";
        pInspectorNormalizePreset.SelectionChanged += (_, _) => PInspectorNormalizePresetApply();

        pInspectorNormalizeMode = new ComboBox
        {
            Height = PInspectorFieldHeight,
            Width = 140,
            HorizontalAlignment = HorizontalAlignment.Left,
            FontSize = 12,
            FontFamily = pInspectorFontFamily
        };
        PDropdown.PDropdownApply(pInspectorNormalizeMode);
        pInspectorNormalizeMode.Items.Add("Loudness");
        pInspectorNormalizeMode.Items.Add("Dynamic");
        pInspectorNormalizeMode.SelectedIndex = 0;
        pInspectorNormalizeMode.SelectionChanged += (_, _) => PInspectorNormalizeModeUpdate();

        pInspectorNormalizeTarget = PInspectorDecimalBoxBuild();
        pInspectorNormalizeTarget.Text = "-16";
        pInspectorNormalizePeak = PInspectorDecimalBoxBuild();
        pInspectorNormalizePeak.Text = "-1.5";
        pInspectorNormalizeRange = PInspectorDecimalBoxBuild();
        pInspectorNormalizeRange.Text = "11";

        pInspectorNormalizeTwoPass = new CheckBox
        {
            Content = "Two-pass (accurate)",
            ToolTip = "Measure loudness in a first pass, then apply it in a second pass",
            FontSize = 12,
            FontFamily = pInspectorFontFamily,
            Foreground = PPanelTextBrush,
            VerticalContentAlignment = VerticalAlignment.Center,
            IsChecked = true,
            Margin = new Thickness(0, 8, 0, 0)
        };
        PMainWindow.PCheckbox.PCheckboxApply(pInspectorNormalizeTwoPass);

        pInspectorNormalizeLoudnessStack = new StackPanel();
        pInspectorNormalizeLoudnessStack.Children.Add(PInspectorFieldBuild("Target", PInspectorNormalizeUnitRowBuild(pInspectorNormalizeTarget, "LUFS")));
        pInspectorNormalizeLoudnessStack.Children.Add(PInspectorFieldBuild("Peak", PInspectorNormalizeUnitRowBuild(pInspectorNormalizePeak, "dBTP")));
        pInspectorNormalizeLoudnessStack.Children.Add(PInspectorFieldBuild("Range", PInspectorNormalizeUnitRowBuild(pInspectorNormalizeRange, "LU")));
        pInspectorNormalizeLoudnessStack.Children.Add(pInspectorNormalizeTwoPass);

        var pNotice = new TextBlock
        {
            Text = "Normalize may depend on the analyzed range; preview and export can differ.",
            FontSize = 11,
            FontFamily = pInspectorFontFamily,
            Foreground = pInspectorMutedBrush,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 4, 0, 0)
        };

        pInspectorNormalizeStack = new StackPanel();
        pInspectorNormalizeStack.Children.Add(PInspectorFieldBuild("Preset", pInspectorNormalizePreset));
        pInspectorNormalizeStack.Children.Add(PInspectorFieldBuild("Mode", pInspectorNormalizeMode));
        pInspectorNormalizeStack.Children.Add(pInspectorNormalizeLoudnessStack);
        pInspectorNormalizeStack.Children.Add(pNotice);

        pInspectorNormalizeBody = new StackPanel
        {
            Margin = new Thickness(12, 12, 12, 12),
            Visibility = Visibility.Collapsed
        };
        pInspectorNormalizeBody.Children.Add(pInspectorNormalizeApply);
        pInspectorNormalizeBody.Children.Add(PInspectorSeparatorBuild());
        pInspectorNormalizeBody.Children.Add(pInspectorNormalizeStack);

        PInspectorNormalizeApplyUpdate();
        PInspectorNormalizeModeUpdate();
        PInspectorNormalizePresetApply();
        return pInspectorNormalizeBody;
    }

    private void PInspectorNormalizePresetApply()
    {
        if (pInspectorNormalizePreset.SelectedItem is not string pPreset || pPreset == "Custom")
        {
            PInspectorNormalizeFieldsLock(false);
            return;
        }

        (double pTarget, double pPeak, double pRange) = pPreset switch
        {
            "Streaming" => (-14d, -1d, 11d),
            "Medium" => (-21d, -2d, 11d),
            "Broadcast" => (-23d, -1d, 11d),
            "TV" => (-24d, -2d, 11d),
            _ => (-16d, -1.5d, 11d)
        };

        pInspectorNormalizeTarget.Text = pTarget.ToString("0.###", CultureInfo.InvariantCulture);
        pInspectorNormalizePeak.Text = pPeak.ToString("0.###", CultureInfo.InvariantCulture);
        pInspectorNormalizeRange.Text = pRange.ToString("0.###", CultureInfo.InvariantCulture);
        PInspectorNormalizeFieldsLock(true);
    }

    private void PInspectorNormalizeFieldsLock(bool pLocked)
    {
        double pOpacity = pLocked ? 0.6 : 1;
        pInspectorNormalizeTarget.IsReadOnly = pLocked;
        pInspectorNormalizePeak.IsReadOnly = pLocked;
        pInspectorNormalizeRange.IsReadOnly = pLocked;
        pInspectorNormalizeTarget.Opacity = pOpacity;
        pInspectorNormalizePeak.Opacity = pOpacity;
        pInspectorNormalizeRange.Opacity = pOpacity;
    }

    private UIElement PInspectorNormalizeUnitRowBuild(TextBox pValueBox, string pUnitLabel)
    {
        pValueBox.VerticalAlignment = VerticalAlignment.Center;
        var pUnitRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };
        pUnitRow.Children.Add(pValueBox);
        pUnitRow.Children.Add(new TextBlock
        {
            Text = pUnitLabel,
            FontSize = 11,
            FontFamily = pInspectorFontFamily,
            Foreground = pInspectorMutedBrush,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 0, 0)
        });
        return pUnitRow;
    }

    private void PInspectorNormalizeApplyUpdate()
    {
        bool pNormalizeActive = pInspectorNormalizeApply.IsChecked == true;
        pInspectorNormalizeStack.IsEnabled = pNormalizeActive;
        pInspectorNormalizeStack.Opacity = pNormalizeActive ? 1 : 0.4;
        PInspectorAudioActiveRaise();
    }

    private void PInspectorNormalizeModeUpdate()
    {
        bool pLoudness = PInspectorNormalizeModeRead() == LWorkAudioNormalizeMode.LWorkAudioNormalizeLoudness;
        pInspectorNormalizeLoudnessStack.IsEnabled = pLoudness;
        pInspectorNormalizeLoudnessStack.Opacity = pLoudness ? 1 : 0.4;
    }
}
