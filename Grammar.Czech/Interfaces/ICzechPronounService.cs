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
        /// Determines whether the lemma is a relative pronoun that possesses rather than stands for a
        /// participant.
        /// </summary>
        /// <param name="baseForm">The dictionary form to resolve or analyze.</param>
        /// <returns>
        /// <see langword="true"/> for jehož, jejíž and jejichž; otherwise, <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// Read off the readings rather than listed in code: these three carry both a relative and a
        /// possessive reading, and no other pronoun carries that pair. It is the question three separate
        /// stages have to ask — the role resolver reserves no slot for one, the planner makes it an
        /// attribute of the noun possessed, and the word-order resolver does not render it a second time
        /// — so it is asked once, here.
        /// </remarks>
        bool IsPossessiveRelative(string baseForm);

        /// <summary>
        /// Gets the inflection class used to choose pronoun form lookup.
        /// </summary>
        /// <param name="lemma">The dictionary form to resolve or analyze.</param>
        /// <returns>The inflection class stored for the lemma, or <see langword="null"/> when the lemma is unknown.</returns>
        InflectionClass? GetInflectionClass(string lemma);
    }
}
