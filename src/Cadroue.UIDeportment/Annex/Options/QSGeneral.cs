using Cadroue.Core;
using Cadroue.Conduct;
using System.Windows;
using System.Windows.Controls;

namespace Cadroue.UIDeportment;

public sealed record QSGeneralSnapshot(
    string QSGeneralSnapshotStartup,
    IReadOnlyList<string> QSGeneralSnapshotSelection,
    bool QSGeneralSnapshotRecent,
    bool QSGeneralSnapshotSafety,
    bool QSGeneralSnapshotVacancy,
    bool QSGeneralSnapshotOrientation,
    string QSGeneralSnapshotLanguage);

public sealed class QSGeneral
{
    private readonly CSOptions? csOptions;

    public QSGeneral(QSGeneralSnapshot snapshot)
    {
        QSGeneralUpdate(snapshot);
    }

    public QSGeneral(QSGeneralSnapshot snapshot, CSOptions options)
    {
        csOptions = options;
        QSGeneralUpdate(snapshot);
    }

    private FrameworkElement? PSGeneral { get; set; }

    public QSGeneralSnapshot QSGeneralSnapshot { get; private set; } = null!;

    public FrameworkElement QSGeneralAttach(FrameworkElement surface)
    {
        QSGeneralDetach();
        PSGeneral = surface;
        QSGeneralUpdate(
            surface,
            new Queue<string>(
            [
                "Options.General.Startup",
                "Options.General.OpenWith",
                "Options.General.LastMedia",
                "Options.General.Confirm",
                "Options.General.DestructiveActions",
                "Options.General.DestructiveNotice",
                "Options.General.Relay",
                "Options.General.RelayClear",
                "Options.General.RelayClearNotice",
                "Options.General.Layout",
                "Options.General.Tabs",
                "Options.General.Language",
                "Options.General.Language"
            ]));

        ContentControl startup = QContract.QContractFind<ContentControl>(surface, "PSGeneralStartup");
        Control selection = QContract.QContractFind<Control>(surface, "PSGeneralSelection");
        CheckBox recent = QContract.QContractFind<CheckBox>(surface, "PSGeneralRecent");
        CheckBox safety = QContract.QContractFind<CheckBox>(surface, "PSGeneralSafety");
        CheckBox vacancy = QContract.QContractFind<CheckBox>(surface, "PSGeneralVacancy");
        ContentControl orientation = QContract.QContractFind<ContentControl>(surface, "PSGeneralOrientation");
        ComboBox language = QContract.QContractFind<ComboBox>(surface, "PSGeneralLanguage");

        QSGeneralUpdate(
            startup,
            QSGeneralSnapshot.QSGeneralSnapshotStartup,
            token =>
            {
                QSGeneralUpdate(QSGeneralSnapshot with { QSGeneralSnapshotStartup = token });
                if (csOptions is not null)
                {
                    ((LOptionsPort)csOptions).CSOptionsUpdate(token);
                }
                QSGeneralUpdate();
            },
            new KeyValuePair<string, string>("LastSession", "Options.Startup.LastSession"),
            new KeyValuePair<string, string>("DefaultTab", "Options.Startup.DefaultTab"));
        if (selection.Style is null)
        {
            QPickerControl.QPickerControlAttach(
                selection,
                QPicker.QPickerItemsCreate(
                    [
                        new Cadroue.Application.LLocalizationChoice("Split", "Tab.Split"),
                        new Cadroue.Application.LLocalizationChoice("Edit", "Tab.Edit"),
                        new Cadroue.Application.LLocalizationChoice("Audio", "Tab.Audio"),
                        new Cadroue.Application.LLocalizationChoice("Convert", "Tab.Convert"),
                        new Cadroue.Application.LLocalizationChoice("Merge", "Tab.Merge"),
                        new Cadroue.Application.LLocalizationChoice("Worklist", "Tab.Worklist")
                    ],
                    QSGeneralSnapshot.QSGeneralSnapshotSelection),
                QLocalizationCatalog.QLocalizationTextRead("Options.Startup.NoTab"));
        }

        recent.IsChecked = QSGeneralSnapshot.QSGeneralSnapshotRecent;
        safety.IsChecked = QSGeneralSnapshot.QSGeneralSnapshotSafety;
        vacancy.IsChecked = QSGeneralSnapshot.QSGeneralSnapshotVacancy;
        recent.Content = QLocalizationCatalog.QLocalizationTextRead("Options.Startup.OpenLastMedia");
        safety.Content = QLocalizationCatalog.QLocalizationTextRead("Options.Confirm.Ask");
        vacancy.Content = QLocalizationCatalog.QLocalizationTextRead("Options.Relay.ClearCheck");
        if (recent.Style is null)
        {
            QCheckbox.QCheckboxAttach(recent);
            QCheckbox.QCheckboxAttach(safety);
            QCheckbox.QCheckboxAttach(vacancy);
        }

        QSGeneralUpdate(
            orientation,
            QSGeneralSnapshot.QSGeneralSnapshotOrientation ? "Vertical" : "Horizontal",
            token =>
            {
                bool vertical = string.Equals(token, "Vertical", StringComparison.Ordinal);
                QSGeneralUpdate(QSGeneralSnapshot with
                {
                    QSGeneralSnapshotOrientation = vertical
                });
                if (csOptions is not null)
                {
                    ((LOptionsPort)csOptions).CSOptionsUpdate(vertical);
                }
            },
            new KeyValuePair<string, string>("Horizontal", "Options.Layout.TabsHorizontal"),
            new KeyValuePair<string, string>("Vertical", "Options.Layout.TabsVertical"));

        language.ItemsSource = Cadroue.Application.LLocalization.LLocalizationLanguagesRead();
        language.DisplayMemberPath = "Value";
        language.SelectedValuePath = "Key";
        language.SelectedValue = QSGeneralSnapshot.QSGeneralSnapshotLanguage;
        language.SelectedIndex = Math.Max(language.SelectedIndex, 0);
        QSGeneralUpdate(QSGeneralSnapshot with
        {
            QSGeneralSnapshotLanguage = (string?)language.SelectedValue ?? string.Empty
        });
        if (language.Style is null)
        {
            QDropdown.QDropdownAttach(language);
        }

        foreach (RadioButton radio in ((Panel)((Border)startup.Content).Child).Children.OfType<RadioButton>())
        {
            radio.Checked += QSGeneralUpdate;
        }
        foreach (RadioButton radio in ((Panel)((Border)orientation.Content).Child).Children.OfType<RadioButton>())
        {
            radio.Checked += QSGeneralUpdate;
        }
        return surface;
    }

