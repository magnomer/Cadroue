using System.Windows.Controls;
using System.Windows.Input;
using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

// An inspector field row from a fresh PField copy: label, slider, unit and number box, with their commits.
public static class QField
{
    private const string QFieldSheet = "PField";

    private const string QFieldLabelPart = "PFieldLabel";

    private const string QFieldSliderPart = "PFieldSlider";

    private const string QFieldUnitPart = "PFieldUnit";

    private const string QFieldValuePart = "PFieldValue";

    private static readonly IReadOnlyDictionary<Key, bool> qFieldHandled = new Dictionary<Key, bool>
    {
        [Key.Enter] = true,
    };

    public static Grid QFieldBuild(string lLabel, string lUnit, double lLeast, double lMost, double lOrigin)
    {
        Grid lField = QFieldCreate(lLabel, lUnit, lLeast, lMost, lOrigin);
        QFieldControlAttach(lField, lOrigin);
        return lField;
    }

    private static Grid QFieldCreate(string lLabel, string lUnit, double lLeast, double lMost, double lOrigin)
    {
        Grid lField = QContract.QContractSheetFind<Grid>(QFieldSheet);
        QContract.QContractFind<TextBlock>(lField, QFieldLabelPart).Text = lLabel;
        QContract.QContractFind<TextBlock>(lField, QFieldUnitPart).Text = lUnit;

        Slider lSlider = QContract.QContractFind<Slider>(lField, QFieldSliderPart);
        lSlider.Minimum = lLeast;
        lSlider.Maximum = lMost;
        lSlider.Value = lOrigin;
        return lField;
    }

    // The field's own control behaviour: slider and number box kits, reset to origin, decimal-only typing.
    private static void QFieldControlAttach(Grid lField, double lOrigin)
    {
        Slider lSlider = QContract.QContractFind<Slider>(lField, QFieldSliderPart);
        QSlider.QSliderAttach(lSlider);
        QSlider.QSliderResetAttach(lSlider, () => lOrigin);

        TextBox lValue = QContract.QContractFind<TextBox>(lField, QFieldValuePart);
        QTextbox.QTextboxAttach(lValue);
        lValue.PreviewTextInput += QFieldInputHandle;
    }

    private static void QFieldInputHandle(object lSender, TextCompositionEventArgs lEvent) =>
        lEvent.Handled = !CInspectorField.CInspectorDecimalCheck(lEvent.Text);

    public static void QFieldAttach(
        Grid lField, double? lLeast, double? lMost, Func<double> lRead, Action<double> lSet)
    {
        Slider lSlider = QContract.QContractFind<Slider>(lField, QFieldSliderPart);
        TextBox lValue = QContract.QContractFind<TextBox>(lField, QFieldValuePart);
        var lValueKeys = new Dictionary<Key, Action>
        {
            [Key.Enter] = () => QFieldCommit(lValue, lLeast, lMost, lRead, lSet),
        };
        lSlider.ValueChanged += (_, _) =>
            CInspectorField.CInspectorSlideCommit(lSlider.Value, lRead(), lSlider.Minimum, lSlider.Maximum, lSet);
        lValue.LostFocus += (_, _) => QFieldCommit(lValue, lLeast, lMost, lRead, lSet);
        lValue.KeyDown += (_, lKeyEvent) => QFieldKeyHandle(lKeyEvent, lValueKeys);
    }

    private static void QFieldKeyHandle(KeyEventArgs lKeyEvent, IReadOnlyDictionary<Key, Action> lValueKeys)
    {
        lValueKeys.GetValueOrDefault(lKeyEvent.Key)?.Invoke();
        lKeyEvent.Handled = qFieldHandled.GetValueOrDefault(lKeyEvent.Key);
    }

    public static void QFieldUpdate(Grid lField, double lNumber, string lFormat)
    {
        Slider lSlider = QContract.QContractFind<Slider>(lField, QFieldSliderPart);
        TextBox lValue = QContract.QContractFind<TextBox>(lField, QFieldValuePart);
        QInspectorField.QInspectorFieldUpdate(lSlider, lValue, lNumber, lFormat);
    }

    private static void QFieldCommit(
        TextBox lValue, double? lLeast, double? lMost, Func<double> lRead, Action<double> lSet)
    {
        lSet(CInspectorField.CInspectorValueCommit(lValue.Text, lRead(), lLeast, lMost));
        QFieldCaretSet(lValue);
    }

    private static void QFieldCaretSet(TextBox lValue) => lValue.CaretIndex = lValue.Text.Length;
}
