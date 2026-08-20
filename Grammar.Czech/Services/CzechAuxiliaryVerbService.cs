using Grammar.Core.Enums;
using Grammar.Czech.Models;

namespace Grammar.Czech.Services
{
    /// <summary>
    /// Provides Czech auxiliary verb forms used by compound verb phrases.
    /// </summary>
    public class CzechAuxiliaryVerbService
    {
        private readonly MorphologyEngine engine;

        /// <summary>
        /// Initializes a new instance of the <see cref="CzechAuxiliaryVerbService"/> type.
        /// </summary>
        public CzechAuxiliaryVerbService(MorphologyEngine engine)
        {
            this.engine = engine;
        }

        /// <summary>
        /// Gets the Czech auxiliary form of "byt" for the requested grammatical context.
        /// </summary>
        /// <param name="tense">The requested grammatical tense.</param>
        /// <param name="number">The grammatical number supplied by the test data.</param>
        /// <param name="person">The requested grammatical person.</param>
        /// <param name="modus">The requested grammatical mood.</param>
        /// <param name="gender">The grammatical gender supplied by the test data.</param>
        /// <param name="isNegative">True when the generated phrase should be negated; otherwise, false.</param>
        /// <returns>The auxiliary form, including negation when requested.</returns>
        public string GetBeForm(Tense? tense, Number? number, Person? person, Modus? modus, Gender? gender, bool isNegative = false)
        {
            if (tense == Tense.Present && number == Number.Singular && person == Person.Third)
                return isNegative ? "není" : "je";

            var request = new CzechWordRequest
            {
                Lemma = "být",
                Pattern = "být",
                WordCategory = WordCategory.Verb,
                Tense = tense,
                Number = number,
                Person = person,
                Gender = gender,
                Modus = modus
            };

            var baseForm = engine.GetBasicForm(request).Form;

            return isNegative ? $"ne{baseForm}" : baseForm;
        }

        /// <summary>
        /// Gets the Czech auxiliary form of "mít" for the requested grammatical context.
        /// </summary>
        /// <param name="tense">The requested grammatical tense.</param>
        /// <param name="number">The grammatical number supplied by the test data.</param>
        /// <param name="person">The requested grammatical person.</param>
        /// <param name="modus">The requested grammatical mood.</param>
        /// <param name="gender">The grammatical gender supplied by the test data.</param>
        /// <param name="isNegative">True when the generated phrase should be negated; otherwise, false.</param>
        /// <returns>The auxiliary form, including negation when requested.</returns>
        /// <remarks>
        /// Used for the resultative diathesis (<em>mám napsáno</em>), where "mít" governs the sentence
        /// as an ordinary verb rather than a clitic auxiliary — unlike "být" it has no irregular present
        /// tense worth special-casing, so this conjugates through the regular vzor. In the past this
        /// returns the bare l-participle ("měl"); the clitic first/second person needs on top of it
        /// ("měl jsem") is <see cref="CzechVerbPhraseBuilderService"/>'s to attach, the same as any other
        /// past tense.
        /// </remarks>
        public string GetHaveForm(Tense? tense, Number? number, Person? person, Modus? modus, Gender? gender, bool isNegative = false)
        {
            var request = new CzechWordRequest
            {
                Lemma = "mít",
                Pattern = "mít",
                WordCategory = WordCategory.Verb,
                Tense = tense,
                Number = number,
                Person = person,
                Gender = gender,
                Modus = modus
            };

            var baseForm = engine.GetBasicForm(request).Form;

            return isNegative ? $"ne{baseForm}" : baseForm;
        }

        /// <summary>
        /// Gets the Czech auxiliary form of "dostat" for the requested grammatical context.
        /// </summary>
        /// <param name="tense">The requested grammatical tense.</param>
        /// <param name="number">The grammatical number supplied by the test data.</param>
        /// <param name="person">The requested grammatical person.</param>
        /// <param name="modus">The requested grammatical mood.</param>
        /// <param name="gender">The grammatical gender supplied by the test data.</param>
        /// <param name="isNegative">True when the generated phrase should be negated; otherwise, false.</param>
        /// <returns>The auxiliary form, including negation when requested.</returns>
        /// <remarks>
        /// Used for the recipient deobjective diathesis (<em>Karel dostal zaplaceno</em>), where "dostat"
        /// governs the sentence as an ordinary perfective verb (Daneš, Naše řeč 51, 1968). In the past
        /// this returns the bare l-participle ("dostal"); the clitic first/second person needs on top of
        /// it ("dostal jsem") is <see cref="CzechVerbPhraseBuilderService"/>'s to attach, the same as any
        /// other past tense.
        /// </remarks>
        public string GetGetForm(Tense? tense, Number? number, Person? person, Modus? modus, Gender? gender, bool isNegative = false)
        {
            var request = new CzechWordRequest
            {
                Lemma = "dostat",
                Pattern = "dostat",
                WordCategory = WordCategory.Verb,
                Tense = tense,
                Number = number,
                Person = person,
                Gender = gender,
                Modus = modus
            };

            var baseForm = engine.GetBasicForm(request).Form;

            return isNegative ? $"ne{baseForm}" : baseForm;
        }
    }
}
