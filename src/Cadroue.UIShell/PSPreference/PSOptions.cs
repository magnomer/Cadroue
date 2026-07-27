using System.Windows;
using System.Windows.Controls;

namespace Cadroue.UIShell;

public static class PSOptions
{
    public static void PSOptionsShow(Window pOwner, Action<LPreferenceState>? pApplyCallback)
    {
        LPreferenceState lPreferenceDraft = App.LPreferenceStateCurrent.LPreferenceClone();
        var pWindow = new Window
        {
            Title = "Preferences",
            Width = 760,
            Height = 560,
            MinWidth = 620,
            MinHeight = 420,
            Owner = pOwner,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var pRoot = new DockPanel();
        var pButtons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(12) };
        var pApply = new Button { Content = "Apply", Width = 84, Margin = new Thickness(4) };
        var pOk = new Button { Content = "OK", Width = 84, Margin = new Thickness(4) };
        var pCancel = new Button { Content = "Cancel", Width = 84, Margin = new Thickness(4) };
        pButtons.Children.Add(pApply);
        pButtons.Children.Add(pOk);
        pButtons.Children.Add(pCancel);
        DockPanel.SetDock(pButtons, Dock.Bottom);
        pRoot.Children.Add(pButtons);

        var pTabs = new TabControl { Margin = new Thickness(12) };
        pTabs.Items.Add(PSOptionsPageBuild("Layout",
            PSOptionsSliderBuild("Program width", lPreferenceDraft.LPreferenceProgramWidth, 800, 4000, v => lPreferenceDraft.LPreferenceProgramWidth = v),
            PSOptionsSliderBuild("Program height", lPreferenceDraft.LPreferenceProgramHeight, 400, 3000, v => lPreferenceDraft.LPreferenceProgramHeight = v),
            PSOptionsSliderBuild("Timeline height", lPreferenceDraft.LPreferenceFlowHeight, 200, 520, v => lPreferenceDraft.LPreferenceFlowHeight = v),
            PSOptionsSliderBuild("Font size", lPreferenceDraft.LPreferenceFontSize, 9, 18, v => lPreferenceDraft.LPreferenceFontSize = v)));
        pTabs.Items.Add(PSOptionsPageBuild("Timeline",
            PSOptionsComboBuild("Timeline order", lPreferenceDraft.LPreferenceTimelineOrder, new[] { "OverviewFirst", "WorkingFirst" }, v => lPreferenceDraft.LPreferenceTimelineOrder = v),
            PSOptionsSliderBuild("Keyframe minimum spacing", lPreferenceDraft.LPreferenceKeyframeMinimumPixels, 1, 50, v => lPreferenceDraft.LPreferenceKeyframeMinimumPixels = v),
            PSOptionsSliderBuild("Immediate keyframe window (ms)", lPreferenceDraft.LPreferenceImmediateKeyframeWindowMilliseconds, 1000, 600000, v => lPreferenceDraft.LPreferenceImmediateKeyframeWindowMilliseconds = v)));
        pTabs.Items.Add(PSOptionsPageBuild("Section & Group",
            PSOptionsSliderBuild("Section overlap opacity", lPreferenceDraft.LPreferenceSectionOpacity, 0.10, 0.95, v => lPreferenceDraft.LPreferenceSectionOpacity = v),
            PSOptionsCheckBuild("Allow duplicate sections in groups", lPreferenceDraft.LPreferenceGroupDuplicateAllowed, v => lPreferenceDraft.LPreferenceGroupDuplicateAllowed = v)));
        pTabs.Items.Add(PSOptionsPageBuild("History",
            PSOptionsComboBuild("Shortcut history mode", lPreferenceDraft.LPreferenceHistoryMode, new[] { "Hover", "LastUsed" }, v => lPreferenceDraft.LPreferenceHistoryMode = v),
            PSOptionsSliderBuild("Maximum section/group history", lPreferenceDraft.LPreferenceHistoryMaximum, 0, 1000000, v => lPreferenceDraft.LPreferenceHistoryMaximum = v)));
        pTabs.Items.Add(PSOptionsPageBuild("Misc",
            PSOptionsFolderBuild("Workspace folder", lPreferenceDraft.LPreferenceWorkspaceFolder, v => lPreferenceDraft.LPreferenceWorkspaceFolder = v),
            PSOptionsCheckBuild("Autoplay on load", lPreferenceDraft.LPreferenceAutoplayOnLoad, v => lPreferenceDraft.LPreferenceAutoplayOnLoad = v),
            PSOptionsComboBuild("Volume mode", lPreferenceDraft.LPreferenceVolumeMode, new[] { "Single global volume", "Per-tab volume" }, v => lPreferenceDraft.LPreferenceVolumeMode = v),
            PSOptionsSliderBuild("Player volume", lPreferenceDraft.LPreferenceVolume, 0, 100, v => lPreferenceDraft.LPreferenceVolume = v)));
        pRoot.Children.Add(pTabs);
        pWindow.Content = pRoot;

        void pApplyAction()
        {
            lPreferenceDraft.LPreferenceNormalize();
            App.LPreferenceStateSet(lPreferenceDraft.LPreferenceClone());
            pApplyCallback?.Invoke(App.LPreferenceStateCurrent);
        }

        pApply.Click += (_, _) => pApplyAction();
        pOk.Click += (_, _) => { pApplyAction(); pWindow.Close(); };
        pCancel.Click += (_, _) => pWindow.Close();
        pWindow.ShowDialog();
    }

