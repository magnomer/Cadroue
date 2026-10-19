using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

public sealed record QSEncoderAudioCodec(
    string QSEncoderAudioMode,
    QSCodecQuality? QSEncoderAudioQuality,
    QSCodecSpeed? QSEncoderAudioSpeed,
    IReadOnlyList<(string QSCodecExtraLabel, string QSCodecExtraOption, string QSCodecExtraDefault,
        IReadOnlyList<(string QSEncoderChoiceToken,
            string QSEncoderChoiceLabel)> QSCodecExtraValues)> QSEncoderAudioExtras,
    string QSEncoderAudioCaution);

public sealed record QSCodecQuality(
    string QSCodecQualityLabel,
    string QSCodecQualityDefault,
    double? QSCodecQualityMinimum,
    double? QSCodecQualityMaximum,
    double QSCodecQualityStep,
    bool QSCodecQualityAscending,
    bool QSCodecQualityBitrate,
    string QSCodecQualityRange)
{
    internal static QSCodecQuality QSCodecQualityRead(CSCodecQuality quality) =>
        new(
            quality.CSCodecQualityLabel,
            quality.CSCodecQualityDefault,
            quality.CSCodecQualityMinimum,
            quality.CSCodecQualityMaximum,
            quality.CSCodecQualityStep,
            quality.CSCodecQualityAscending,
            quality.CSCodecQualityBitrate,
            QLocalizationCatalog.QLocalizationFormat(
                quality.CSCodecQualityKey, quality.CSCodecQualityArguments));
}

public sealed record QSCodecSpeed(
    string QSCodecSpeedLabel,
    string QSCodecSpeedDefault,
    IReadOnlyList<(string QSEncoderChoiceToken, string QSEncoderChoiceLabel)> QSCodecSpeedValues)
{
    internal static QSCodecSpeed QSCodecSpeedRead(CSCodecSpeed speed) =>
        new(
            speed.CSCodecSpeedLabel,
            speed.CSCodecSpeedDefault,
            speed.CSCodecSpeedValues);
}
