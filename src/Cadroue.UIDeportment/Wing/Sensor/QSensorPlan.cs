using System.Windows;
using System.Windows.Controls;
using Cadroue.Conduct;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed record QSensorGroup(
    string QSensorGroupLabel,
    IReadOnlyList<string> QSensorGroupNames,
    bool QSensorGroupShown);

public sealed record QSensorPlan(
    LDetectorKind QSensorPlanKind,
    string QSensorPlanSwitch,
    string QSensorPlanTip,
    IReadOnlyList<QInspectorRow> QSensorPlanRows,
    QSensorGroup QSensorPlanMode,
    QSensorGroup QSensorPlanSpeed,
    QSensorGroup QSensorPlanMetric,
    string QSensorPlanChoice,
    bool QSensorPlanPreset)
{
    public const int QSensorPlanThreshold = 0;
    public const int QSensorPlanWindow = 1;
    public const int QSensorPlanMinimum = 2;

    public StackPanel PSensorBody { get; private set; } = null!;
    internal CheckBox PSensorEnablement { get; private set; } = null!;
    internal Grid PSensorChoices { get; private set; } = null!;
    internal Grid PSensorModes { get; private set; } = null!;
    internal Grid PSensorSpeeds { get; private set; } = null!;
    internal Grid PSensorMetrics { get; private set; } = null!;
    internal ComboBox PSensorProfiles { get; private set; } = null!;
    internal Grid PSensorThreshold { get; private set; } = null!;
    internal Grid PSensorSpan { get; private set; } = null!;
    internal Grid PSensorMinimum { get; private set; } = null!;

    internal void QSensorBuild(FrameworkElement lSurface)
    {
        PSensorBody = (StackPanel)lSurface;
        PSensorEnablement = QContract.QContractFind<CheckBox>(lSurface, nameof(PSensorEnablement));
        PSensorChoices = QContract.QContractFind<Grid>(lSurface, nameof(PSensorChoices));
        PSensorModes = QContract.QContractFind<Grid>(lSurface, nameof(PSensorModes));
        PSensorSpeeds = QContract.QContractFind<Grid>(lSurface, nameof(PSensorSpeeds));
        PSensorMetrics = QContract.QContractFind<Grid>(lSurface, nameof(PSensorMetrics));
        PSensorProfiles = QContract.QContractFind<ComboBox>(lSurface, nameof(PSensorProfiles));
        PSensorThreshold = QContract.QContractFind<Grid>(lSurface, nameof(PSensorThreshold));
        PSensorSpan = QContract.QContractFind<Grid>(lSurface, nameof(PSensorSpan));
        PSensorMinimum = QContract.QContractFind<Grid>(lSurface, nameof(PSensorMinimum));

        PSensorEnablement.Content = QSensorPlanSwitch;
        PSensorEnablement.ToolTip = QSensorPlanTip;
        foreach ((Grid lSurfaceGroup, QSensorGroup lGroup) in new[]
                 {
                     (PSensorModes, QSensorPlanMode),
                     (PSensorSpeeds, QSensorPlanSpeed),
                     (PSensorMetrics, QSensorPlanMetric),
                 })
        {
            ((TextBlock)lSurfaceGroup.Children[0]).Text = lGroup.QSensorGroupLabel;
            lSurfaceGroup.Visibility = lGroup.QSensorGroupShown ? Visibility.Visible : Visibility.Collapsed;
            var lOptions = (StackPanel)((Border)lSurfaceGroup.Children[1]).Child;
            for (int lIndex = 0; lIndex < lGroup.QSensorGroupNames.Count; lIndex++)
            {
                ((RadioButton)lOptions.Children[lIndex]).Content = lGroup.QSensorGroupNames[lIndex];
            }
        }

        var lChoicePanel = (StackPanel)PSensorChoices.Children[0];
        ((TextBlock)lChoicePanel.Children[0]).Text = QSensorPlanChoice;
        PSensorChoices.Visibility = QSensorPlanPreset ? Visibility.Visible : Visibility.Collapsed;

        foreach ((Grid lValue, int lRow) in new[]
                 {
                     (PSensorThreshold, QSensorPlanThreshold),
                     (PSensorSpan, QSensorPlanWindow),
                     (PSensorMinimum, QSensorPlanMinimum),
                 })
        {
            QInspectorRow? lPlan = QSensorPlanRows.FirstOrDefault(lEntry => lEntry.QInspectorRowIndex == lRow);
            lValue.Visibility = lPlan is null ? Visibility.Collapsed : Visibility.Visible;
            if (lPlan is null)
            {
                continue;
            }

            ((TextBlock)lValue.Children[0]).Text = lPlan.QInspectorRowLabel;
            var lTrack = (Slider)lValue.Children[1];
            lTrack.Minimum = lPlan.QInspectorRowLeast;
            lTrack.Maximum = lPlan.QInspectorRowMost;
            ((TextBlock)lValue.Children[2]).Text = lPlan.QInspectorRowUnit;
        }
    }

    internal static QSensorPlan QSensorPlanCreate(CDetectorPlan lPlan)
    {
        var lRows = new List<QInspectorRow>
        {
            QInspectorPlan.QInspectorRowCreate(
                QSensorPlanThreshold,
                lPlan.CDetectorPlanLabel,
                lPlan.CDetectorPlanUnit,
                lPlan.CDetectorPlanPattern,
                lPlan.CDetectorPlanThreshold.LDetectorBoundLeast,
                lPlan.CDetectorPlanThreshold.LDetectorBoundMost)
        };
        if (lPlan.CDetectorPlanWindow is { } lWindow)
        {
            lRows.Add(QInspectorPlan.QInspectorRowCreate(
                QSensorPlanWindow,
                "Inspector.Detector.Window",
                "s",
                "0.0",
                lWindow.LDetectorBoundLeast,
                lWindow.LDetectorBoundMost));
        }

        lRows.Add(QInspectorPlan.QInspectorRowCreate(
            QSensorPlanMinimum,
            lPlan.CDetectorPlanKind == LDetectorKind.LDetectorKindBlank
                ? "Inspector.Detector.Minimum"
                : "Inspector.Detector.Minimal",
            "s",
            "0.0",
            lPlan.CDetectorPlanMinimum.LDetectorBoundLeast,
            lPlan.CDetectorPlanMinimum.LDetectorBoundMost));
        return new QSensorPlan(
            lPlan.CDetectorPlanKind,
            QLocalizationCatalog.QLocalizationTextRead(lPlan.CDetectorPlanSwitch),
            QLocalizationCatalog.QLocalizationTextRead(lPlan.CDetectorPlanTip),
            lRows,
            QSensorGroupCreate(lPlan.CDetectorPlanMode),
            QSensorGroupCreate(lPlan.CDetectorPlanSpeed),
            QSensorGroupCreate(lPlan.CDetectorPlanMetric),
            QLocalizationCatalog.QLocalizationTextRead(lPlan.CDetectorPlanChoice),
            lPlan.CDetectorPlanPreset);
    }

    private static QSensorGroup QSensorGroupCreate(CDetectorGroup lGroup) => new(
        QLocalizationCatalog.QLocalizationTextRead(lGroup.CDetectorGroupLabel),
        lGroup.CDetectorGroupNames.Select(QLocalizationCatalog.QLocalizationTextRead).ToList(),
        lGroup.CDetectorGroupShown);
}
