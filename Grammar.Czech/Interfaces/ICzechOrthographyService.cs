namespace Grammar.Czech.Interfaces
{
    /// <summary>
    /// Defines operations for czech orthography behavior.
    /// </summary>
    public interface ICzechOrthographyService
    {
        /// <summary>
        /// Ortografická konverze výsledku jotace: e→ě v koncovce.
        /// Zápis morfonologického procesu vložení /j/ po labiálách (pje→pě, bje→bě...).
        /// </summary>
        string ApplyJotationOrthography(string ending);

        /// <summary>
        /// Normalizace ě→e v koncovce kde ě ortograficky nedává smysl.
        /// ě se drží po d/t/n vždy; po labiále jen v tvrdém skloňování — v měkkém
        /// (vzor <paramref name="pattern"/> píseň/růže…) se koncové soft -e nemění na ě (větev→větve).
        /// </summary>
        string NormalizeEndingOrthography(string stem, string ending, string pattern);
    }
}
