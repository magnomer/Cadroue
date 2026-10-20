using System.Windows;
using System.Windows.Controls;

namespace Cadroue.UIDeportment;

public sealed class QSEncoderOutput
{
    private readonly UserControl qsEncoderOutputSurface;
    private readonly QSEncoder qsEncoderOutputOwner;
    private readonly QSEncoderSuffix qsEncoderOutputSuffix;
    private readonly QSEncoderLocation qsEncoderOutputLocation;
    private readonly QSEncoderName qsEncoderOutputName;
    private readonly Action<string> qsEncoderOutputContainer;

    public QSEncoderOutput(UserControl lSurface, QSEncoder lOwner, Action<string> lContainerChanged)
    {
        qsEncoderOutputSurface = lSurface;
        qsEncoderOutputOwner = lOwner;
        qsEncoderOutputSuffix = lOwner.QSEncoderSuffix;
        qsEncoderOutputLocation = lOwner.QSEncoderLocation;
        qsEncoderOutputContainer = lContainerChanged;
        (string lDisplay, string lContainer, string lExtension, string lCollision, string lLocation) =
            lOwner.QSEncoderOutputRead();
        qsEncoderOutputName = new QSEncoderName(QSEncoderOutputName, lOwner.QSEncoderTokenCreate(), lDisplay);

        QSEncoderOutputContainer.ItemsSource = new[]
        {
            new QSEncoderChoice(
                "Same as source",
                QLocalizationCatalog.QLocalizationTextRead("Encoder.Location.Source")),
            new QSEncoderChoice("MP4", QLocalizationCatalog.QLocalizationTextRead("Encoder.Container.MP4")),
            new QSEncoderChoice("Matroska", QLocalizationCatalog.QLocalizationTextRead("Encoder.Container.Matroska")),
            new QSEncoderChoice("MOV", QLocalizationCatalog.QLocalizationTextRead("Encoder.Container.MOV")),
            new QSEncoderChoice("WebM", QLocalizationCatalog.QLocalizationTextRead("Encoder.Container.WebM")),
            new QSEncoderChoice("AVI", QLocalizationCatalog.QLocalizationTextRead("Encoder.Container.AVI")),
            new QSEncoderChoice("MPEG-TS", QLocalizationCatalog.QLocalizationTextRead("Encoder.Container.TS")),
            new QSEncoderChoice("FLV", QLocalizationCatalog.QLocalizationTextRead("Encoder.Container.FLV")),
            new QSEncoderChoice("Ogg", QLocalizationCatalog.QLocalizationTextRead("Encoder.Container.Ogg"))
        };
        QSEncoderOutputContainer.SelectedItem = QSEncoderOutputContainer.Items.Cast<QSEncoderChoice>()
            .FirstOrDefault(lChoice => lChoice.QSEncoderChoiceToken == lContainer);
        QDropdown.QDropdownAttach(QSEncoderOutputContainer);

        QSEncoderOutputCollision.ItemsSource = new[]
        {
            new QSEncoderChoice("Overwrite", QLocalizationCatalog.QLocalizationTextRead("Encoder.Collision.Overwrite")),
            new QSEncoderChoice(
                "Rename output",
                QLocalizationCatalog.QLocalizationTextRead("Encoder.Collision.RenameOutput")),
            new QSEncoderChoice(
                "Rename existing",
                QLocalizationCatalog.QLocalizationTextRead("Encoder.Collision.RenameExisting"))
        };
        QSEncoderOutputCollision.SelectedItem = QSEncoderOutputCollision.Items.Cast<QSEncoderChoice>()
            .FirstOrDefault(lChoice => lChoice.QSEncoderChoiceToken == lCollision);
        QDropdown.QDropdownAttach(QSEncoderOutputCollision);

        QSEncoderOutputMode.ItemsSource = qsEncoderOutputLocation.QSEncoderLocationModes
            .Select(lChoice => new QSEncoderChoice(
                lChoice.Item1, QLocalizationCatalog.QLocalizationTextRead(lChoice.Item2))).ToArray();
        QSEncoderOutputMode.SelectedItem = QSEncoderOutputMode.Items.Cast<QSEncoderChoice>()
            .FirstOrDefault(lChoice => lChoice.QSEncoderChoiceToken == lLocation);
        QDropdown.QDropdownAttach(QSEncoderOutputMode);
        QSEncoderOutputFolder.Text = qsEncoderOutputLocation.QSEncoderLocationRead(lLocation);
        QSOutputSuffixBox.Text = qsEncoderOutputSuffix.QSEncoderSuffixRead(lCollision);
        QSEncoderOutputApply(lExtension);
        QSEncoderOutputBuild();
    }

