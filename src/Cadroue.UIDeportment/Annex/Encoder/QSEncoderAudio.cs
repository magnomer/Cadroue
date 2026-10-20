using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

// The encoder window's audio codec: the chosen encoder and rate, the encoders a container offers, and their trial.
public sealed class QSEncoderAudio
{
    private readonly CSEncoderAudio qsEncoderAudioGate;
    private IReadOnlyList<QSVerdictRow> qsEncoderAudioResults = [];
    private QSEncoderAudioContract? qsEncoderAudioSurface;
    private Action<QSVerdict>? qsEncoderAudioVerdict;
    private QSEncoderSound? qsEncoderAudioSound;

    internal QSEncoderAudio(CSEncoderAudio lAudio)
    {
        qsEncoderAudioGate = lAudio;
        lAudio.CSEncoderAudioChanged += _ =>
        {
            if (qsEncoderAudioSurface is not null)
            {
                QSEncoderAudioApply();
            }
        };
    }

    private QSEncoderAudioContract QSEncoderAudioSurface =>
        qsEncoderAudioSurface ?? throw new InvalidOperationException();

    private string QSEncoderAudioMode =>
        QSEncoderAudioSurface.QSEncoderAudioBar.Tag as string ?? string.Empty;

    public UserControl QSEncoderAudioBuild(UserControl lSurface, Action<QSVerdict> lVerdict) =>
        QSEncoderAudioBuild(lSurface, lVerdict, qsEncoderAudioGate.CSEncoderAudioOpen());

    private UserControl QSEncoderAudioBuild(
        UserControl lSurface,
        Action<QSVerdict> lVerdict,
        (CSEncoderAudioState, string, string, string[], bool) lOpen)
    {
        qsEncoderAudioSurface = new QSEncoderAudioContract(lSurface);
        qsEncoderAudioVerdict = lVerdict;
        var lStreams = new[]
        {
            new QSEncoderChoice(
                "Include first audio track",
                QLocalizationCatalog.QLocalizationTextRead("Encoder.Stream.FirstAudio")),
            new QSEncoderChoice(
                "Include all audio tracks",
                QLocalizationCatalog.QLocalizationTextRead("Encoder.Stream.AllAudio")),
            new QSEncoderChoice("Exclude", QLocalizationCatalog.QLocalizationTextRead("Encoder.Stream.Exclude"))
        };
        QSEncoderAudioSurface.QSEncoderAudioStream.ItemsSource = lStreams;
        QSEncoderAudioSurface.QSEncoderAudioStream.SelectedItem = lStreams.FirstOrDefault(
            lChoice => lChoice.QSEncoderChoiceToken == lOpen.Item2) ?? lStreams[0];
        QDropdown.QDropdownAttach(QSEncoderAudioSurface.QSEncoderAudioStream);
        QSEncoderAudioLoad(lOpen.Item3);
        QSEncoderAudioReset(
            QSEncoderAudioSurface.QSEncoderAudioCodec,
            lOpen.Item4,
            lOpen.Item1.CSEncoderAudioEncoder);
        QSEncoderChip.QSEncoderChipApply(QSEncoderAudioSurface.QSEncoderAudioCodec);
        QDropdown.QDropdownAttach(QSEncoderAudioSurface.QSEncoderAudioCodec);
        QSEncoderAudioReset(
            QSEncoderAudioSurface.QSEncoderAudioControl,
            lOpen.Item1.CSEncoderAudioModes,
            lOpen.Item1.CSEncoderAudioRate);
        QDropdown.QDropdownAttach(QSEncoderAudioSurface.QSEncoderAudioControl);
        qsEncoderAudioSound = new QSEncoderSound(
            QSEncoderAudioSurface.QSEncoderAudioRate,
            QSEncoderAudioSurface.QSEncoderAudioSample,
            QSEncoderAudioSurface.QSEncoderAudioChannel,
            this);

        Button lVerification = QButton.QButtonBuild(
            QContract.QContractFind<Button>(lSurface, "psAudioCodecVerification"), QButtonLook.QButtonLookWhite);
        lVerification.Content = QLocalizationCatalog.QLocalizationTextRead("Encoder.Button.Verify");
        Button lResult = QButton.QButtonBuild(
            QContract.QContractFind<Button>(lSurface, "psAudioCodecResult"), QButtonLook.QButtonLookWhite);
        lResult.Content = QLocalizationCatalog.QLocalizationTextRead("Encoder.Button.Result");
        QSEncoderAudioSync(lResult);
        QSEncoderAudioAttach(
            lVerification,
            lResult,
            QContract.QContractFind<ProgressBar>(lSurface, "psAudioCodecProgress"));

        QContract.QContractFind<TextBlock>(lSurface, "psAudioStreamCaption").Text =
            QLocalizationCatalog.QLocalizationTextRead("Encoder.Audio.Field.Stream");
        QContract.QContractFind<TextBlock>(lSurface, "psAudioModeCaption").Text =
            QLocalizationCatalog.QLocalizationTextRead("Encoder.Audio.Field.Mode");
        QContract.QContractFind<TextBlock>(lSurface, "psAudioCodecCaption").Text =
            QLocalizationCatalog.QLocalizationTextRead("Encoder.Audio.Field.Encoder");
        QContract.QContractFind<TextBlock>(lSurface, "psAudioControlCaption").Text =
            QLocalizationCatalog.QLocalizationTextRead("Encoder.Audio.Field.RateControl");
        qsEncoderAudioSound.QSEncoderSoundRebuild();
        QSEncoderAudioToggle();
        QSEncoderAudioUpdate(lOpen.Item5);
        return lSurface;
    }

