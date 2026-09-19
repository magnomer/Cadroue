using System.Windows.Controls;
using Cadroue.Application;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PWing;

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
        public required LFilter PFilterOwner { get; init; }
        public required CheckBox PFilterApplyBox { get; init; }
        public required CheckBox PInspectorPassPersistent { get; init; }
        public required ComboBox PInspectorPassPreset { get; init; }
        public required Slider PInspectorPassFrequency { get; init; }
        public required TextBox PInspectorPassValue { get; init; }
        public required Slider PInspectorPassStages { get; init; }
        public required TextBox PFilterStageValue { get; init; }
        public required ComboBox PInspectorPassPoles { get; init; }
        public required Slider PFilterResonanceSlider { get; init; }
        public required TextBox PInspectorPassResonance { get; init; }
        public required Grid PFilterResonanceRow { get; init; }
        public required StackPanel PInspectorPassStack { get; init; }
        public required StackPanel PInspectorPassBody { get; init; }
        public required IReadOnlyList<PInspectorPassChoice> PInspectorPassPresets { get; init; }
    }

    private PInspectorPass pInspectorHighPass = null!;
    private PInspectorPass pInspectorLowPass = null!;

    private StackPanel PFilterHighBuild()
    {
        pInspectorHighPass = PInspectorPassBuild(
            LFilterHigh,
            LLocalization.LLocalizationTextRead("Inspector.Pass.HighApply"),
            pFilterHighChoices);
        return pInspectorHighPass.PInspectorPassBody;
    }

    private StackPanel PFilterLowBuild()
    {
        pInspectorLowPass = PInspectorPassBuild(
            LFilterLow,
            LLocalization.LLocalizationTextRead("Inspector.Pass.LowApply"),
            pFilterLowChoices);
        return pInspectorLowPass.PInspectorPassBody;
    }
}
