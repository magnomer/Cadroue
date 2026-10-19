namespace Cadroue.UIDeportment;

// Program start applying the Kit: the look state first, then each Kit owner's style-keyed class handler.
internal static class QProgramKit
{
    internal static void QProgramKitApply()
    {
        QLook.QLookStateAttach();
        QButton.QButtonHook();
        QCheckbox.QCheckboxHook();
        QTextbox.QTextboxHook();
        QSlider.QSliderHook();
        QScrollbar.QScrollbarHook();
        QPanel.QPanelHook();
        QDropdown.QDropdownHook();
        QDropdownAction.QDropdownActionHook();
        QPickerControl.QPickerControlHook();
        QTokenControl.QTokenControlHook();
    }
}
