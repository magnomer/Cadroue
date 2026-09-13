using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.ShellEngine;

namespace Cadroue.Tests;

internal static partial class TInterface
{
    internal static LWorkCrop TWorkCropCreate() => LWorkCrop.LWorkCropCreate();
    internal static LWorkCrop TWorkCropCreate(
        int left, int top, int right, int bottom, int rotation, bool horizontal, bool vertical) =>
        new(left, top, right, bottom, rotation, horizontal, vertical);
    internal static LWorkVideo TWorkVideoCreate() => LWorkVideo.LWorkVideoCreate();
    internal static LWorkVideo TWorkVideoCreate(IReadOnlyList<LWorkVideoStep> steps) => new(steps);
    internal static LWorkBand TWorkBandCreate(double frequency, double gain) => new(frequency, gain);
    internal static LWorkAudio TWorkAudioCreate(IReadOnlyList<LWorkAudioStep> steps) => new(steps);
    internal static LWorkMedia TWorkMediaCreate(int width, int height, double rate, long durationMs, bool audio) =>
        new(width, height, rate, durationMs, audio);
    internal static LSplitSectionDescription TSplitSectionCreate(
        TimeSpan start, TimeSpan end, string name, bool hidden = false) =>
        new(start, end, name, LSplitSectionHidden: hidden);
    internal static LSplitWorkDescription TSplitDescriptionCreate(
        string? source, IReadOnlyList<LSplitSectionDescription> sections, LEncoding output) =>
        new(source, sections, output);
    internal static LConvertWorkDescription TConvertDescriptionCreate(
        IReadOnlyList<string> sources, LEncoding output, IReadOnlyDictionary<string, LWorkMedia>? media = null) =>
        new(sources, output, media);
    internal static LEditWorkDescription TEditDescriptionCreate(
        string? source, TimeSpan duration, LWorkCrop crop, LWorkVideo video, LEncoding output) =>
        new(source, duration, crop, video, output);
    internal static LFixWorkDescription TFixDescriptionCreate(
        IReadOnlyList<string> sources, LEncoding output, IReadOnlyDictionary<string, LWorkMedia>? media = null) =>
        new(sources, output, media);
    internal static LWorkGroup TWorkGroupCreate(string name, IReadOnlyList<string> sources) => new(name, sources);
    internal static LWorkVideoStep TWorkBrightnessCreate(bool active, double value) =>
        LWorkVideoStep.LWorkBrightnessCreate(active, value);
    internal static LWorkVideoStep TWorkContrastCreate(bool active, double value) =>
        LWorkVideoStep.LWorkContrastCreate(active, value);
    internal static LWorkVideoStep TWorkSaturationCreate(bool active, double value) =>
        LWorkVideoStep.LWorkSaturationCreate(active, value);
    internal static LWorkVideoStep TWorkExposureCreate(bool active, double value) =>
        LWorkVideoStep.LWorkExposureCreate(active, value);
    internal static LWorkVideoStep TWorkGammaCreate(bool active, double value) =>
        LWorkVideoStep.LWorkGammaCreate(active, value);
    internal static LWorkVideoStep TWorkGammaCreate(
        bool active, double global, double red, double green, double blue, double protection) =>
        LWorkVideoStep.LWorkGammaCreate(active, global, red, green, blue, protection);
    internal static LWorkVideoStep TWorkWhitebalanceCreate(
        bool active, LWhitebalanceMethod method, double saturation) =>
        LWorkVideoStep.LWorkWhitebalanceCreate(active, method, saturation);
    internal static LWorkVideoStep TWorkManualCreate(
        bool active, double saturation,
        double red, double green, double blue,
        int sampleRed, int sampleGreen, int sampleBlue) =>
        LWorkVideoStep.LWorkWhitebalanceCreate(
            active, LWhitebalanceMethod.LWhitebalanceMethodManual, saturation,
            red, green, blue, sampleRed, sampleGreen, sampleBlue);
    internal static LWorkWhitebalanceSettings TWorkWhitebalanceRead(LWorkVideoStep step) =>
        step.LWorkWhitebalanceRead();
    internal static LWorkVideoStep TWorkCurveCreate(
        bool active,
        IReadOnlyList<LWorkCurvePoint>? master = null,
        IReadOnlyList<LWorkCurvePoint>? red = null,
        IReadOnlyList<LWorkCurvePoint>? green = null,
        IReadOnlyList<LWorkCurvePoint>? blue = null) =>
        LWorkVideoStep.LWorkCurveCreate(active, master, red, green, blue);
    internal static LWorkCurvePoint TWorkPointCreate(double input, double output) =>
        new(input, output);
    internal static LWorkCurveSettings TWorkCurveRead(LWorkVideoStep step) => step.LWorkCurveRead();
    internal static string TWorkCurveFormat(LWorkVideoStep step) => step.LWorkCurveFormat();
    internal static LWorkVideoStep TWorkMalformedCreate(
        LWhitebalanceMethod method, double value, double saturation) =>
        new(LColorKind.LColorKindWhitebalance, true, value)
        {
            LWorkStepWhitebalance = new LWorkWhitebalanceSettings(method, saturation)
        };
    internal static LWorkVideoStep TWorkBrokenCreate(
        double saturation, double red, double green, double blue,
        int sampleRed, int sampleGreen, int sampleBlue) =>
        new(LColorKind.LColorKindWhitebalance, true, saturation)
        {
            LWorkStepWhitebalance = new LWorkWhitebalanceSettings(
                LWhitebalanceMethod.LWhitebalanceMethodManual, saturation)
            {
                LWorkWhitebalanceRed = red,
                LWorkWhitebalanceGreen = green,
                LWorkWhitebalanceBlue = blue,
                LWorkSampleRed = sampleRed,
                LWorkSampleGreen = sampleGreen,
                LWorkSampleBlue = sampleBlue
            }
        };
    internal static LWorkVideoStep TWorkStrayCreate(
        LWhitebalanceMethod method, double red, int sampleRed) =>
        new(LColorKind.LColorKindWhitebalance, true, 100)
        {
            LWorkStepWhitebalance = new LWorkWhitebalanceSettings(method, 100)
            {
                LWorkWhitebalanceRed = red,
                LWorkSampleRed = sampleRed
            }
        };
    internal static string TWorkDiagnosticRead(LWorkVideoStep step) => step.LWorkDiagnosticRead();
    internal static LWorkItem? TWorkRecordMatch(LWorkItem work)
    {
        string json = LWorkRecord.LWorkRecordCreate(work).LWorkJsonCreate();
        return LWorkRecord.LWorkRecordParse(json)?.LWorkItemCreate();
    }
    internal static LSidecarEditRecord TSidecarEditMatch(LSidecarEditRecord record) =>
        System.Text.Json.JsonSerializer.Deserialize<LSidecarEditRecord>(
            System.Text.Json.JsonSerializer.Serialize(record))!;
    internal static LWorkAudioStep TWorkVolumeCreate(bool active, double gain) =>
        LWorkAudioStep.LWorkVolumeCreate(active, gain);
    internal static LWorkAudioStep TWorkNormalizeCreate(
        bool active, LLeveling mode, double target, double peak, double range, bool twoPass,
        double frame, double gauss, double maxGain, double compress) =>
        LWorkAudioStep.LWorkNormalizeCreate(
            active,
            mode,
            target,
            peak,
            range,
            twoPass,
            frame,
            gauss,
            maxGain,
            compress);
    internal static LWorkAudioStep TWorkNoiseCreate(
        bool active, double reduction, double floor, bool outputNoise, LGrain grain,
        double smooth, double adaptivity, double residual) =>
        LWorkAudioStep.LWorkNoiseCreate(active, reduction, floor, outputNoise, grain, smooth, adaptivity, residual);
    internal static LWorkAudioStep TWorkHighCreate(
        bool active, double frequency, int stages, int poles, double resonance) =>
        LWorkAudioStep.LWorkHighCreate(active, frequency, stages, poles, resonance);
    internal static LWorkAudioStep TWorkLowCreate(
        bool active, double frequency, int stages, int poles, double resonance) =>
        LWorkAudioStep.LWorkLowCreate(active, frequency, stages, poles, resonance);
    internal static LWorkAudioStep TWorkEqualizerCreate(bool active, IReadOnlyList<LWorkBand> bands) =>
        LWorkAudioStep.LWorkEqualizerCreate(active, bands);
    internal static string TWorkAudioFormat(LWorkAudio audio, int rate = 0) => audio.LWorkAudioFormat(rate);
}
