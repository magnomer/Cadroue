using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

using Xunit;

namespace Cadroue.Tests.Windows;

[Collection("Preference")]
public sealed class TGeneralSurface
{
    [Fact]
    public void Attach_ProjectsDraftAndLanguages()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                System.Windows.Application program = System.Windows.Application.Current
                    ?? new System.Windows.Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                string configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
                _ = System.Reflection.Assembly.LoadFrom(Path.GetFullPath(Path.Combine(
                    AppContext.BaseDirectory,
                    "../../../../../src/Cadroue.UIVeneer/bin",
                    configuration,
                    "net10.0-windows10.0.19041.0/Cadroue.UIVeneer.dll")));
                foreach (string sheet in new[]
                {
                    "Kit/PRadio.xaml",
                    "Kit/PCheckbox.xaml",
                    "Kit/Dropdown/PDropdown.xaml",
                    "Kit/PScrollbar.xaml",
                    "Kit/Picker/PPicker.xaml"
                })
                {
                    program.Resources.MergedDictionaries.Add(new ResourceDictionary
                    {
                        Source = new Uri($"pack://application:,,,/Cadroue.UIVeneer;component/{sheet}")
                    });
                }

                string path = Path.GetFullPath(Path.Combine(
                    AppContext.BaseDirectory,
                    "../../../../../src/Cadroue.UIVeneer/Annex/Options/PSGeneral.xaml"));
                string contract = File.ReadAllText(path)
                    .Replace(" x:Class=\"Cadroue.UIVeneer.PSGeneral\"", string.Empty, StringComparison.Ordinal);
                var surface = (UserControl)XamlReader.Parse(contract);
                string languageToken = LLocalization.LLocalizationLanguagesRead().First().Key;
                var selectionSource = new List<string> { "Edit", "Merge" };
                var inputSnapshot = new QSGeneralSnapshot(
                    "DefaultTab",
                    selectionSource,
                    true,
                    false,
                    true,
                    true,
                    languageToken);
                var general = new QSGeneral(inputSnapshot);
                QSGeneralSnapshot publishedSnapshot = general.QSGeneralSnapshot;

                ((IList<string>)inputSnapshot.QSGeneralSnapshotSelection)[0] = "Audio";
                selectionSource.Add("Split");

                Assert.Same(publishedSnapshot, general.QSGeneralSnapshot);
                Assert.Equal(2, publishedSnapshot.QSGeneralSnapshotSelection.Count);
                Assert.Equal(new[] { "Edit", "Merge" }, publishedSnapshot.QSGeneralSnapshotSelection);

                general.QSGeneralAttach(surface);

                ContentControl startup = QContract.QContractFind<ContentControl>(surface, "PSGeneralStartup");
                RadioButton[] startupRadios = ((Panel)((Border)startup.Content).Child).Children
                    .OfType<RadioButton>()
                    .ToArray();
                Control selection = QContract.QContractFind<Control>(surface, "PSGeneralSelection");
                ContentControl orientation = QContract.QContractFind<ContentControl>(surface, "PSGeneralOrientation");
                RadioButton[] orientationRadios = ((Panel)((Border)orientation.Content).Child).Children
                    .OfType<RadioButton>()
                    .ToArray();
                ComboBox language = QContract.QContractFind<ComboBox>(surface, "PSGeneralLanguage");
                var root = (StackPanel)surface.Content;