    public void QSEncoderAudioHandle(string lContainer) =>
        QSEncoderAudioHandle(qsEncoderAudioGate.CSEncoderAudioRead(
            lContainer,
            QSEncoderChoice.QSEncoderChoiceRead(
                QSEncoderAudioSurface.QSEncoderAudioCodec.SelectedItem)));

    private void QSEncoderAudioHandle((string[], string, bool) lAnswer)
    {
        QSEncoderAudioReset(
            QSEncoderAudioSurface.QSEncoderAudioCodec,
            lAnswer.Item1,
            lAnswer.Item2);
        QSEncoderAudioUpdate(lAnswer.Item3);
    }

    public void QSEncoderAudioCommit() =>
        qsEncoderAudioSound?.QSEncoderSoundSave(
            QSEncoderChoice.QSEncoderChoiceRead(QSEncoderAudioSurface.QSEncoderAudioStream.SelectedItem),
            QSEncoderAudioMode);

    private void QSEncoderAudioLoad(string lSelected)
    {
        var lModes = new[]
        {
            new QSEncoderChoice("Copy", QLocalizationCatalog.QLocalizationTextRead("Encoder.Codec.Copy")),
            new QSEncoderChoice("Encode", QLocalizationCatalog.QLocalizationTextRead("Encoder.Codec.Encode")),
            new QSEncoderChoice("Exclude", QLocalizationCatalog.QLocalizationTextRead("Encoder.Stream.Exclude"))
        };
        if (!lModes.Any(lMode => lMode.QSEncoderChoiceToken == lSelected))
        {
            lSelected = lModes[0].QSEncoderChoiceToken;
        }

        QSEncoderAudioSurface.QSEncoderAudioBar.Tag = lSelected;
        for (int lIndex = 0; lIndex < lModes.Length; lIndex++)
        {
            QSEncoderChoice lMode = lModes[lIndex];
            var lFacet = (Border)QContract.QContractSheetFind<DataTemplate>(
                QSEncoderAudioSurface.QSEncoderAudioSource, "psAudioModeFacet").LoadContent();
            lFacet.Tag = lMode.QSEncoderChoiceToken;
            lFacet.BorderThickness = new Thickness(lIndex == 0 ? 0 : 1, 0, 0, 0);
            lFacet.CornerRadius = new CornerRadius(
                lIndex == 0 ? 5 : 0,
                lIndex == lModes.Length - 1 ? 5 : 0,
                lIndex == lModes.Length - 1 ? 5 : 0,
                lIndex == 0 ? 5 : 0);
            ((TextBlock)lFacet.Child).Text = lMode.QSEncoderChoiceLabel;
            lFacet.MouseLeftButtonUp += (lSender, _) => QSEncoderAudioSelect(lSender);
            QSEncoderAudioSurface.QSEncoderAudioBar.Children.Add(lFacet);
        }

        QSEncoderAudioShow();
    }

    private void QSEncoderAudioSelect(object lFacet)
    {
        string lMode = (string)((FrameworkElement)lFacet).Tag;
        QSEncoderAudioSurface.QSEncoderAudioBar.Tag = lMode;
        QSEncoderAudioShow();
        QSEncoderAudioToggle();
    }

    private void QSEncoderAudioShow()
    {
        foreach (Border lFacet in QSEncoderAudioSurface.QSEncoderAudioBar.Children)
        {
            bool lActive = (string)lFacet.Tag == QSEncoderAudioMode;
            lFacet.Background = lActive
                ? QSEncoderAudioSurface.QSEncoderAudioAccent
                : QSEncoderAudioSurface.QSEncoderAudioInactive;
            ((TextBlock)lFacet.Child).Foreground = lActive
                ? QSEncoderAudioSurface.QSEncoderAudioBright
                : QSEncoderAudioSurface.QSEncoderAudioInk;
        }
    }

