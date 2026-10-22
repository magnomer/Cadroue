using System.Windows;
using System.Windows.Controls;
using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

// The ratio fields' wiring: shows the crop gate's ratio and hands every ratio input to it.
public sealed class QInspectorRatio
{
    private readonly QInspectorCrop qInspectorRatioCrop;
    private readonly CCrop qInspectorRatioGate;
    private Delegate[] qInspectorRatioPainting = null!;
    private StackPanel qInspectorRatioCustom = null!;
    private CheckBox qInspectorRatioFixed = null!;
    private TextBox qInspectorRatioHeight = null!;
    private CheckBox qInspectorRatioLenient = null!;
    private TextBlock qInspectorRatioNotice = null!;
    private ComboBox qInspectorRatioPreset = null!;
    private TextBox qInspectorRatioWidth = null!;

    public QInspectorRatio(QInspector lOwner)
    {
        qInspectorRatioCrop = lOwner.QInspectorCrop;
        qInspectorRatioGate = lOwner.QInspectorGate.CInspectorCrop;
    }

    public StackPanel QInspectorRatioBody { get; private set; } = null!;

    public void QInspectorRatioBuild()
    {
        QInspectorRatioBody = QContract.QContractSheetFind<StackPanel>("PInspectorRatioBody");
        qInspectorRatioCustom =
            QContract.QContractFind<StackPanel>(QInspectorRatioBody, "PInspectorRatioCustom");
        qInspectorRatioFixed = QContract.QContractFind<CheckBox>(QInspectorRatioBody, "PInspectorRatioFixed");
        qInspectorRatioHeight = QContract.QContractFind<TextBox>(QInspectorRatioBody, "PInspectorRatioHeight");
        qInspectorRatioLenient =
            QContract.QContractFind<CheckBox>(QInspectorRatioBody, "PInspectorRatioLenient");
        qInspectorRatioNotice =
            QContract.QContractFind<TextBlock>(QInspectorRatioBody, "PInspectorRatioNotice");
        qInspectorRatioPreset = QContract.QContractFind<ComboBox>(QInspectorRatioBody, "PInspectorRatioPreset");
        qInspectorRatioWidth = QContract.QContractFind<TextBox>(QInspectorRatioBody, "PInspectorRatioWidth");
        TextBlock lLabel = QContract.QContractFind<TextBlock>(QInspectorRatioBody, "PInspectorRatioLabel");
        lLabel.Text = QLocalizationCatalog.QLocalizationTextRead("Inspector.Crop.Ratio");
        lLabel.Foreground = QPanel.QPanelTextRead();
        qInspectorRatioFixed.Content = QLocalizationCatalog.QLocalizationTextRead("Inspector.Crop.FixedRatio");
        qInspectorRatioFixed.Foreground = QPanel.QPanelTextRead();
        qInspectorRatioLenient.Content = QLocalizationCatalog.QLocalizationTextRead("Inspector.Crop.Lenient");
        qInspectorRatioLenient.Foreground = QPanel.QPanelTextRead();
        qInspectorRatioLenient.ToolTip =
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Crop.LenientTooltip");
        qInspectorRatioPreset.Items.Add(QLocalizationCatalog.QLocalizationTextRead("Inspector.Crop.RatioCustom"));
        foreach (string lRatio in new[] { "16:9", "9:16", "4:3", "3:4", "1:1", "21:9" })
        {
            qInspectorRatioPreset.Items.Add(lRatio);
        }

        qInspectorRatioPreset.SelectedIndex = 0;
        QDropdown.QDropdownAttach(qInspectorRatioPreset);
        QTextbox.QTextboxAttach(qInspectorRatioWidth);
        QTextbox.QTextboxAttach(qInspectorRatioHeight);
        QInteractionClick.QInteractionClickAttach(qInspectorRatioFixed);
        QInteractionClick.QInteractionClickAttach(qInspectorRatioLenient);
        QCheckbox.QCheckboxAttach(qInspectorRatioFixed);
        QCheckbox.QCheckboxAttach(qInspectorRatioLenient);
        QInspectorRatioAttach();
        QInspectorRatioBuild(true);
    }

    public void QInspectorRatioBuild(bool lAttached)
    {
        foreach ((Action lAttach, Action lDetach) in
                 (List<(Action, Action)>)qInspectorRatioCustom.Resources[typeof(QInspectorRatio)])
        {
            if (lAttached) lAttach(); else lDetach();
        }

        qInspectorRatioCustom.Resources[typeof(QInspectorFrame)] = lAttached;
    }

