using System.Windows;
using System.Windows.Controls;

namespace Cadroue.UIDeportment;

// The curve section's surface wiring: pulls one sheet and one canvas driver, hands every input to the curve driver,
// and applies the curve state and availability to that independent surface.
public sealed class QCurveSection
{
    private readonly QCurve qCurve;
    private readonly QCurveCanvas qCurveCanvas;
    private readonly Canvas qCurveSectionCanvas;
    private readonly CheckBox qCurveActive;
    private readonly StackPanel qCurveStack;
    private readonly ComboBox qCurveChoice;
    private readonly TextBox qCurveInput;
    private readonly TextBox qCurveOutput;

    public QCurveSection(QCurve lCurve)
    {
        qCurve = lCurve;
        QCurveSectionBody = QContract.QContractSheetFind<StackPanel>("PCurve");
        QCurveSectionPersistent = QContract.QContractSheetFind<CheckBox>("PCurvePersistent");
        qCurveActive = QContract.QContractFind<CheckBox>(QCurveSectionBody, "PCurveActive");
        qCurveStack = QContract.QContractFind<StackPanel>(QCurveSectionBody, "PCurveStack");
        qCurveChoice = QContract.QContractFind<ComboBox>(QCurveSectionBody, "PCurveChoice");
        qCurveInput = QContract.QContractFind<TextBox>(QCurveSectionBody, "PCurveInput");
        qCurveOutput = QContract.QContractFind<TextBox>(QCurveSectionBody, "PCurveOutput");

        qCurveCanvas = lCurve.QCurveCanvas;
        qCurveSectionCanvas = QContract.QContractSheetFind<Canvas>("PCurveCanvas");
        qCurveCanvas.QCurveAttach(qCurveSectionCanvas);
        QContract.QContractFind<Border>(QCurveSectionBody, "PCurveCanvasHost").Child = qCurveSectionCanvas;

        qCurveActive.Content = QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Apply");
        qCurveActive.ToolTip = QLocalizationCatalog.QLocalizationTextRead("Inspector.Video.ApplyCurve");
        QCurveSectionPersistent.Content = QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Persistent");
        QCurveSectionPersistent.ToolTip =
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Video.PersistCurve");
        QContract.QContractFind<TextBlock>(QCurveSectionBody, "PCurveChoiceLabel").Text =
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Video.CurveChannel");
        QContract.QContractFind<TextBlock>(QCurveSectionBody, "PCurveInputLabel").Text =
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Video.CurveInput");
        QContract.QContractFind<TextBlock>(QCurveSectionBody, "PCurveOutputLabel").Text =
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Video.CurveOutput");

        QDropdown.QDropdownAttach(qCurveChoice);
        qCurveChoice.Items.Add(new QLocalizationChoice("Inspector.Video.CurveChannelMaster"));
        qCurveChoice.Items.Add(new QLocalizationChoice("Inspector.Video.CurveChannelRed"));
        qCurveChoice.Items.Add(new QLocalizationChoice("Inspector.Video.CurveChannelGreen"));
        qCurveChoice.Items.Add(new QLocalizationChoice("Inspector.Video.CurveChannelBlue"));
        qCurveChoice.SelectedIndex = 0;

        QTextbox.QTextboxAttach(qCurveInput);
        QTextbox.QTextboxAttach(qCurveOutput);
        qCurveInput.PreviewTextInput +=
            (_, lInput) => lInput.Handled = !QInspectorField.QInspectorDecimalCheck(lInput.Text);
        qCurveOutput.PreviewTextInput +=
            (_, lInput) => lInput.Handled = !QInspectorField.QInspectorDecimalCheck(lInput.Text);

        Button lPointButton = QContract.QContractFind<Button>(QCurveSectionBody, "PCurvePointButton");
        lPointButton.Content = QLocalizationCatalog.QLocalizationTextRead("Inspector.Video.CurveDeletePoint");
        Button lChannelButton = QContract.QContractFind<Button>(QCurveSectionBody, "PCurveChannelButton");
        lChannelButton.Content = QLocalizationCatalog.QLocalizationTextRead("Inspector.Video.CurveResetChannel");
        Button lResetButton = QContract.QContractFind<Button>(QCurveSectionBody, "PCurveResetButton");
        lResetButton.Content = QLocalizationCatalog.QLocalizationTextRead("Inspector.Video.CurveResetAll");
        SelectionChangedEventHandler lChoice = (_, _) => qCurve.QCurveChannelSelect(qCurveChoice.SelectedIndex);
        TextChangedEventHandler lInput = (_, _) => QCurvePointCommit();
        TextChangedEventHandler lOutput = (_, _) => QCurvePointCommit();
        Action lAttach = () =>
        {
            qCurveChoice.SelectionChanged -= lChoice;
            qCurveInput.TextChanged -= lInput;
            qCurveOutput.TextChanged -= lOutput;
            qCurveChoice.SelectionChanged += lChoice;
            qCurveInput.TextChanged += lInput;
            qCurveOutput.TextChanged += lOutput;
        };
        Action lDetach = () =>
        {
            qCurveChoice.SelectionChanged -= lChoice;
            qCurveInput.TextChanged -= lInput;
            qCurveOutput.TextChanged -= lOutput;
        };
        lAttach();
        QCurveSectionBody.Resources[typeof(QCurveSection)] = new List<(Action, Action)>
        {
            QCurveActionAttach(lPointButton, qCurve.QCurvePointDelete),
            QCurveActionAttach(lChannelButton, qCurve.QCurveChannelReset),
            QCurveActionAttach(lResetButton, qCurve.QCurveReset),
            QTool.QToolSwitchAttach(qCurveActive, qCurve.QCurveActiveSet),
            QTool.QToolSwitchAttach(QCurveSectionPersistent, qCurve.QCurvePersistentSet),
            (lAttach, lDetach),
        };
        QCurveSectionAttach();
    }