    public void QSGeneralDetach()
    {
        if (PSGeneral is null)
        {
            return;
        }

        _ = QSGeneralSnapshotRead();
        ContentControl startup = QContract.QContractFind<ContentControl>(PSGeneral, "PSGeneralStartup");
        ContentControl orientation = QContract.QContractFind<ContentControl>(PSGeneral, "PSGeneralOrientation");
        foreach (RadioButton radio in ((Panel)((Border)startup.Content).Child).Children.OfType<RadioButton>())
        {
            radio.Checked -= QSGeneralUpdate;
        }
        foreach (RadioButton radio in ((Panel)((Border)orientation.Content).Child).Children.OfType<RadioButton>())
        {
            radio.Checked -= QSGeneralUpdate;
        }
        PSGeneral = null;
    }

    public QSGeneralSnapshot QSGeneralSnapshotRead()
    {
        if (PSGeneral is null)
        {
            return QSGeneralSnapshot;
        }

        Control selection = QContract.QContractFind<Control>(PSGeneral, "PSGeneralSelection");
        CheckBox recent = QContract.QContractFind<CheckBox>(PSGeneral, "PSGeneralRecent");
        CheckBox safety = QContract.QContractFind<CheckBox>(PSGeneral, "PSGeneralSafety");
        CheckBox vacancy = QContract.QContractFind<CheckBox>(PSGeneral, "PSGeneralVacancy");
        ComboBox language = QContract.QContractFind<ComboBox>(PSGeneral, "PSGeneralLanguage");

        IReadOnlyList<string> selectionValue = QPickerControl.QPickerControlRead(selection);
        bool recentValue = recent.IsChecked == true;
        bool safetyValue = safety.IsChecked == true;
        bool vacancyValue = vacancy.IsChecked == true;
        string languageValue = (string?)language.SelectedValue ?? string.Empty;

        QSGeneralUpdate(new QSGeneralSnapshot(
            QSGeneralSnapshot.QSGeneralSnapshotStartup,
            selectionValue,
            recentValue,
            safetyValue,
            vacancyValue,
            QSGeneralSnapshot.QSGeneralSnapshotOrientation,
            languageValue));
        if (csOptions is not null)
        {
            ((LOptionsPort)csOptions).CSOptionsUpdate(
                selectionValue,
                recentValue,
                safetyValue,
                vacancyValue,
                languageValue);
        }
        return QSGeneralSnapshot;
    }

    private void QSGeneralUpdate(object sender, RoutedEventArgs e)
    {
        ((Action)((FrameworkElement)sender).DataContext).Invoke();
    }

    private void QSGeneralUpdate(QSGeneralSnapshot snapshot) => QSGeneralSnapshot = snapshot with
    {
        QSGeneralSnapshotSelection = Array.AsReadOnly(snapshot.QSGeneralSnapshotSelection.ToArray())
    };

    private void QSGeneralUpdate()
    {
        if (PSGeneral is not null)
        {
            QContract.QContractFind<Control>(PSGeneral, "PSGeneralSelection").Visibility =
                string.Equals(
                    QSGeneralSnapshot.QSGeneralSnapshotStartup,
                    "DefaultTab",
                    StringComparison.Ordinal)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        }
    }

    private static void QSGeneralUpdate(
        ContentControl host,
        string selected,
        Action<string> changed,
        params KeyValuePair<string, string>[] choices)
    {
        string token = choices
            .Select(choice => choice.Key)
            .FirstOrDefault(choice => string.Equals(choice, selected, StringComparison.Ordinal))
            ?? choices.FirstOrDefault().Key
            ?? string.Empty;
        RadioButton[] radios = choices
            .Select(choice => new RadioButton
            {
                Tag = choice.Key,
                Content = QLocalizationCatalog.QLocalizationTextRead(choice.Value),
                IsChecked = string.Equals(choice.Key, token, StringComparison.Ordinal),
                DataContext = (Action)(() => changed(choice.Key))
            })
            .ToArray();
        Border segment = QRadio.QRadioSegmentBuild(radios);
        segment.Height = 32;
        segment.HorizontalAlignment = HorizontalAlignment.Left;
        host.Content = segment;
        changed(token);
    }

    private static void QSGeneralUpdate(DependencyObject surface, Queue<string> keys)
    {
        if (surface is TextBlock text)
        {
            text.Text = QLocalizationCatalog.QLocalizationTextRead(keys.Dequeue());
        }

        foreach (object child in LogicalTreeHelper.GetChildren(surface))
        {
            if (child is DependencyObject element)
            {
                QSGeneralUpdate(element, keys);
            }
        }
    }
}