    public void QInspectorRatioUpdate()
    {
        bool lAttached = !qInspectorRatioCustom.Resources.Contains(typeof(QInspectorFrame))
            || qInspectorRatioCustom.Resources[typeof(QInspectorFrame)] is true;
        if (lAttached)
        {
            qInspectorRatioWidth.TextChanged -= (TextChangedEventHandler)qInspectorRatioPainting[0];
            qInspectorRatioHeight.TextChanged -= (TextChangedEventHandler)qInspectorRatioPainting[1];
            qInspectorRatioPreset.SelectionChanged -= (SelectionChangedEventHandler)qInspectorRatioPainting[2];
        }

        try
        {
            QInspectorCrop lCrop = qInspectorRatioCrop;
            qInspectorRatioPreset.SelectedIndex = lCrop.QInspectorPresetIndex;
            qInspectorRatioCustom.Visibility = lCrop.QInspectorCustomShown ? Visibility.Visible : Visibility.Collapsed;
            QTool.QToolSwitchUpdate(qInspectorRatioFixed, lCrop.QInspectorRatioFixed);
            QTool.QToolSwitchUpdate(qInspectorRatioLenient, lCrop.QInspectorRatioLenient);
            qInspectorRatioLenient.IsEnabled = lCrop.QInspectorRatioFixed;
            QInspectorRatioApply(true);
            QInspectorRatioApply(false);
            qInspectorRatioNotice.Text = lCrop.QInspectorNoticeRead();
            qInspectorRatioNotice.Visibility = lCrop.QInspectorNoticeShown ? Visibility.Visible : Visibility.Collapsed;
        }
        finally
        {
            if (lAttached)
            {
                qInspectorRatioWidth.TextChanged += (TextChangedEventHandler)qInspectorRatioPainting[0];
                qInspectorRatioHeight.TextChanged += (TextChangedEventHandler)qInspectorRatioPainting[1];
                qInspectorRatioPreset.SelectionChanged += (SelectionChangedEventHandler)qInspectorRatioPainting[2];
            }
        }
    }

    public string QInspectorRatioFormat(bool lWidth, string lText) =>
        QInspectorField.QInspectorWholeFormat(lText, qInspectorRatioGate.CCropRatioRead(lWidth));

    public void QInspectorRatioCommit(string lWidthText, string lHeightText, bool lTyped) =>
        qInspectorRatioGate.CCropRatioCommit(lWidthText, lHeightText, lTyped);

    public void QInspectorFixedSet(bool lFixed) => qInspectorRatioGate.CCropFixedSet(lFixed);

    public void QInspectorLenientSet(bool lLenient) => qInspectorRatioGate.CCropLenientSet(lLenient);

    public void QInspectorPresetSelect(int lIndex) => qInspectorRatioGate.CCropPresetSelect(lIndex);

    public void QInspectorRatioApply(bool lRatioFixed, bool lRatioLenient, int lRatioWidth, int lRatioHeight) =>
        qInspectorRatioGate.CCropRatioApply(lRatioFixed, lRatioLenient, lRatioWidth, lRatioHeight);

    private void QInspectorRatioAttach()
    {
        qInspectorRatioWidth.PreviewTextInput += (_, lEvent) =>
            lEvent.Handled = !QInspectorField.QInspectorDigitCheck(lEvent.Text);
        qInspectorRatioHeight.PreviewTextInput += (_, lEvent) =>
            lEvent.Handled = !QInspectorField.QInspectorDigitCheck(lEvent.Text);
        TextChangedEventHandler lWidth = (_, _) =>
            QInspectorRatioCommit(true);
        TextChangedEventHandler lHeight = (_, _) =>
            QInspectorRatioCommit(false);
        SelectionChangedEventHandler lPreset = (_, _) =>
            QInspectorPresetSelect(qInspectorRatioPreset.SelectedIndex);
        qInspectorRatioPainting = [lWidth, lHeight, lPreset];
        Action lAttach = () =>
        {
            qInspectorRatioWidth.TextChanged -= lWidth;
            qInspectorRatioHeight.TextChanged -= lHeight;
            qInspectorRatioPreset.SelectionChanged -= lPreset;
            qInspectorRatioWidth.TextChanged += lWidth;
            qInspectorRatioHeight.TextChanged += lHeight;
            qInspectorRatioPreset.SelectionChanged += lPreset;
        };
        Action lDetach = () =>
        {
            qInspectorRatioWidth.TextChanged -= lWidth;
            qInspectorRatioHeight.TextChanged -= lHeight;
            qInspectorRatioPreset.SelectionChanged -= lPreset;
        };
        lAttach();
        qInspectorRatioCustom.Resources[typeof(QInspectorRatio)] = new List<(Action, Action)>
        {
            (lAttach, lDetach),
            QTool.QToolSwitchAttach(qInspectorRatioFixed, QInspectorFixedSet),
            QTool.QToolSwitchAttach(qInspectorRatioLenient, QInspectorLenientSet),
        };
    }

    private void QInspectorRatioCommit(bool lWidth) =>
        QInspectorRatioCommit(
            qInspectorRatioWidth.Text,
            qInspectorRatioHeight.Text,
            lWidth ? qInspectorRatioWidth.IsKeyboardFocusWithin : qInspectorRatioHeight.IsKeyboardFocusWithin);

    private void QInspectorRatioApply(bool lWidth)
    {
        TextBox lRatioBox = lWidth ? qInspectorRatioWidth : qInspectorRatioHeight;
        string lText = QInspectorRatioFormat(lWidth, lRatioBox.Text);
        lRatioBox.Text = lText;
    }

}
