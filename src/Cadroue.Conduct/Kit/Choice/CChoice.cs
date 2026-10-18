namespace Cadroue.Conduct;

// One row of a choice list a gate answers. The token is what a selection maps to, null for the custom row; the key
// is the row's wording key; the basis is the wording key of the preset a custom row started from, when it shows one.
// The gate marks the chosen row and maps a selected index back to its token.
public sealed record CChoice(string? CChoiceToken, string CChoiceKey, string? CChoiceBasis, bool CChoiceChosen);