    private void QSEncoderAudioToggle()
    {
        (bool lEncoded, string lNotice) = qsEncoderAudioGate.CSEncoderAudioCheck(
            QSEncoderChoice.QSEncoderChoiceRead(QSEncoderAudioSurface.QSEncoderAudioStream.SelectedItem),
            QSEncoderAudioMode);
        QSEncoderAudioSurface.QSEncoderAudioRows.Visibility =
            lEncoded ? Visibility.Visible : Visibility.Collapsed;
        QSEncoderAudioSurface.QSEncoderAudioNotice.Visibility =
            lEncoded ? Visibility.Collapsed : Visibility.Visible;
        QSEncoderAudioSurface.QSEncoderAudioNotice.Text =
            QLocalizationCatalog.QLocalizationTextRead(lNotice);
    }

    private void QSEncoderAudioAttach(Button lVerification, Button lResult, ProgressBar lProgress)
    {
        var lFeed = new Progress<double>(lValue => lProgress.Value = lValue);
        lVerification.Click += async (_, _) =>
            await QSEncoderAudioRun(lVerification, lResult, lProgress, lFeed);
        lResult.Click += (_, _) => qsEncoderAudioVerdict?.Invoke(QSEncoderVerdictCreate());
        QSEncoderAudioSurface.QSEncoderAudioAttach(QSEncoderAudioToggle, QSEncoderAudioSet);
    }

    private async Task QSEncoderAudioRun(
        Button lVerification, Button lResult, ProgressBar lProgress, IProgress<double> lFeed)
    {
        lProgress.Value = 0;
        lProgress.Visibility = Visibility.Visible;
        try
        {
            await QSEncoderAudioValidate(lVerification, lFeed);
        }
        finally
        {
            lProgress.Visibility = Visibility.Collapsed;
        }

        QSEncoderAudioSync(lResult);
    }

    private async Task QSEncoderAudioValidate(Button lVerification, IProgress<double> lFeed)
    {
        lVerification.IsEnabled = false;
        lVerification.Content = QLocalizationCatalog.QLocalizationTextRead("Encoder.Verification.Checking");
        (IReadOnlyList<string>, string, bool) lAnswer;
        try
        {
            lAnswer = await QSEncoderAudioScan(lFeed);
        }
        catch (OperationCanceledException)
        {
            lVerification.Content = QLocalizationCatalog.QLocalizationTextRead("Encoder.Button.Verify");
            lVerification.IsEnabled = true;
            return;
        }

        QSEncoderAudioReset(
            QSEncoderAudioSurface.QSEncoderAudioCodec,
            lAnswer.Item1,
            lAnswer.Item2);
        QSEncoderAudioUpdate(lAnswer.Item3);
        lVerification.Content = QLocalizationCatalog.QLocalizationTextRead("Encoder.Button.Verify");
        lVerification.IsEnabled = true;
    }

    private void QSEncoderAudioSync(Button lResult) => lResult.IsEnabled = qsEncoderAudioResults.Count > 0;

    private static void QSEncoderAudioReset(
        ComboBox lCombo, IReadOnlyList<string> lItems, string lSelected)
    {
        lCombo.ItemsSource = lItems;
        lCombo.SelectedItem = lItems.Contains(lSelected) ? lSelected : lItems.FirstOrDefault();
    }

    public void QSEncoderAudioApply()
    {
        string[] lModes = qsEncoderAudioGate.CSEncoderAudioState.CSEncoderAudioModes.ToArray();
        if (QSEncoderAudioSurface.QSEncoderAudioControl.ItemsSource is not string[] lShown
            || !lShown.SequenceEqual(lModes))
        {
            QSEncoderAudioSurface.QSEncoderAudioControl.ItemsSource = lModes;
        }

        QSEncoderAudioSurface.QSEncoderAudioControl.SelectedItem =
            qsEncoderAudioGate.CSEncoderAudioState.CSEncoderAudioRate;
        qsEncoderAudioSound?.QSEncoderSoundRebuild();
        QSEncoderAudioUpdate(qsEncoderAudioGate.CSEncoderAudioCheck(
            QSEncoderChoice.QSEncoderChoiceRead(QSEncoderAudioSurface.QSEncoderAudioCodec.SelectedItem)));
    }

    private void QSEncoderAudioUpdate(bool lAvailable)
    {
        QSEncoderAudioSurface.QSEncoderAudioError.Visibility =
            lAvailable ? Visibility.Collapsed : Visibility.Visible;
        if (!lAvailable)
        {
            QSEncoderAudioSurface.QSEncoderAudioError.Text =
                QLocalizationCatalog.QLocalizationTextRead("Encoder.Audio.Notice.Unavailable");
        }
    }

    internal void QSEncoderAudioRead(string? lSample, double? lChannel, Action<CSEncoderSound> lAnswer) =>
        lAnswer(qsEncoderAudioGate.CSEncoderAudioRead(lSample, lChannel));

