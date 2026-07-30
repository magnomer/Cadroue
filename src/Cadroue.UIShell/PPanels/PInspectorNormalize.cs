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
        pInspectorNormalizeApply = PInspectorSwitchBuild(LLocalization.LLocalizationTextRead("Inspector.Common.Apply"), LLocalization.LLocalizationTextRead("Inspector.Normalize.ApplyTooltip"));
        pInspectorNormalizeApply.Checked += (_, _) => PInspectorNormalizeApplyUpdate();
        pInspectorNormalizeApply.Unchecked += (_, _) => PInspectorNormalizeApplyUpdate();

        pInspectorNormalizePersistent = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Persistent"),
            LLocalization.LLocalizationTextRead("Inspector.Normalize.PersistentTooltip"));

        pInspectorNormalizePreset = new ComboBox
        {
            Height = PInspectorFieldHeight,
            Width = 140,
            HorizontalAlignment = HorizontalAlignment.Left,
            FontSize = 12,
            FontFamily = pInspectorFontFamily
        };
        PDropdown.PDropdownApply(pInspectorNormalizePreset);
        pInspectorNormalizePreset.Items.Add(new LLocalizationChoice("Streaming", "Inspector.Normalize.Streaming"));
        pInspectorNormalizePreset.Items.Add(new LLocalizationChoice("Podcast", "Inspector.Normalize.Podcast"));
        pInspectorNormalizePreset.Items.Add(new LLocalizationChoice("Medium", "Inspector.Normalize.Medium"));
        pInspectorNormalizePreset.Items.Add(new LLocalizationChoice("Broadcast", "Inspector.Normalize.Broadcast"));
        pInspectorNormalizePreset.Items.Add(new LLocalizationChoice("TV", "Inspector.Normalize.TV"));
        pInspectorNormalizePreset.Items.Add(new LLocalizationChoice("Custom", "Inspector.Common.Custom"));
        pInspectorNormalizePreset.SelectedIndex = 2;
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
        pInspectorNormalizeMode.Items.Add(new LLocalizationChoice("Loudness", "Inspector.Normalize.Loudness"));
        pInspectorNormalizeMode.Items.Add(new LLocalizationChoice("Dynamic", "Inspector.Normalize.Dynamic"));
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
            Content = LLocalization.LLocalizationTextRead("Inspector.Normalize.TwoPass"),
            ToolTip = LLocalization.LLocalizationTextRead("Inspector.Normalize.TwoPassTooltip"),
            FontSize = 12,
            FontFamily = pInspectorFontFamily,
            Foreground = PPanelTextBrush,
            VerticalContentAlignment = VerticalAlignment.Center,
            IsChecked = true,
            Margin = new Thickness(0, 8, 0, 0)
        };
        PMainWindow.PCheckbox.PCheckboxApply(pInspectorNormalizeTwoPass);

        pInspectorNormalizeLoudnessStack = new StackPanel();
        pInspectorNormalizeLoudnessStack.Children.Add(PInspectorFieldBuild(LLocalization.LLocalizationTextRead("Inspector.Normalize.Target"), PInspectorNormalizeUnitRowBuild(pInspectorNormalizeTarget, "LUFS")));
        pInspectorNormalizeLoudnessStack.Children.Add(PInspectorFieldBuild(LLocalization.LLocalizationTextRead("Inspector.Normalize.Peak"), PInspectorNormalizeUnitRowBuild(pInspectorNormalizePeak, "dBTP")));
        pInspectorNormalizeLoudnessStack.Children.Add(PInspectorFieldBuild(LLocalization.LLocalizationTextRead("Inspector.Normalize.Range"), PInspectorNormalizeUnitRowBuild(pInspectorNormalizeRange, "LU")));
        pInspectorNormalizeLoudnessStack.Children.Add(pInspectorNormalizeTwoPass);

        var pNotice = new TextBlock
        {
            Text = LLocalization.LLocalizationTextRead("Inspector.Normalize.Notice"),
            FontSize = 11,
            FontFamily = pInspectorFontFamily,
            Foreground = pInspectorMutedBrush,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 4, 0, 0)
        };

        pInspectorNormalizeStack = new StackPanel();
        pInspectorNormalizeStack.Children.Add(PInspectorFieldBuild(LLocalization.LLocalizationTextRead("Inspector.Common.Preset"), pInspectorNormalizePreset));
        pInspectorNormalizeStack.Children.Add(PInspectorFieldBuild(LLocalization.LLocalizationTextRead("Inspector.Normalize.Mode"), pInspectorNormalizeMode));
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

    private static string PInspectorNormalizePresetResolve(double pTarget, double pPeak, double pRange)
    {
        bool pNear(double pLeft, double pRight) => Math.Abs(pLeft - pRight) < 0.01;
        if (pNear(pTarget, -14) && pNear(pPeak, -1) && pNear(pRange, 11)) return "Streaming";
        if (pNear(pTarget, -21) && pNear(pPeak, -2) && pNear(pRange, 11)) return "Medium";
        if (pNear(pTarget, -23) && pNear(pPeak, -1) && pNear(pRange, 11)) return "Broadcast";
        if (pNear(pTarget, -24) && pNear(pPeak, -2) && pNear(pRange, 11)) return "TV";
        return "Custom";
    }

    private void PInspectorNormalizePresetApply()
    {
        string pPreset = LLocalizationChoice.LLocalizationChoiceRead(pInspectorNormalizePreset.SelectedItem);
        if (string.IsNullOrEmpty(pPreset) || pPreset == "Custom")
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