    private static TabItem PSOptionsPageBuild(string pTitle, params UIElement[] pChildren)
    {
        var pPanel = new StackPanel { Margin = new Thickness(14) };
        foreach (UIElement pChild in pChildren) pPanel.Children.Add(pChild);
        return new TabItem { Header = pTitle, Content = new ScrollViewer { Content = pPanel } };
    }

    private static FrameworkElement PSOptionsSliderBuild(string pLabel, double pValue, double pMin, double pMax, Action<double> pChange)
    {
        var pText = new TextBlock { Width = 230, VerticalAlignment = VerticalAlignment.Center, Text = pLabel };
        var pValueText = new TextBlock { Width = 72, VerticalAlignment = VerticalAlignment.Center };
        var pSlider = new Slider { Minimum = pMin, Maximum = pMax, Value = pValue, Width = 260, VerticalAlignment = VerticalAlignment.Center };
        pValueText.Text = PSOptionsNumberFormat(pSlider.Value);
        pSlider.ValueChanged += (_, _) => { pValueText.Text = PSOptionsNumberFormat(pSlider.Value); pChange(pSlider.Value); };
        var pRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
        pRow.Children.Add(pText); pRow.Children.Add(pSlider); pRow.Children.Add(pValueText);
        return pRow;
    }

    private static FrameworkElement PSOptionsCheckBuild(string pLabel, bool pValue, Action<bool> pChange)
    {
        var pCheck = new CheckBox { Content = pLabel, IsChecked = pValue, Margin = new Thickness(0, 0, 0, 10) };
        pCheck.Checked += (_, _) => pChange(true);
        pCheck.Unchecked += (_, _) => pChange(false);
        return pCheck;
    }

    private static FrameworkElement PSOptionsFolderBuild(string pLabel, string pValue, Action<string> pChange)
    {
        var pText = new TextBlock { Width = 230, VerticalAlignment = VerticalAlignment.Center, Text = pLabel };
        var pPathBox = new TextBox
        {
            Width = 300,
            VerticalAlignment = VerticalAlignment.Center,
            Text = pValue,
            ToolTip = "Scheduled work is stored here as files. Leave blank for the default."
        };
        var pDefaultText = new TextBlock
        {
            Margin = new Thickness(238, 2, 0, 0),
            FontSize = 11,
            Foreground = System.Windows.Media.Brushes.Gray,
            Text = $"Default: {Cadroue.Core.LDepot.LDepotDefaultRootRead()}"
        };

        var pBrowse = new Button { Content = "Browse", Width = 76, Margin = new Thickness(6, 0, 0, 0) };
        var pReset = new Button { Content = "Reset", Width = 60, Margin = new Thickness(4, 0, 0, 0) };

        pPathBox.TextChanged += (_, _) => pChange(pPathBox.Text);
        pBrowse.Click += (_, _) =>
        {
            var pDialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Choose workspace folder",
                InitialDirectory = string.IsNullOrWhiteSpace(pPathBox.Text)
                    ? Cadroue.Core.LDepot.LDepotDefaultRootRead()
                    : pPathBox.Text
            };
            if (pDialog.ShowDialog() == true)
            {
                pPathBox.Text = pDialog.FolderName;
            }
        };
        pReset.Click += (_, _) => pPathBox.Text = string.Empty;

        var pRow = new StackPanel { Orientation = Orientation.Horizontal };
        pRow.Children.Add(pText);
        pRow.Children.Add(pPathBox);
        pRow.Children.Add(pBrowse);
        pRow.Children.Add(pReset);

        var pStack = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
        pStack.Children.Add(pRow);
        pStack.Children.Add(pDefaultText);
        return pStack;
    }

    private static FrameworkElement PSOptionsComboBuild(string pLabel, string pValue, string[] pValues, Action<string> pChange)
    {
        var pText = new TextBlock { Width = 230, VerticalAlignment = VerticalAlignment.Center, Text = pLabel };
        var pCombo = new ComboBox { Width = 180, ItemsSource = pValues, SelectedItem = pValue, Margin = new Thickness(0, 0, 0, 10) };
        pCombo.SelectionChanged += (_, _) => { if (pCombo.SelectedItem is string pSelected) pChange(pSelected); };
        var pRow = new StackPanel { Orientation = Orientation.Horizontal };
        pRow.Children.Add(pText); pRow.Children.Add(pCombo);
        return pRow;
    }

    private static string PSOptionsNumberFormat(double pValue) => pValue >= 1000 ? $"{pValue:0}" : $"{pValue:0.##}";
}
