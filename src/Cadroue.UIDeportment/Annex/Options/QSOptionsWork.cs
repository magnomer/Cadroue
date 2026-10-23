using Cadroue.Conduct;
using Cadroue.Core;
using System.Windows;
using System.Windows.Controls;

namespace Cadroue.UIDeportment;

public sealed class QSOptionsWork
{
    private readonly CSOptions csOptions;
    private readonly QSOptionsField qsOptionsField = new();
    private readonly Delegate[] qsOptionsWork;
    private FrameworkElement? psWork;
    private Func<CSOptionsWork>? qsOptionsWorkValue;

    public QSOptionsWork(CSOptions options)
    {
        csOptions = options;
        qsOptionsWork =
        [
            new RoutedEventHandler(QSOptionsWorkToggle),
            new SelectionChangedEventHandler((sender, _) =>
                QSOptionsWorkSelect(sender, LCustodyFamily.LCustodyFamilyEncode)),
            new SelectionChangedEventHandler((sender, _) =>
                QSOptionsWorkSelect(sender, LCustodyFamily.LCustodyFamilyKeyframe)),
            new SelectionChangedEventHandler((sender, _) =>
                QSOptionsWorkSelect(sender, LCustodyFamily.LCustodyFamilyWaveform))
        ];
    }

    public FrameworkElement QSOptionsWorkAttach(FrameworkElement surface)
    {
        QSOptionsWorkDetach();
        psWork = surface;
        LOptionsSnapshot snapshot = ((LOptionsPort)csOptions).CSOptionsSnapshotRead();
        QContract.QContractFind<TextBlock>(surface, "PSWorkFailure").Text =
            QLocalizationCatalog.QLocalizationTextRead("Options.Work.Failure");
        QContract.QContractFind<TextBlock>(surface, "PSWorkReaction").Text =
            QLocalizationCatalog.QLocalizationTextRead("Options.Work.OnFailure");
        QContract.QContractFind<TextBlock>(surface, "PSWorkRetry").Text =
            QLocalizationCatalog.QLocalizationTextRead("Options.Work.Retry");
        QContract.QContractFind<TextBlock>(surface, "PSWorkMaximum").Text =
            QLocalizationCatalog.QLocalizationTextRead("Options.Work.RetryLimit");

        CheckBox retry = QContract.QContractFind<CheckBox>(surface, "PSWorkRepeat");
        CheckBox paused = QContract.QContractFind<CheckBox>(surface, "PSWorkPaused");
        QSOptionsField.QSOptionsFieldPrepare(
            paused,
            QLocalizationCatalog.QLocalizationTextRead("Options.Work.FailurePause"),
            snapshot.CSOptionsFailure);
        QSOptionsField.QSOptionsFieldPrepare(
            retry,
            QLocalizationCatalog.QLocalizationTextRead("Options.Work.RetryCheck"),
            snapshot.CSOptionsRetry);
        Slider maximum = QContract.QContractFind<Slider>(surface, "PSWorkSlider");
        QSOptionsField.QSOptionsFieldPrepare(maximum, snapshot.CSOptionsRetryMaximum);
        qsOptionsField.QSOptionsFieldAttach(
            maximum,
            QContract.QContractFind<TextBlock>(surface, "PSWorkReadout"),
            string.Empty);
        QSOptionsWorkUpdate();
        retry.Checked += (RoutedEventHandler)qsOptionsWork[0];
        retry.Unchecked += (RoutedEventHandler)qsOptionsWork[0];

        QContract.QContractFind<TextBlock>(surface, "PSWorkPriority").Text = QSOptions.QSOptionsLevelTitle;
        QSOptionsWorkAttach(
            QContract.QContractFind<TextBlock>(surface, "PSWorkEncoding"),
            QContract.QContractFind<ComboBox>(surface, "PSWorkEncoder"),
            (SelectionChangedEventHandler)qsOptionsWork[1],
            "Options.Work.PriorityEncode",
            snapshot.CSOptionsEncodeLevel);
        QSOptionsWorkAttach(
            QContract.QContractFind<TextBlock>(surface, "PSWorkKeyframing"),
            QContract.QContractFind<ComboBox>(surface, "PSWorkKeyframer"),
            (SelectionChangedEventHandler)qsOptionsWork[2],
            "Options.Work.PriorityKeyframe",
            snapshot.CSOptionsKeyframeLevel);
        QSOptionsWorkAttach(
            QContract.QContractFind<TextBlock>(surface, "PSWorkSampling"),
            QContract.QContractFind<ComboBox>(surface, "PSWorkSampler"),
            (SelectionChangedEventHandler)qsOptionsWork[3],
            "Options.Work.PriorityWaveform",
            snapshot.CSOptionsWaveformLevel);
        qsOptionsWorkValue = () => new CSOptionsWork(
            QSOptionsField.QSOptionsFieldRead(paused),
            QSOptionsField.QSOptionsFieldRead(retry),
            maximum.Value);
        return surface;
    }

    public void QSOptionsWorkDetach()
    {
        if (psWork is null)
        {
            return;
        }
        CheckBox retry = QContract.QContractFind<CheckBox>(psWork, "PSWorkRepeat");
        retry.Checked -= (RoutedEventHandler)qsOptionsWork[0];
        retry.Unchecked -= (RoutedEventHandler)qsOptionsWork[0];
        QContract.QContractFind<ComboBox>(psWork, "PSWorkEncoder").SelectionChanged -=
            (SelectionChangedEventHandler)qsOptionsWork[1];
        QContract.QContractFind<ComboBox>(psWork, "PSWorkKeyframer").SelectionChanged -=
            (SelectionChangedEventHandler)qsOptionsWork[2];
        QContract.QContractFind<ComboBox>(psWork, "PSWorkSampler").SelectionChanged -=
            (SelectionChangedEventHandler)qsOptionsWork[3];
        qsOptionsField.QSOptionsFieldDetach();
        psWork = null;
    }

    public CSOptionsWork QSOptionsWorkRead() => qsOptionsWorkValue!();

    private static void QSOptionsWorkAttach(
        TextBlock label,
        ComboBox combo,
        SelectionChangedEventHandler select,
        string key,
        int level)
    {
        label.Text = QLocalizationCatalog.QLocalizationTextRead(key);
        string[] names = QSOptions.QSOptionsLevelNames;
        combo.ItemsSource = names;
        combo.SelectedItem = names.ElementAtOrDefault(level);
        QDropdown.QDropdownAttach(combo);
        combo.SelectionChanged += select;
    }

    private void QSOptionsWorkToggle(object sender, RoutedEventArgs _) => QSOptionsWorkUpdate();

    private void QSOptionsWorkUpdate() =>
        QContract.QContractFind<UIElement>(psWork!, "PSWorkLimit").IsEnabled =
            QSOptionsField.QSOptionsFieldRead(QContract.QContractFind<CheckBox>(psWork!, "PSWorkRepeat"));

    private void QSOptionsWorkSelect(object sender, LCustodyFamily family) =>
        ((LOptionsPort)csOptions).CSOptionsLevelSet(family, ((ComboBox)sender).SelectedIndex);
}