                Assert.Equal(
                    QLocalizationCatalog.QLocalizationTextRead("Options.General.Startup"),
                    ((TextBlock)((StackPanel)root.Children[0]).Children[0]).Text);
                Assert.False(startupRadios[0].IsChecked);
                Assert.True(startupRadios[1].IsChecked);
                Assert.Equal(new[] { "Edit", "Merge" }, QPickerControl.QPickerControlRead(selection));
                Assert.True(QContract.QContractFind<CheckBox>(surface, "PSGeneralRecent").IsChecked);
                Assert.False(QContract.QContractFind<CheckBox>(surface, "PSGeneralSafety").IsChecked);
                Assert.True(QContract.QContractFind<CheckBox>(surface, "PSGeneralVacancy").IsChecked);
                Assert.False(orientationRadios[0].IsChecked);
                Assert.True(orientationRadios[1].IsChecked);
                Assert.Equal(LLocalization.LLocalizationLanguagesRead().Count, language.Items.Count);
                Assert.Equal(languageToken, (string)language.SelectedValue);
                general.QSGeneralDetach();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        Assert.Null(failure);
    }

    [Fact]
    public void StartupMode_TogglesPickerWithoutSelectionLoss()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                System.Windows.Application program = System.Windows.Application.Current
                    ?? new System.Windows.Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                string configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
                _ = System.Reflection.Assembly.LoadFrom(Path.GetFullPath(Path.Combine(
                    AppContext.BaseDirectory,
                    "../../../../../src/Cadroue.UIVeneer/bin",
                    configuration,
                    "net10.0-windows10.0.19041.0/Cadroue.UIVeneer.dll")));
                foreach (string sheet in new[]
                {
                    "Kit/PRadio.xaml",
                    "Kit/PCheckbox.xaml",
                    "Kit/Dropdown/PDropdown.xaml",
                    "Kit/PScrollbar.xaml",
                    "Kit/Picker/PPicker.xaml"
                })
                {
                    program.Resources.MergedDictionaries.Add(new ResourceDictionary
                    {
                        Source = new Uri($"pack://application:,,,/Cadroue.UIVeneer;component/{sheet}")
                    });
                }

                string path = Path.GetFullPath(Path.Combine(
                    AppContext.BaseDirectory,
                    "../../../../../src/Cadroue.UIVeneer/Annex/Options/PSGeneral.xaml"));
                string contract = File.ReadAllText(path)
                    .Replace(" x:Class=\"Cadroue.UIVeneer.PSGeneral\"", string.Empty, StringComparison.Ordinal);
                var surface = (UserControl)XamlReader.Parse(contract);
                var general = new QSGeneral(new QSGeneralSnapshot(
                    "DefaultTab",
                    ["Audio", "Worklist"],
                    false,
                    true,
                    false,
                    false,
                    LLocalization.LLocalizationLanguagesRead().First().Key));

                general.QSGeneralAttach(surface);
                ContentControl startup = QContract.QContractFind<ContentControl>(surface, "PSGeneralStartup");
                RadioButton[] radios = ((Panel)((Border)startup.Content).Child).Children
                    .OfType<RadioButton>()
                    .ToArray();
                Control selection = QContract.QContractFind<Control>(surface, "PSGeneralSelection");
                Assert.Equal(Visibility.Visible, selection.Visibility);

                radios[0].IsChecked = true;
                Assert.Equal(Visibility.Collapsed, selection.Visibility);
                Assert.Equal(new[] { "Audio", "Worklist" }, QPickerControl.QPickerControlRead(selection));

                radios[1].IsChecked = true;
                Assert.Equal(Visibility.Visible, selection.Visibility);
                Assert.Equal(new[] { "Audio", "Worklist" }, QPickerControl.QPickerControlRead(selection));
                general.QSGeneralDetach();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        Assert.Null(failure);
    }

    [Fact]
    public void Apply_CopiesSnapshotAfterPendingEdits()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            LPreferenceState? before = null;
            try
            {
                System.Windows.Application program = System.Windows.Application.Current
                    ?? new System.Windows.Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                string configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
                _ = System.Reflection.Assembly.LoadFrom(Path.GetFullPath(Path.Combine(
                    AppContext.BaseDirectory,
                    "../../../../../src/Cadroue.UIVeneer/bin",
                    configuration,
                    "net10.0-windows10.0.19041.0/Cadroue.UIVeneer.dll")));
                foreach (string sheet in new[]
                {
                    "Kit/PRadio.xaml",
                    "Kit/PCheckbox.xaml",
                    "Kit/Dropdown/PDropdown.xaml",
                    "Kit/PScrollbar.xaml",
                    "Kit/Picker/PPicker.xaml"
                })
                {
                    program.Resources.MergedDictionaries.Add(new ResourceDictionary
                    {
                        Source = new Uri($"pack://application:,,,/Cadroue.UIVeneer;component/{sheet}")
                    });
                }

                string path = Path.GetFullPath(Path.Combine(
                    AppContext.BaseDirectory,
                    "../../../../../src/Cadroue.UIVeneer/Annex/Options/PSGeneral.xaml"));
                string contract = File.ReadAllText(path)
                    .Replace(" x:Class=\"Cadroue.UIVeneer.PSGeneral\"", string.Empty, StringComparison.Ordinal);
                var surface = (UserControl)XamlReader.Parse(contract);
                before = TInterface.TPreferenceCurrentRead();
                LPreferenceState draft = Interface.TInterface.TPreferenceClone(before);
                draft.LPreferenceStartupMode = "LastSession";
                draft.LPreferenceStartupTabs = ["Split"];
                draft.LPreferenceMediaAutomatic = false;
                draft.LPreferenceConfirmDestructive = true;
                draft.LPreferenceRelayEmpty = false;
                draft.LPreferenceVerticalTabs = false;
                draft.LPreferenceLanguage = "zz-Unoffered";
                string originalLanguage = draft.LPreferenceLanguage;
                QSOptions options = TInterface.TOptionsCreate(draft, false);
                options.QSOptionsGeneral.QSGeneralAttach(surface);
                Assert.Equal(LLocalization.LLocalizationLanguagesRead().First().Key,
                    (string)QContract.QContractFind<ComboBox>(surface, "PSGeneralLanguage").SelectedValue);

                ContentControl startup = QContract.QContractFind<ContentControl>(surface, "PSGeneralStartup");
                ((Panel)((Border)startup.Content).Child).Children.OfType<RadioButton>().Last().IsChecked = true;
                Control selection = QContract.QContractFind<Control>(surface, "PSGeneralSelection");
                _ = selection.ApplyTemplate();
                Panel rows = (Panel)selection.Template.FindName("PPickerList", selection);
                foreach ((CheckBox row, int index) in rows.Children
                    .OfType<CheckBox>()
                    .Select((row, index) => (row, index)))
                {
                    row.IsChecked = index is 1 or 4;
                }
                QContract.QContractFind<CheckBox>(surface, "PSGeneralRecent").IsChecked = true;
                QContract.QContractFind<CheckBox>(surface, "PSGeneralSafety").IsChecked = false;
                QContract.QContractFind<CheckBox>(surface, "PSGeneralVacancy").IsChecked = true;
                ContentControl orientation = QContract.QContractFind<ContentControl>(surface, "PSGeneralOrientation");
                ((Panel)((Border)orientation.Content).Child).Children.OfType<RadioButton>().Last().IsChecked = true;
                ComboBox language = QContract.QContractFind<ComboBox>(surface, "PSGeneralLanguage");
                language.SelectedIndex = language.Items.Count - 1;
                string languageToken = (string)language.SelectedValue;

                Assert.Equal("LastSession", draft.LPreferenceStartupMode);
                Assert.Equal(new[] { "Split" }, draft.LPreferenceStartupTabs);
                Assert.False(draft.LPreferenceMediaAutomatic);
                Assert.True(draft.LPreferenceConfirmDestructive);
                Assert.False(draft.LPreferenceRelayEmpty);
                Assert.False(draft.LPreferenceVerticalTabs);
                Assert.Equal(originalLanguage, draft.LPreferenceLanguage);

                int saves = 0;
                TInterface.TPreferenceSaveAttach(_ => { saves++; return true; });
                Assert.True(TInterface.TOptionsApply(options));
                Assert.Equal(1, saves);
                Assert.Equal("DefaultTab", draft.LPreferenceStartupMode);
                Assert.Equal(new[] { "Edit", "Merge" }, draft.LPreferenceStartupTabs);
                Assert.True(draft.LPreferenceMediaAutomatic);
                Assert.False(draft.LPreferenceConfirmDestructive);
                Assert.True(draft.LPreferenceRelayEmpty);
                Assert.True(draft.LPreferenceVerticalTabs);
                Assert.Equal(languageToken, draft.LPreferenceLanguage);
                Assert.Equal(languageToken, TInterface.TPreferenceCurrentRead().LPreferenceLanguage);
                options.QSOptionsGeneral.QSGeneralDetach();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
            finally
            {
                TInterface.TPreferenceSaveAttach(null);
                if (before is not null)
                {
                    TInterface.TPreferenceRestore(before);
                }
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        Assert.Null(failure);
    }

    [Fact]
    public void Reattach_DoesNotDuplicateHandlers()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                System.Windows.Application program = System.Windows.Application.Current
                    ?? new System.Windows.Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                string configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
                _ = System.Reflection.Assembly.LoadFrom(Path.GetFullPath(Path.Combine(
                    AppContext.BaseDirectory,
                    "../../../../../src/Cadroue.UIVeneer/bin",
                    configuration,
                    "net10.0-windows10.0.19041.0/Cadroue.UIVeneer.dll")));
                foreach (string sheet in new[]
                {
                    "Kit/PRadio.xaml",
                    "Kit/PCheckbox.xaml",
                    "Kit/Dropdown/PDropdown.xaml",
                    "Kit/PScrollbar.xaml",
                    "Kit/Picker/PPicker.xaml"
                })
                {
                    program.Resources.MergedDictionaries.Add(new ResourceDictionary
                    {
                        Source = new Uri($"pack://application:,,,/Cadroue.UIVeneer;component/{sheet}")
                    });
                }

                string path = Path.GetFullPath(Path.Combine(
                    AppContext.BaseDirectory,
                    "../../../../../src/Cadroue.UIVeneer/Annex/Options/PSGeneral.xaml"));
                string contract = File.ReadAllText(path)
                    .Replace(" x:Class=\"Cadroue.UIVeneer.PSGeneral\"", string.Empty, StringComparison.Ordinal);
                var surface = (UserControl)XamlReader.Parse(contract);
                var general = new QSGeneral(new QSGeneralSnapshot(
                    "LastSession",
                    ["Split"],
                    false,
                    true,
                    false,
                    false,
                    LLocalization.LLocalizationLanguagesRead().First().Key));

                general.QSGeneralAttach(surface);
                general.QSGeneralAttach(surface);
                CheckBox recent = QContract.QContractFind<CheckBox>(surface, "PSGeneralRecent");
                recent.IsChecked = true;
                _ = general.QSGeneralSnapshotRead();
                Assert.True(general.QSGeneralSnapshot.QSGeneralSnapshotRecent);

                general.QSGeneralDetach();
                recent.IsChecked = false;
                Assert.True(general.QSGeneralSnapshot.QSGeneralSnapshotRecent);

                general.QSGeneralAttach(surface);
                recent.IsChecked = false;
                _ = general.QSGeneralSnapshotRead();
                Assert.False(general.QSGeneralSnapshot.QSGeneralSnapshotRecent);
                general.QSGeneralDetach();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        Assert.Null(failure);
    }
}
