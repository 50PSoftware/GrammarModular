using Grammar.Core.Enums;
using Grammar.Core.Interfaces;
using Grammar.Core.Models.Word;
using Grammar.Czech.Models;

namespace Grammar.Czech.Services
{
    /// <summary>
    /// Dispatches Czech word requests to the matching inflection service.
    /// </summary>
    /// <remarks>
    /// This is what <see cref="IInflectionService{TWord}"/> resolves to, because it is the only inflection
    /// service that accepts a request of any word class. The per-class services are registered under their
    /// own concrete types for a caller who already knows which one it needs.
    /// </remarks>
    public class MorphologyEngine : IInflectionService<CzechWordRequest>, IVerbInflectionService<CzechWordRequest>
    {
        private readonly CzechNounDeclensionService nounDeclensionService;
        private readonly CzechAdjectiveDeclensionService adjectiveDeclensionService;
        private readonly CzechPronounService pronounService;
        private readonly CzechNumeralService numeralService;
        private readonly CzechVerbConjugationService verbConjugationService;
        private readonly CzechAdverbService adverbService;

        /// <summary>
        /// Initializes a new instance of the <see cref="MorphologyEngine"/> type.
        /// </summary>
        public MorphologyEngine(CzechNounDeclensionService nounDeclensionService, CzechAdjectiveDeclensionService adjectiveDeclensionService, CzechPronounService pronounService, CzechNumeralService numeralService, CzechVerbConjugationService verbConjugationService, CzechAdverbService adverbService)
        {
            this.adverbService = adverbService;
            this.nounDeclensionService = nounDeclensionService;
            this.adjectiveDeclensionService = adjectiveDeclensionService;
            this.pronounService = pronounService;
            this.numeralService = numeralService;
            this.verbConjugationService = verbConjugationService;
        }

        /// <summary>
        /// Builds or dispatches the basic verb form without phrase-level composition.
        /// </summary>
        /// <param name="wordRequest">The word request to analyze or inflect.</param>
        /// <returns>The generated basic verb form.</returns>
        public WordForm GetBasicForm(CzechWordRequest wordRequest)
        {
            return wordRequest.WordCategory switch
            {
                WordCategory.Verb => verbConjugationService.GetBasicForm(wordRequest),
                _ => throw new NotSupportedException($"Basic form retrieval is only supported for verbs. Category: {wordRequest.WordCategory}")
            };
        }

        /// <summary>
        /// Builds the requested inflected form for any word class the engine covers.
        /// </summary>
        /// <param name="word">The Czech word request containing the lemma and requested grammatical categories.</param>
        /// <returns>The generated inflected word form.</returns>
        /// <remarks>
        /// A verb is routed to <see cref="GetBasicForm"/>, so this returns a single word for it. The verb
        /// forms that are several words — the periphrastic future, the passive with an auxiliary, the
        /// conditional, negation, the reflexive — are assembled by
        /// <see cref="CzechWordFormComposer.GetFullForm"/>, which is what a caller building a phrase wants.
        /// </remarks>
        public WordForm GetForm(CzechWordRequest word)
        {
            return word.WordCategory switch
            {
                WordCategory.Noun => nounDeclensionService.GetForm(word),
                WordCategory.Adjective => adjectiveDeclensionService.GetForm(word),
                WordCategory.Pronoun => pronounService.GetForm(word),
                WordCategory.Numerale => numeralService.GetForm(word),
                WordCategory.Adverb => adverbService.GetForm(word),
                WordCategory.Verb => verbConjugationService.GetBasicForm(word),

                // The uninflected classes. Handing back the lemma is not a stub here — it is the whole of
                // their morphology, and saying so is what lets them travel through a word request like any
                // other word. What they do in a sentence is a different question, answered by their own
                // services: government by the preposition service, comma and clause position by the
                // conjunction and particle ones, punctuation by the interjection one.
                WordCategory.Preposition
                    or WordCategory.Conjunction
                    or WordCategory.Particle
                    or WordCategory.Interjection => new WordForm(word.Lemma),

                _ => throw new NotSupportedException($"Unsupported category: {word.WordCategory}")
            };
        }
    }
}
