namespace Grammar.Czech.Models
{
    /// <summary>
    /// Represents lookup options for selecting a Czech numeral form.
    /// </summary>
    /// <param name="PreferColloquial">Prefers the colloquial standard doublet: třech over tří.</param>
    /// <param name="Paired">Prefers the dual form used with paired body parts: třema over třemi.</param>
    /// <param name="PreferRare">Prefers the rare or bookish variant.</param>
    public sealed record NumeralFormOptions(
        bool PreferColloquial = false,
        bool Paired = false,
        bool PreferRare = false
        );
}
