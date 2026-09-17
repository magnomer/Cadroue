namespace Cadroue.UIVeneer.PPanel;

public sealed partial class PInspector
{
    private static string PEqualizerKeyRead(string pToken) => pToken switch
    {
        "Flat" => "Inspector.Equalizer.Preset.Flat",
        "Bass boost" => "Inspector.Equalizer.Preset.BassBoost",
        "Bright" => "Inspector.Equalizer.Preset.Bright",
        "Warm" => "Inspector.Equalizer.Preset.Warm",
        "Loudness" => "Inspector.Equalizer.Preset.Loudness",
        "Vocal" => "Inspector.Equalizer.Preset.Vocal",
        "De-ess" => "Inspector.Equalizer.Preset.Deess",
        "Podcast" => "Inspector.Equalizer.Preset.Podcast",
        "Telephone" => "Inspector.Equalizer.Preset.Telephone",
        _ => "Inspector.Common.Custom"
    };
}