    internal void QSEncoderAudioSave(
        string lStream,
        string lMode,
        string? lQuality,
        int lSpeed,
        IReadOnlyList<int> lExtras,
        string? lSample,
        double? lChannel) =>
        qsEncoderAudioGate.CSEncoderAudioSave(lStream, lMode, lQuality, lSpeed, lExtras, lSample, lChannel);

    public QSVerdict QSEncoderVerdictCreate() =>
        new(QLocalizationCatalog.QLocalizationTextRead("Encoder.Verification.AudioTitle"), qsEncoderAudioResults);

    // The gate keeps the encoder and its resolved rate and answers whether the choice changed.
    public bool QSEncoderAudioSet(string lEncoder, string lRate) =>
        qsEncoderAudioGate.CSEncoderAudioSet(lEncoder, lRate);

    private void QSEncoderAudioSet()
    {
        string lEncoder = QSEncoderChoice.QSEncoderChoiceRead(
            QSEncoderAudioSurface.QSEncoderAudioCodec.SelectedItem);
        string lRate = QSEncoderChoice.QSEncoderChoiceRead(
            QSEncoderAudioSurface.QSEncoderAudioControl.SelectedItem);
        qsEncoderAudioGate.CSEncoderAudioSet(lEncoder, lRate);
    }

    public async Task<(IReadOnlyList<string>, string, bool)> QSEncoderAudioScan(IProgress<double> lFeed)
    {
        (IReadOnlyList<string>, string, bool) lAnswer = await qsEncoderAudioGate.CSEncoderAudioRun(lFeed);
        qsEncoderAudioResults = qsEncoderAudioGate.CSEncoderAudioRows.Select(QSEncoderVerdictRead).ToArray();
        return lAnswer;
    }

    private static QSVerdictRow QSEncoderVerdictRead(CSEncoderRow lEntry) =>
        new(
            lEntry.CSEncoderRowFamily,
            lEntry.CSEncoderRowEncoder,
            lEntry.CSEncoderRowSuccess,
            lEntry.CSEncoderRowMessage);

    private sealed class QSEncoderAudioContract(UserControl qsEncoderAudioSource)
    {
        internal UserControl QSEncoderAudioSource => qsEncoderAudioSource;

        internal ComboBox QSEncoderAudioStream =>
            QContract.QContractFind<ComboBox>(qsEncoderAudioSource, "psAudioStreamSelection");

        internal StackPanel QSEncoderAudioBar =>
            QContract.QContractFind<StackPanel>(qsEncoderAudioSource, "psAudioModeBar");

        internal StackPanel QSEncoderAudioRows =>
            QContract.QContractFind<StackPanel>(qsEncoderAudioSource, "psAudioRows");

        internal TextBlock QSEncoderAudioNotice =>
            QContract.QContractFind<TextBlock>(qsEncoderAudioSource, "psAudioNotice");

        internal ComboBox QSEncoderAudioCodec =>
            QContract.QContractFind<ComboBox>(qsEncoderAudioSource, "psAudioCodecSelection");

        internal TextBlock QSEncoderAudioError =>
            QContract.QContractFind<TextBlock>(qsEncoderAudioSource, "psAudioCodecError");

        internal ComboBox QSEncoderAudioControl =>
            QContract.QContractFind<ComboBox>(qsEncoderAudioSource, "psAudioControlSelection");

        internal UserControl QSEncoderAudioRate =>
            QContract.QContractFind<UserControl>(qsEncoderAudioSource, "psAudioRate");

        internal UserControl QSEncoderAudioSample =>
            QContract.QContractFind<UserControl>(qsEncoderAudioSource, "psAudioSample");

        internal UserControl QSEncoderAudioChannel =>
            QContract.QContractFind<UserControl>(qsEncoderAudioSource, "psAudioChannel");

        internal Brush QSEncoderAudioAccent =>
            QContract.QContractSheetFind<Brush>(qsEncoderAudioSource, "psAudioAccent");

        internal Brush QSEncoderAudioInactive =>
            QContract.QContractSheetFind<Brush>(qsEncoderAudioSource, "psAudioInactive");

        internal Brush QSEncoderAudioInk =>
            QContract.QContractSheetFind<Brush>(qsEncoderAudioSource, "psAudioInk");

        internal Brush QSEncoderAudioBright =>
            QContract.QContractSheetFind<Brush>(qsEncoderAudioSource, "psAudioBright");

        internal void QSEncoderAudioAttach(Action lToggle, Action lSet)
        {
            QSEncoderAudioStream.SelectionChanged += (_, _) => lToggle();
            QSEncoderAudioCodec.SelectionChanged += (_, _) => lSet();
            QSEncoderAudioControl.SelectionChanged += (_, _) => lSet();
        }
    }
}
