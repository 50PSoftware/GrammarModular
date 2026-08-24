using Grammar.Core.Enums;
using Grammar.Czech.Models;

namespace Grammar.Czech.Analyzer.Candidates
{
    /// <summary>
    /// The requests that make up "the paradigm slice this analyzer cares about" for a verb —
    /// infinitive, present/future by person and number, and the past participle by gender and number.
    /// </summary>
    /// <remarks>
    /// Shared by <see cref="KnownWords"/> (expanding an already-known verb's own forms) and
    /// <see cref="VerbMatcher"/> (testing a hypothesis), so the two can never quietly drift into
    /// checking two different slices of the paradigm.
    /// <para>
    /// Conditional and passive are left out on purpose. Conditional reuses the exact same past
    /// participle string this already generates — "dělal bych" is "dělal" plus the clitic "bych",
    /// which <see cref="Analyzer.KnownWords"/> already knows — so asking for it again would just
    /// regenerate a form already in the set. Passive and imperative are deferred rather than free,
    /// and the aspect field is left unset everywhere: leaving it null never triggers the periphrastic-
    /// future shortcut in <see cref="Services.CzechVerbConjugationService"/>, so the present-tense
    /// endings table is used either way — the same surface string comes out whether the real verb
    /// turns out to be perfective or imperfective.
    /// </para>
    /// </remarks>
    public static class VerbForms
    {
        private static readonly (Person Person, Number Number)[] PresentSlots =
        [
            (Person.First, Number.Singular), (Person.Second, Number.Singular), (Person.Third, Number.Singular),
            (Person.First, Number.Plural), (Person.Second, Number.Plural), (Person.Third, Number.Plural),
        ];

        private static readonly (Gender Gender, Number Number)[] PastSlots =
        [
            (Gender.Masculine, Number.Singular), (Gender.Feminine, Number.Singular), (Gender.Neuter, Number.Singular),
            (Gender.Masculine, Number.Plural), (Gender.Feminine, Number.Plural), (Gender.Neuter, Number.Plural),
        ];

        /// <summary>
        /// The requests for the infinitive, present/future and past-participle slice of the paradigm
        /// of a given lemma under a given pattern.
        /// </summary>
        public static IEnumerable<CzechWordRequest> Requests(string lemma, string pattern)
        {
            yield return new CzechWordRequest
            {
                Lemma = lemma, Pattern = pattern, Modus = Modus.Infinitive, WordCategory = WordCategory.Verb,
            };

            foreach (var (person, number) in PresentSlots)
            {
                yield return new CzechWordRequest
                {
                    Lemma = lemma, Pattern = pattern, WordCategory = WordCategory.Verb,
                    Modus = Modus.Indicative, Tense = Tense.Present, Person = person, Number = number,
                };
            }

            foreach (var (gender, number) in PastSlots)
            {
                yield return new CzechWordRequest
                {
                    Lemma = lemma, Pattern = pattern, WordCategory = WordCategory.Verb,
                    Modus = Modus.Indicative, Tense = Tense.Past, Gender = gender, Number = number,
                };
            }
        }
    }
}