    public StackPanel QCurveSectionBody { get; }

    public CheckBox QCurveSectionPersistent { get; }

    public void QCurveSectionAttach()
    {
        qCurveCanvas.QCurveAttach(qCurveSectionCanvas, true);
        foreach ((Action lAttach, _) in
                 (List<(Action, Action)>)QCurveSectionBody.Resources[typeof(QCurveSection)])
        {
            lAttach();
        }

        qCurve.QCurveChanged -= QCurveSectionUpdate;
        qCurve.QCurveChanged += QCurveSectionUpdate;
    }

    public void QCurveSectionDetach()
    {
        qCurveCanvas.QCurveAttach(qCurveSectionCanvas, false);
        qCurve.QCurveChanged -= QCurveSectionUpdate;
        foreach ((_, Action lDetach) in
                 (List<(Action, Action)>)QCurveSectionBody.Resources[typeof(QCurveSection)])
        {
            lDetach();
        }
    }

    public void QCurveSectionUpdate()
    {
        QCurveStateApply();
        QTool.QToolTipApply(
            qCurveActive,
            QCurveSectionPersistent,
            qCurveStack,
            QCurveSectionBody,
            QInspectorTip.QInspectorTipResolve(
                qCurve.QCurveActive,
                qCurve.QCurveCapable,
                qCurve.QCurvePreview,
                "Inspector.Video.CurveRequiresEq",
                "Inspector.Video.CurvePreviewMpv",
                "Inspector.Video.ApplyCurve",
                "Inspector.Video.PersistCurve"));
        qCurveCanvas.QCurveRebuild(qCurveSectionCanvas);
    }

    private void QCurvePointCommit() => qCurve.QCurvePointCommit(qCurveInput.Text, qCurveOutput.Text);

    private void QCurveStateApply()
    {
        QTool.QToolSwitchUpdate(qCurveActive, qCurve.QCurveActive);
        QTool.QToolSwitchUpdate(QCurveSectionPersistent, qCurve.QCurvePersistent);
        qCurveChoice.SelectedIndex = qCurve.QCurveChannel;
        qCurveInput.Text = QInspectorField.QInspectorValueFormat(qCurveInput.Text, qCurve.QCurveInputPercent, "0.#");
        qCurveOutput.Text =
            QInspectorField.QInspectorValueFormat(qCurveOutput.Text, qCurve.QCurveOutputPercent, "0.#");
    }

    private static (Action, Action) QCurveActionAttach(Button lButton, Action lAction)
    {
        QButton.QButtonAttach(lButton, QButtonLook.QButtonLookPanel);
        RoutedEventHandler lHandler = (_, _) => lAction();
        Action lAttach = () =>
        {
            lButton.Click -= lHandler;
            lButton.Click += lHandler;
        };
        Action lDetach = () => lButton.Click -= lHandler;
        lAttach();
        return (lAttach, lDetach);
    }
}
