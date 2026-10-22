using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Cadroue.UIDeportment;

public sealed class QBlankLifecycle
{
    private readonly FrameworkElement qBlankLifecycle;

    public QBlankLifecycle(
        FrameworkElement lBody,
        CheckBox lEnabled,
        Action<bool> lEnabledSet,
        RadioButton lBlack,
        RadioButton lColor,
        ToggleButton lPicker,
        IReadOnlyList<(Slider, Func<double>, Action<double>)> lSliders,
        IReadOnlyList<(TextBox, Action)> lValues,
        Canvas lWheel,
        Action lBlackSet,
        Action lColorSet,
        Action lPickSet,
        Action<MouseButtonEventArgs> lPress,
        Action<MouseEventArgs> lMove)
    {
        qBlankLifecycle = lBody;
        var lHandlers = new List<(Action, Action)>();
        QInteractionClick.QInteractionClickAttach(lEnabled);
        QCheckbox.QCheckboxAttach(lEnabled);
        QLook.QLookAttach(lBlack, "PRadio");
        QLook.QLookAttach(lColor, "PRadio");

        RoutedEventHandler lEnabledHandler = (_, _) =>
        {
            if (lBody.Resources[typeof(QInspectorFrame)] is true)
                lEnabledSet(lEnabled.IsChecked == true);
        };
        RoutedEventHandler lBlackHandler = (_, _) =>
        {
            if (lBody.Resources[typeof(QInspectorFrame)] is true) lBlackSet();
        };
        RoutedEventHandler lColorHandler = (_, _) =>
        {
            if (lBody.Resources[typeof(QInspectorFrame)] is true) lColorSet();
        };
        RoutedEventHandler lPickHandler = (_, _) =>
        {
            if (lBody.Resources[typeof(QInspectorFrame)] is true) lPickSet();
        };
        Action lSwitchAttach = () =>
        {
            lEnabled.Checked -= lEnabledHandler;
            lEnabled.Unchecked -= lEnabledHandler;
            lBlack.Checked -= lBlackHandler;
            lColor.Checked -= lColorHandler;
            lPicker.Checked -= lPickHandler;
            lPicker.Unchecked -= lPickHandler;
            lEnabled.Checked += lEnabledHandler;
            lEnabled.Unchecked += lEnabledHandler;
            lBlack.Checked += lBlackHandler;
            lColor.Checked += lColorHandler;
            lPicker.Checked += lPickHandler;
            lPicker.Unchecked += lPickHandler;
        };
        Action lSwitchDetach = () =>
        {
            lEnabled.Checked -= lEnabledHandler;
            lEnabled.Unchecked -= lEnabledHandler;
            lBlack.Checked -= lBlackHandler;
            lColor.Checked -= lColorHandler;
            lPicker.Checked -= lPickHandler;
            lPicker.Unchecked -= lPickHandler;
        };
        lHandlers.Add((lSwitchAttach, lSwitchDetach));

        foreach ((Slider lSlider, Func<double> lReset, Action<double> lSet) in lSliders)
        {
            QSlider.QSliderAttach(lSlider);
            QSlider.QSliderResetAttach(lSlider, lReset);
            RoutedPropertyChangedEventHandler<double> lHandler = (_, _) =>
            {
                if (lBody.Resources[typeof(QInspectorFrame)] is true) lSet(lSlider.Value);
            };
            Action lAttach = () =>
            {
                lSlider.ValueChanged -= lHandler;
                lSlider.ValueChanged += lHandler;
            };
            Action lDetach = () => lSlider.ValueChanged -= lHandler;
            lHandlers.Add((lAttach, lDetach));
        }

        foreach ((TextBox lValue, Action lCommit) in lValues)
        {
            QTextbox.QTextboxAttach(lValue);
            lValue.PreviewTextInput += (_, lInput) =>
                lInput.Handled = !QInspectorField.QInspectorDecimalCheck(lInput.Text);
            RoutedEventHandler lLost = (_, _) =>
            {
                if (lBody.Resources[typeof(QInspectorFrame)] is true) lCommit();
            };
            KeyEventHandler lKey = (_, lInput) =>
            {
                if (lInput.Key == Key.Enter && lBody.Resources[typeof(QInspectorFrame)] is true) lCommit();
                lInput.Handled = lInput.Key == Key.Enter;
            };
            Action lAttach = () =>
            {
                lValue.LostFocus -= lLost;
                lValue.KeyDown -= lKey;
                lValue.LostFocus += lLost;
                lValue.KeyDown += lKey;
            };
            Action lDetach = () =>
            {
                lValue.LostFocus -= lLost;
                lValue.KeyDown -= lKey;
            };
            lHandlers.Add((lAttach, lDetach));
        }

        MouseButtonEventHandler lPressHandler = (_, lMouse) =>
        {
            if (lBody.Resources[typeof(QInspectorFrame)] is true) lPress(lMouse);
        };
        MouseEventHandler lMoveHandler = (_, lMouse) =>
        {
            if (lBody.Resources[typeof(QInspectorFrame)] is true) lMove(lMouse);
        };
        Action lWheelAttach = () =>
        {
            lWheel.MouseLeftButtonDown -= lPressHandler;
            lWheel.MouseMove -= lMoveHandler;
            lWheel.MouseLeftButtonDown += lPressHandler;
            lWheel.MouseMove += lMoveHandler;
        };
        Action lWheelDetach = () =>
        {
            lWheel.MouseLeftButtonDown -= lPressHandler;
            lWheel.MouseMove -= lMoveHandler;
        };
        lHandlers.Add((lWheelAttach, lWheelDetach));
        lWheel.MouseLeftButtonUp += (_, _) => lWheel.ReleaseMouseCapture();
        lBody.Resources[typeof(QBlankLifecycle)] = lHandlers;
        QBlankLifecycleAttach();
    }

    public void QBlankLifecycleAttach()
    {
        foreach ((Action lAttach, _) in
                 (List<(Action, Action)>)qBlankLifecycle.Resources[typeof(QBlankLifecycle)])
        {
            lAttach();
        }

        qBlankLifecycle.Resources[typeof(QInspectorFrame)] = true;
    }

    public void QBlankLifecycleDetach()
    {
        qBlankLifecycle.Resources[typeof(QInspectorFrame)] = false;
        foreach ((_, Action lDetach) in
                 (List<(Action, Action)>)qBlankLifecycle.Resources[typeof(QBlankLifecycle)])
        {
            lDetach();
        }
    }
}
