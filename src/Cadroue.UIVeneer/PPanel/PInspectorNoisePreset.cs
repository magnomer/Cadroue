namespace Cadroue.UIVeneer.PPanel;

public sealed partial class PInspector
{
    private static string PNoiseKeyRead(string pToken) => pToken switch
    {
        "Light" => "Inspector.Noise.Light",
        "Medium" => "Inspector.Noise.Medium",
        "Strong" => "Inspector.Noise.Strong",
        "Dialogue" => "Inspector.Noise.Dialogue",
        "Vinyl" => "Inspector.Noise.Vinyl",
        "Shellac" => "Inspector.Noise.Shellac",
        _ => "Inspector.Common.Custom"
    };
}
