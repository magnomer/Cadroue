using System.Windows.Controls;

namespace Cadroue.UIDeportment;

// The Skip section's surface wiring: pulls its passive sheet, relays both switches, and paints gate state.
public sealed class QSkipSection
{
    private readonly CheckBox qSkipSectionActive;
    private readonly QSkip qSkip;

    public QSkipSection(QSkip lSkip)
    {
        qSkip = lSkip;
        QSkipSectionBody = QContract.QContractSheetFind<StackPanel>("PSkip");
        QSkipSectionPersistent = QContract.QContractSheetFind<CheckBox>("PSkipPersistent");
        qSkipSectionActive = QContract.QContractFind<CheckBox>(QSkipSectionBody, "PSkipActive");
        TextBlock lNote = QContract.QContractFind<TextBlock>(QSkipSectionBody, "PSkipNote");

        qSkipSectionActive.Content = QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Apply");
        qSkipSectionActive.ToolTip = QLocalizationCatalog.QLocalizationTextRead("Inspector.Skip.ApplyTooltip");
        QSkipSectionPersistent.Content = QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Persistent");
        QSkipSectionPersistent.ToolTip =
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Skip.PersistentTooltip");
        lNote.Text = QLocalizationCatalog.QLocalizationTextRead("Inspector.Skip.Note");

        QTool.QToolSwitchAttach(qSkipSectionActive, qSkip.QSkipActiveSet);
        QTool.QToolSwitchAttach(QSkipSectionPersistent, qSkip.QSkipPersistentSet);
    }

    public StackPanel QSkipSectionBody { get; }

    public CheckBox QSkipSectionPersistent { get; }

    public void QSkipSectionAttach() => qSkip.QSkipChanged += QSkipSectionUpdate;

    public void QSkipSectionUpdate()
    {
        QTool.QToolSwitchUpdate(qSkipSectionActive, qSkip.QSkipActive);
        QTool.QToolSwitchUpdate(QSkipSectionPersistent, qSkip.QSkipPersistent);
    }
}
