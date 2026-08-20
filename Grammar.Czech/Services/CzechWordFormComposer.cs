using Grammar.Core.Enums;
using Grammar.Core.Interfaces;
using Grammar.Core.Models.Word;
using Grammar.Czech.Models;

namespace Grammar.Czech.Services
{
    /// <summary>
    /// Composes final Czech word forms by combining morphology, negation, and verb phrase logic.
    /// </summary>
    public class CzechWordFormComposer : IWordFormComposer<CzechWordRequest>
    {
        private readonly INegationService<CzechWordRequest> negationService;
        private readonly CzechVerbPhraseBuilderService verbPhraseBuilderService;
        private readonly MorphologyEngine morphologyEngine;

        /// <summary>
        /// Initializes a new instance of the <see cref="CzechWordFormComposer"/> type.
        /// </summary>
        public CzechWordFormComposer(CzechVerbPhraseBuilderService verbPhraseBuilderService, INegationService<CzechWordRequest> negationService, MorphologyEngine morphologyEngine)
        {
            this.negationService = negationService;
            this.verbPhraseBuilderService = verbPhraseBuilderService;
            this.morphologyEngine = morphologyEngine;
        }

        /// <summary>
        /// Builds the complete requested word or phrase form.
        /// </summary>
        /// <param name="request">The Czech word request to process.</param>
        /// <returns>The composed word or phrase form.</returns>
        public WordForm GetFullForm(CzechWordRequest request)
        {
            // TODO: Make full form of phrase (especially verb for now). If word is single, return single form.
            WordForm form;
            var verbNegationApplied = false;
            if (request.WordCategory == WordCategory.Verb)
            {
                var verbForm = morphologyEngine.GetBasicForm(request).Form;
                // The particle is added here rather than by the conjugation service, which sees a copy
                // the lexicon has already enriched. Every other tense reads it off this request, and a
                // caller that cleared the field — CzechSentenceBuilder does, to put se in the clitic
                // cluster instead — has to be believed by the imperative too, or it lands twice.
                if (request.Modus == Modus.Imperative)
                {
                    return new WordForm(request.ReflexiveType == ReflexiveType.None
                        ? verbForm
                        : verbPhraseBuilderService.BuildReflexivePhrase(
                            verbForm, request.ReflexiveType, request.HasPrecedingConstituent.GetValueOrDefault()));
                }

                if (request.Diathesis == Diathesis.Resultative)
                {
                    // Mít governs the sentence, not the verb itself: what agrees with the actor is the
                    // auxiliary, and the participle stays neuter singular no matter what is written
                    // (mám napsáno, ne *mám napsán/napsanou) — so it is built via a copy asking for the
                    // periphrastic passive's neuter singular form rather than off request.Voice, which
                    // stays Active here precisely because the clause is not passive.
                    var participleRequest = request;
                    participleRequest.Voice = Voice.Passive;
                    participleRequest.Gender = Gender.Neuter;
                    participleRequest.Number = Number.Singular;

                    var participleForm = morphologyEngine.GetBasicForm(participleRequest).Form;
                    verbForm = verbPhraseBuilderService.BuildResultativePhrase(
                        participleForm, request.Tense, request.Number, request.Person, request.Modus, request.Gender, request.IsNegative);
                    verbNegationApplied = request.IsNegative;
                }
                else if (request.Aspect == VerbAspect.Imperfective && request.Tense == Tense.Future)
                {
                    verbForm = verbPhraseBuilderService.BuildSynteticFuturePhrase(verbForm, request.Number, request.Person, request.Modus, request.Gender, request.IsNegative);
                    verbNegationApplied = request.IsNegative;
                }
                else if (request.Voice == Voice.Passive)
                {
                    if (request.Modus == Modus.Conditional)
                    {
                        verbForm = verbPhraseBuilderService.BuildPassiveConditionalPhrase(verbForm, request.Number, request.Person, request.Modus, request.Gender, request.IsNegative);
                        verbNegationApplied = request.IsNegative;
                    }
                    else
                    {
                        verbForm = verbPhraseBuilderService.BuildPassivePhrase(verbForm, request.Tense, request.Number, request.Person, request.Modus, request.Gender, request.IsNegative);
                        verbNegationApplied = request.IsNegative;
                    }
                }
                else if (request.Modus == Modus.Conditional)
                {
                    verbForm = verbPhraseBuilderService.BuildConditionalPhrase(verbForm, request.Number, request.Person, request.HasPrecedingConstituent.GetValueOrDefault(), request.IsNegative);
                    verbNegationApplied = request.IsNegative;
                }
                else if (request.Tense == Tense.Past)
                {
                    verbForm = verbPhraseBuilderService.BuildPastPhrase(verbForm, request.Number, request.Person, request.HasPrecedingConstituent.GetValueOrDefault(), request.IsNegative);
                    verbNegationApplied = request.IsNegative;
                }

                if (request.ReflexiveType != ReflexiveType.None)
                {
                    verbForm = verbPhraseBuilderService.BuildReflexivePhrase(verbForm, request.ReflexiveType, request.HasPrecedingConstituent.GetValueOrDefault());
                }

                form = new WordForm(verbForm);
            }
            else
            {
                form = morphologyEngine.GetForm(request);
            }

            if (request.IsNegative && !verbNegationApplied)
            {
                form = negationService.ApplyNegation(request, form.Form);
            }

            return form;
        }
    }
}
