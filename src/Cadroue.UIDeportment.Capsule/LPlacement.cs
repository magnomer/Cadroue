using System.Text.Json.Serialization;

namespace Cadroue.UIDeportment.Capsule;

public sealed record LPlacement(
    [property: JsonPropertyName("LPlacementLeft")] double LPlacementLeft,
    [property: JsonPropertyName("LPlacementTop")] double LPlacementTop,
    [property: JsonPropertyName("LPlacementWidth")] double LPlacementWidth,
    [property: JsonPropertyName("LPlacementHeight")] double LPlacementHeight);
