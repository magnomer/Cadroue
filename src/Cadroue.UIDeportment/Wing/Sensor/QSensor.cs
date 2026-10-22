using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Cadroue.Conduct;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed class QSensor
{
    public QSensor()
        : this(new CDetector(), new CSweep())
    {
    }

    internal QSensor(CDetector detector, CSweep sweep)
    {
        CDetector = detector;
        QSensorSweep = new QSensorSweep(sweep);
        QSensorPreset = new QSensorPreset(this);
        QSensorPlans = detector.CDetectorPlans.Select(QSensorPlan.QSensorPlanCreate).ToList();
    }

    internal CDetector CDetector { get; }

    public IReadOnlyList<QSensorPlan> QSensorPlans { get; }

    public QSensorPreset QSensorPreset { get; }

    public QSensorSweep QSensorSweep { get; }

    public LDetectorStillMode QSensorMode => CDetector.CDetectorMode;

    public LDetectorLuminanceMode QSensorSpeed => CDetector.CDetectorSpeed;

    public LDetectorMetricMode QSensorMetric => CDetector.CDetectorMetric;

    public LDetectorStep QSensorStepRead(LDetectorKind lKind) => CDetector.CDetectorStepRead(lKind);

    public void QSensorBuild(LDetectorKind lKind, FrameworkElement lSurface)
    {
        QSensorPlan lPlan = QSensorPlans.Single(lEntry => lEntry.QSensorPlanKind == lKind);
        if (ReferenceEquals(lPlan.PSensorBody, lSurface))
        {
            QSensorUpdate();
            return;
        }

        QSensorDetach();
        lPlan.QSensorBuild(lSurface);
        QInteractionClick.QInteractionClickAttach(lPlan.PSensorEnablement);
        QCheckbox.QCheckboxAttach(lPlan.PSensorEnablement);
        QDropdown.QDropdownAttach(lPlan.PSensorProfiles);

        QInspectorChoice lChoice = QSensorPreset.QSensorChoiceRead(lKind);
        foreach (string lName in lChoice.QInspectorChoiceNames)
        {
            lPlan.PSensorProfiles.Items.Add(lName);
        }

        foreach (Grid lValue in new[] { lPlan.PSensorThreshold, lPlan.PSensorSpan, lPlan.PSensorMinimum })
        {
            var lTrack = (Slider)lValue.Children[1];
            var lText = (TextBox)lValue.Children[3];
            QSlider.QSliderAttach(lTrack);
            QTextbox.QTextboxAttach(lText);
            lText.PreviewTextInput += (_, lInput) =>
                lInput.Handled = !QInspectorField.QInspectorDecimalCheck(lInput.Text);
            int lRow = ReferenceEquals(lValue, lPlan.PSensorThreshold)
                ? QSensorPlan.QSensorPlanThreshold
                : ReferenceEquals(lValue, lPlan.PSensorSpan)
                    ? QSensorPlan.QSensorPlanWindow
                    : QSensorPlan.QSensorPlanMinimum;
            QSlider.QSliderResetAttach(lTrack, () => CDetector.CDetectorDefaultRead(lKind, lRow));
        }

        QSensorUpdate();
        QSensorAttach();
    }

    public void QSensorAttach()
    {
        foreach (QSensorPlan lPlan in QSensorPlans.Where(lEntry => lEntry.PSensorBody is not null))
        {
            lPlan.PSensorEnablement.Checked -= QSensorAttach;
            lPlan.PSensorEnablement.Unchecked -= QSensorAttach;
            lPlan.PSensorProfiles.SelectionChanged -= QSensorAttach;
            lPlan.PSensorEnablement.Checked += QSensorAttach;
            lPlan.PSensorEnablement.Unchecked += QSensorAttach;
            lPlan.PSensorProfiles.SelectionChanged += QSensorAttach;
            foreach (Grid lGroup in new[] { lPlan.PSensorModes, lPlan.PSensorSpeeds, lPlan.PSensorMetrics })
            {
                var lOptions = (StackPanel)((Border)lGroup.Children[1]).Child;
                foreach (RadioButton lOption in lOptions.Children)
                {
                    lOption.Checked -= QSensorAttach;
                    lOption.Checked += QSensorAttach;
                }
            }

            foreach (Grid lValue in new[] { lPlan.PSensorThreshold, lPlan.PSensorSpan, lPlan.PSensorMinimum })
            {
                ((Slider)lValue.Children[1]).ValueChanged -= QSensorAttach;
                ((Slider)lValue.Children[1]).ValueChanged += QSensorAttach;
                var lText = (TextBox)lValue.Children[3];
                lText.LostFocus -= QSensorAttach;
                lText.KeyDown -= QSensorAttach;
                lText.LostFocus += QSensorAttach;
                lText.KeyDown += QSensorAttach;
            }
        }

        QSensorPlan? lBuilt = QSensorPlans.FirstOrDefault(lPlan => lPlan.PSensorBody is not null);
        if (lBuilt is not null)
        {
            lBuilt.PSensorBody.Resources[typeof(QSensor)] = true;
        }

        CDetector.CDetectorChanged -= QSensorUpdate;
        CDetector.CDetectorChanged += QSensorUpdate;
    }

    public void QSensorDetach()
    {
        CDetector.CDetectorChanged -= QSensorUpdate;
        QSensorPlan? lBuilt = QSensorPlans.FirstOrDefault(lPlan => lPlan.PSensorBody is not null);
        if (lBuilt is not null)
        {
            lBuilt.PSensorBody.Resources[typeof(QSensor)] = false;
        }

        foreach (QSensorPlan lPlan in QSensorPlans.Where(lEntry => lEntry.PSensorBody is not null))
        {
            lPlan.PSensorEnablement.Checked -= QSensorAttach;
            lPlan.PSensorEnablement.Unchecked -= QSensorAttach;
            lPlan.PSensorProfiles.SelectionChanged -= QSensorAttach;
            foreach (Grid lGroup in new[] { lPlan.PSensorModes, lPlan.PSensorSpeeds, lPlan.PSensorMetrics })
            {
                var lOptions = (StackPanel)((Border)lGroup.Children[1]).Child;
                foreach (RadioButton lOption in lOptions.Children)
                {
                    lOption.Checked -= QSensorAttach;
                }
            }

            foreach (Grid lValue in new[] { lPlan.PSensorThreshold, lPlan.PSensorSpan, lPlan.PSensorMinimum })
            {
                ((Slider)lValue.Children[1]).ValueChanged -= QSensorAttach;
                var lText = (TextBox)lValue.Children[3];
                lText.LostFocus -= QSensorAttach;
                lText.KeyDown -= QSensorAttach;
            }
        }
    }

    private void QSensorAttach(object lSender, KeyEventArgs lKey)
    {
        QSensorPlan? lBuilt = QSensorPlans.FirstOrDefault(lPlan => lPlan.PSensorBody is not null);
        if (lBuilt?.PSensorBody.Resources[typeof(QSensor)] is not true)
        {
            return;
        }

        if (lKey.Key == Key.Enter)
        {
            QSensorAttach(lSender, new RoutedEventArgs());
        }

        lKey.Handled = lKey.Key == Key.Enter;
    }

    private void QSensorAttach(object lSender, RoutedEventArgs __)
    {
        QSensorPlan? lBuilt = QSensorPlans.FirstOrDefault(lPlan => lPlan.PSensorBody is not null);
        if (lBuilt?.PSensorBody.Resources[typeof(QSensor)] is not true)
        {
            return;
        }

        foreach (QSensorPlan lPlan in QSensorPlans.Where(lEntry => lEntry.PSensorBody is not null))
        {
            if (ReferenceEquals(lSender, lPlan.PSensorEnablement))
            {
                CDetector.CDetectorEnabledSet(lPlan.QSensorPlanKind, lPlan.PSensorEnablement.IsChecked == true);
                return;
            }

            if (ReferenceEquals(lSender, lPlan.PSensorProfiles))
            {
                CDetector.CDetectorPreset.CDetectorChoiceSelect(
                    lPlan.QSensorPlanKind, lPlan.PSensorProfiles.SelectedIndex);
                return;
            }

            foreach ((Grid lGroup, Action<int> lSelect) in new (Grid, Action<int>)[]
                     {
                         (lPlan.PSensorModes, CDetector.CDetectorModeSelect),
                         (lPlan.PSensorSpeeds, CDetector.CDetectorSpeedSelect),
                         (lPlan.PSensorMetrics, CDetector.CDetectorMetricSelect),
                     })
            {
                var lOptions = (StackPanel)((Border)lGroup.Children[1]).Child;
                int lIndex = lOptions.Children.IndexOf((UIElement)lSender);
                if (lIndex >= 0 && ((RadioButton)lSender).IsChecked == true)
                {
                    lSelect(lIndex);
                    return;
                }
            }

            foreach ((Grid lValue, int lRow) in new[]
                     {
                         (lPlan.PSensorThreshold, QSensorPlan.QSensorPlanThreshold),
                         (lPlan.PSensorSpan, QSensorPlan.QSensorPlanWindow),
                         (lPlan.PSensorMinimum, QSensorPlan.QSensorPlanMinimum),
                     })
            {
                var lTrack = (Slider)lValue.Children[1];
                if (ReferenceEquals(lSender, lTrack))
                {
                    CDetector.CDetectorValueSet(lPlan.QSensorPlanKind, lRow, lTrack.Value);
                    return;
                }

                var lText = (TextBox)lValue.Children[3];
                if (ReferenceEquals(lSender, lText))
                {
                    CDetector.CDetectorValueSet(
                        lPlan.QSensorPlanKind,
                        lRow,
                        QInspectorField.QInspectorValueCommit(
                            lText.Text,
                            CDetector.CDetectorValueRead(lPlan.QSensorPlanKind, lRow),
                            lTrack.Minimum,
                            lTrack.Maximum));
                    lText.CaretIndex = lText.Text.Length;
                    return;
                }
            }
        }
    }

    private void QSensorUpdate()
    {
        QSensorPlan? lBuilt = QSensorPlans.FirstOrDefault(lPlan => lPlan.PSensorBody is not null);
        bool lAttached = lBuilt?.PSensorBody.Resources[typeof(QSensor)] is true;
        if (lAttached)
        {
            lBuilt!.PSensorBody.Resources[typeof(QSensor)] = false;
        }

        try
        {
            foreach (QSensorPlan lPlan in QSensorPlans.Where(lEntry => lEntry.PSensorBody is not null))
            {
                bool lEnabled = CDetector.CDetectorEnabledRead(lPlan.QSensorPlanKind);
                lPlan.PSensorEnablement.IsChecked = lEnabled;
                QTool.QToolStackUpdate((StackPanel)lPlan.PSensorBody.Children[2], lEnabled);

                foreach ((Grid lGroup, int lIndex) in new[]
                         {
                             (lPlan.PSensorModes, CDetector.CDetectorModeIndex),
                             (lPlan.PSensorSpeeds, CDetector.CDetectorSpeedIndex),
                             (lPlan.PSensorMetrics, CDetector.CDetectorMetricIndex),
                         })
                {
                    var lOptions = (StackPanel)((Border)lGroup.Children[1]).Child;
                    ((RadioButton)lOptions.Children[lIndex]).IsChecked = true;
                }

                QInspectorChoice lChoice = QSensorPreset.QSensorChoiceRead(lPlan.QSensorPlanKind);
                lPlan.PSensorProfiles.Items[lChoice.QInspectorChoiceCustom] = lChoice.QInspectorChoiceText;
                lPlan.PSensorProfiles.SelectedIndex = lChoice.QInspectorChoiceIndex;

                foreach ((Grid lValue, int lRow) in new[]
                         {
                             (lPlan.PSensorThreshold, QSensorPlan.QSensorPlanThreshold),
                             (lPlan.PSensorSpan, QSensorPlan.QSensorPlanWindow),
                             (lPlan.PSensorMinimum, QSensorPlan.QSensorPlanMinimum),
                         })
                {
                    if (lValue.Visibility != Visibility.Visible)
                    {
                        continue;
                    }

                    double lNumber = CDetector.CDetectorValueRead(lPlan.QSensorPlanKind, lRow);
                    var lTrack = (Slider)lValue.Children[1];
                    var lText = (TextBox)lValue.Children[3];
                    lTrack.Value = Math.Clamp(lNumber, lTrack.Minimum, lTrack.Maximum);
                    string lPattern = lPlan.QSensorPlanRows
                        .Single(lEntry => lEntry.QInspectorRowIndex == lRow).QInspectorRowPattern;
                    lText.Text = QInspectorField.QInspectorValueFormat(lText.Text, lNumber, lPattern);
                    ((TextBlock)lValue.Children[2]).Text = CDetector.CDetectorUnitRead(
                        lPlan.QSensorPlanKind, lRow);
                }
            }
        }
        finally
        {
            if (lAttached)
            {
                lBuilt!.PSensorBody.Resources[typeof(QSensor)] = true;
            }
        }
    }

    public void QSensorUpdate(IReadOnlyDictionary<LDetectorKind, bool> lShown)
    {
        foreach (QSensorPlan lPlan in QSensorPlans.Where(lEntry => lEntry.PSensorBody is not null))
        {
            lPlan.PSensorBody.Visibility = lShown[lPlan.QSensorPlanKind]
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
    }
}
