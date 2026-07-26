using Grammar.Core.Enums;
using Grammar.Core.Interfaces;
using Grammar.Core.Models.Word;
using Grammar.Czech.Models;

namespace Grammar.Czech.Services
{
    /// <summary>
    /// Dispatches Czech word requests to the matching inflection service.
    /// </summary>
    public class MorphologyEngine : IInflectionService<CzechWordRequest>, IVerbInflectionService<CzechWordRequest>
    {
        private readonly CzechNounDeclensionService nounDeclensionService;
        private readonly CzechAdjectiveDeclensionService adjectiveDeclensionService;
        private readonly CzechPronounService pronounService;
        private readonly CzechNumeralService numeralService;
        private readonly CzechVerbConjugationService verbConjugationService;

        /// <summary>
        /// Initializes a new instance of the <see cref="MorphologyEngine"/> type.
        /// </summary>
        public MorphologyEngine(CzechNounDeclensionService nounDeclensionService, CzechAdjectiveDeclensionService adjectiveDeclensionService, CzechPronounService pronounService, CzechNumeralService numeralService, CzechVerbConjugationService verbConjugationService)
        {
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
        /// Builds the requested inflected form.
        /// </summary>
        /// <param name="word">The Czech word request containing the lemma and requested grammatical categories.</param>
        /// <returns>The generated inflected word form.</returns>
        public WordForm GetForm(CzechWordRequest word)
        {
            return word.WordCategory switch
            {
                WordCategory.Noun => nounDeclensionService.GetForm(word),
                WordCategory.Adjective => adjectiveDeclensionService.GetForm(word),
                WordCategory.Pronoun => pronounService.GetForm(word),
                WordCategory.Numerale => numeralService.GetForm(word),
                _ => throw new NotSupportedException($"Unsupported category: {word.WordCategory}")
            };
        }
    }
}
