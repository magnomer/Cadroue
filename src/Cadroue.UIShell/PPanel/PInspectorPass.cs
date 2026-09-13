using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Cadroue.Core;
using Cadroue.UIShell.PHouse;

namespace Cadroue.UIShell.PPanel;

public sealed partial class PInspector
{
    private sealed record PInspectorPassChoice(
        string PInspectorPassToken,
        string PInspectorPassKey);

    private static readonly PInspectorPassChoice[] pFilterHighChoices =
    {
        new("Rumble", "Inspector.Pass.Preset.Rumble"),
        new("Wind", "Inspector.Pass.Preset.Wind"),
        new("Voice", "Inspector.Pass.Preset.Voice"),
        new("Speech (tight)", "Inspector.Pass.Preset.SpeechTight"),
        new("Tighten", "Inspector.Pass.Preset.Tighten")
    };

    private static readonly PInspectorPassChoice[] pFilterLowChoices =
    {
        new("Air tame", "Inspector.Pass.Preset.Airtame"),
        new("Soften", "Inspector.Pass.Preset.Soften"),
        new("Warm", "Inspector.Pass.Preset.Warm"),
        new("AM radio", "Inspector.Pass.Preset.AmRadio"),
        new("Telephone", "Inspector.Pass.Preset.Telephone")
    };

    private sealed class PInspectorPass
    {
        public required CheckBox PFilterApplyBox { get; init; }
        public required CheckBox PInspectorPassPersistent { get; init; }
        public required ComboBox PInspectorPassPreset { get; init; }
        public required Slider PInspectorPassFrequency { get; init; }
        public required TextBox PInspectorPassValue { get; init; }
        public required Slider PInspectorPassStages { get; init; }
        public required TextBox PFilterStageValue { get; init; }
        public required ComboBox PInspectorPassPoles { get; init; }
        public required TextBox PInspectorPassResonance { get; init; }
        public required StackPanel PInspectorPassStack { get; init; }
        public required StackPanel PInspectorPassBody { get; init; }
        public required IReadOnlyList<PInspectorPassChoice> PInspectorPassPresets { get; init; }
        public bool PInspectorPassHigh { get; init; }
        public double PInspectorPassMin { get; init; }
        public double PInspectorPassMax { get; init; }
        public double PInspectorPassDefault { get; init; }
        public bool PInspectorPassSuppress { get; set; }
        public bool PFilterStageSuppress { get; set; }
        public bool PInspectorPresetSuppress { get; set; }
        public string? PInspectorPassBase { get; set; }
    }

    private PInspectorPass pInspectorHighPass = null!;
    private PInspectorPass pInspectorLowPass = null!;

    private StackPanel PFilterHighBuild()
    {
        LPassbandPreset pHighDefault = LPassband.LPassbandRead(true, LPassband.LPassbandHighDefault)!;
        pInspectorHighPass = PInspectorPassBuild(
            pHighDefault.LPassbandCutoff,
            LPassband.LPassbandHighFloor,
            LPassband.LPassbandHighCeiling,
            LLocalization.LLocalizationTextRead("Inspector.Pass.HighApply"),
            pFilterHighChoices,
            true,
            LPassband.LPassbandHighDefault);
        return pInspectorHighPass.PInspectorPassBody;
    }

    private StackPanel PFilterLowBuild()
    {
        LPassbandPreset pLowDefault = LPassband.LPassbandRead(false, LPassband.LPassbandLowDefault)!;
        pInspectorLowPass = PInspectorPassBuild(
            pLowDefault.LPassbandCutoff,
            LPassband.LPassbandLowFloor,
            LPassband.LPassbandLowCeiling,
            LLocalization.LLocalizationTextRead("Inspector.Pass.LowApply"),
            pFilterLowChoices,
            false,
            LPassband.LPassbandLowDefault);
        return pInspectorLowPass.PInspectorPassBody;
    }
}