    private UserControl QSEncoderOutputName =>
        QContract.QContractFind<UserControl>(qsEncoderOutputSurface, "psOutputName");
    private ComboBox QSEncoderOutputMode =>
        QContract.QContractFind<ComboBox>(qsEncoderOutputSurface, "psOutputLocationMode");
    private TextBlock QSEncoderOutputStatus =>
        QContract.QContractFind<TextBlock>(qsEncoderOutputSurface, "psOutputLocationStatus");
    private UIElement QSOutputFolderRow =>
        QContract.QContractFind<UIElement>(qsEncoderOutputSurface, "psOutputFolderRow");
    private TextBlock QSOutputFolderCaption =>
        QContract.QContractFind<TextBlock>(qsEncoderOutputSurface, "psOutputFolderCaption");
    private TextBox QSEncoderOutputFolder =>
        QContract.QContractFind<TextBox>(qsEncoderOutputSurface, "psOutputFolderBox");
    private Button QSEncoderOutputBrowse =>
        QContract.QContractFind<Button>(qsEncoderOutputSurface, "psOutputFolderBrowse");
    private ComboBox QSEncoderOutputContainer =>
        QContract.QContractFind<ComboBox>(qsEncoderOutputSurface, "psOutputContainerSelection");
    private ComboBox QSEncoderOutputExtension =>
        QContract.QContractFind<ComboBox>(qsEncoderOutputSurface, "psOutputExtensionSelection");
    private ComboBox QSEncoderOutputCollision =>
        QContract.QContractFind<ComboBox>(qsEncoderOutputSurface, "psOutputCollisionSelection");
    private UIElement QSOutputSuffixRow =>
        QContract.QContractFind<UIElement>(qsEncoderOutputSurface, "psOutputSuffixRow");
    private TextBlock QSOutputSuffixCaption =>
        QContract.QContractFind<TextBlock>(qsEncoderOutputSurface, "psOutputSuffixCaption");
    private TextBox QSOutputSuffixBox =>
        QContract.QContractFind<TextBox>(qsEncoderOutputSurface, "psOutputSuffixBox");

    public string QSEncoderOutputRead() => QSEncoderChoice.QSEncoderChoiceRead(QSEncoderOutputContainer.SelectedItem);

    public void QSEncoderOutputSave() => qsEncoderOutputOwner.QSEncoderOutputSet(
        qsEncoderOutputName.QSEncoderNameRead(),
        QSEncoderOutputRead(),
        QSEncoderChoice.QSEncoderChoiceRead(QSEncoderOutputExtension.SelectedItem),
        QSEncoderChoice.QSEncoderChoiceRead(QSEncoderOutputCollision.SelectedItem),
        QSOutputSuffixBox.Text,
        QSEncoderChoice.QSEncoderChoiceRead(QSEncoderOutputMode.SelectedItem),
        QSEncoderOutputFolder.Text);

