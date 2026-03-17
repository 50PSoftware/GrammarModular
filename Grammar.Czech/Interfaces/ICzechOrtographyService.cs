namespace Grammar.Czech.Interfaces
{
    public interface ICzechOrtographyService
    {
        /// <summary>
        /// Ortografická konverze výsledku jotace: e→ě v koncovce.
        /// Zápis morfonologického procesu vložení /j/ po labiálách (pje→pě, bje→bě...).
        /// </summary>
        string ApplyJotationOrthography(string ending);

        /// <summary>
        /// Normalizace ě→e v koncovce kde ě ortograficky nedává smysl
        /// (non-DTN a non-labiální konsonant).
        /// </summary>
        string NormalizeEndingOrthography(string stem, string ending);
    }
}
