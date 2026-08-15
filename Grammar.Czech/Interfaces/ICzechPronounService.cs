using Grammar.Core.Enums;
using Grammar.Czech.Models;

namespace Grammar.Czech.Interfaces
{
    /// <summary>
    /// Defines operations for resolving Czech pronoun forms and metadata.
    /// </summary>
    public interface ICzechPronounService
    {
        /// <summary>
        /// Vrací tvar zájmena v daném pádu. Pokud není k dispozici, vrátí null.
        /// </summary>
        string? TryGetForm(string baseForm, Case grammaticalCase, Gender? gender, Number? number, bool? isAnimate, PronounFormOptions? options);

        /// <summary>
        /// Vrací všechny dostupné pády pro dané zájmeno (např. „já“ → 1., 2., 3., ...)
        /// </summary>
        IEnumerable<Case> GetAvailableCases(string baseForm);

        /// <summary>
        /// Vrací true, pokud daná kombinace zájmena a pádu existuje.
        /// </summary>
        bool IsAllowed(string baseForm, Case grammaticalCase);

        /// <summary>
        /// Vrací typ zájmena (Personal, Possessive, Demonstrative, ...)
        /// </summary>
        PronounType? GetPronounType(string baseForm);

        /// <summary>
        /// Gets every reading the pronoun has, the primary one first.
        /// </summary>
        /// <param name="baseForm">The dictionary form to resolve or analyze.</param>
        /// <returns>
        /// The readings, or an empty list when the lemma is not a registered pronoun.
        /// </returns>
        /// <remarks>
        /// A pronoun may be two words wearing one spelling — <em>co</em> asks a question and introduces a
        /// relative clause — so a caller that knows which construction it is building asks here rather than
        /// comparing <see cref="GetPronounType"/> against one value. That comparison answers "what is this
        /// word normally", which is a different question from "may this word do this job".
        /// </remarks>
        IReadOnlyList<PronounData> GetReadings(string baseForm);

        /// <summary>
        /// Gets the inflection class used to choose pronoun form lookup.
        /// </summary>
        /// <param name="lemma">The dictionary form to resolve or analyze.</param>
        /// <returns>The inflection class stored for the lemma, or <see langword="null"/> when the lemma is unknown.</returns>
        InflectionClass? GetInflectionClass(string lemma);
    }
}