    private void QSEncoderOutputBuild()
    {
        QContract.QContractFind<TextBlock>(qsEncoderOutputSurface, "psOutputLocationCaption").Text =
            QLocalizationCatalog.QLocalizationTextRead("Encoder.Field.Output.Location");
        QContract.QContractFind<TextBlock>(qsEncoderOutputSurface, "psOutputContainerCaption").Text =
            QLocalizationCatalog.QLocalizationTextRead("Encoder.Field.Output.Container");
        QContract.QContractFind<TextBlock>(qsEncoderOutputSurface, "psOutputExtensionCaption").Text =
            QLocalizationCatalog.QLocalizationTextRead("Encoder.Field.Output.Extension");
        QContract.QContractFind<TextBlock>(qsEncoderOutputSurface, "psOutputCollisionCaption").Text =
            QLocalizationCatalog.QLocalizationTextRead("Encoder.Field.Output.Collision");
        QDropdown.QDropdownAttach(QSEncoderOutputExtension);
        QSEncoderOutputContainer.SelectionChanged += (_, _) => QSEncoderOutputHandle();
        QSEncoderOutputCollision.SelectionChanged += (_, _) => QSEncoderOutputUpdate();
        QSOutputSuffixBox.LostFocus += (_, _) => QSEncoderOutputNormalize();
        QSEncoderOutputMode.SelectionChanged += (_, _) => QSEncoderOutputShow();
        QSEncoderOutputBrowse.Click += (_, _) => QSEncoderOutputSelect();
        QSEncoderOutputUpdate();
        QSEncoderOutputShow();
    }

    private void QSEncoderOutputHandle()
    {
        QSEncoderOutputApply(QSEncoderChoice.QSEncoderChoiceRead(QSEncoderOutputExtension.SelectedItem));
        qsEncoderOutputContainer(QSEncoderOutputRead());
    }

    private void QSEncoderOutputApply(string lKeep)
    {
        IReadOnlyList<string> lExtensions = qsEncoderOutputOwner.QSEncoderOutputRead(QSEncoderOutputRead());
        string[] lItems = lExtensions.Count == 0 ? [string.Empty] : lExtensions.ToArray();
        QSEncoderOutputExtension.ItemsSource = lItems;
        QSEncoderOutputExtension.SelectedItem = lItems.Contains(lKeep) ? lKeep : lItems.FirstOrDefault();
    }

    private void QSEncoderOutputUpdate()
    {
        string lPolicy = QSEncoderChoice.QSEncoderChoiceRead(QSEncoderOutputCollision.SelectedItem);
        string lSuffix = qsEncoderOutputSuffix.QSEncoderSuffixSelect(lPolicy, QSOutputSuffixBox.Text);
        string? lLabel = QSEncoderSuffix.QSEncoderSuffixResolve(lPolicy);
        QSOutputSuffixBox.Text = lSuffix;
        QSOutputSuffixRow.Visibility = lLabel is null ? Visibility.Collapsed : Visibility.Visible;
        if (lLabel is not null) QSOutputSuffixCaption.Text = QLocalizationCatalog.QLocalizationTextRead(lLabel);
    }

    private void QSEncoderOutputNormalize() => QSOutputSuffixBox.Text =
        qsEncoderOutputSuffix.QSEncoderSuffixNormalize(
            QSEncoderChoice.QSEncoderChoiceRead(QSEncoderOutputCollision.SelectedItem),
            QSOutputSuffixBox.Text);

    private void QSEncoderOutputShow() => qsEncoderOutputLocation.QSEncoderLocationUpdate(
        QSEncoderChoice.QSEncoderChoiceRead(QSEncoderOutputMode.SelectedItem),
        QSEncoderOutputFolder,
        QSOutputFolderRow,
        QSEncoderOutputBrowse,
        QSOutputFolderCaption,
        QSEncoderOutputStatus);

    private void QSEncoderOutputSelect()
    {
        string lCurrent = QSEncoderOutputFolder.Text.Trim();
        var lDialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = QLocalizationCatalog.QLocalizationTextRead("Encoder.Location.Choose"),
            InitialDirectory = qsEncoderOutputLocation.QSEncoderInitialResolve(lCurrent)
        };
        if (lDialog.ShowDialog() == true) QSEncoderOutputFolder.Text = lDialog.FolderName;
    }
}
