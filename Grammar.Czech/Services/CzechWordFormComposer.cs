using Grammar.Core.Enums;
using Grammar.Core.Interfaces;
using Grammar.Core.Models.Word;
using Grammar.Czech.Models;

namespace Grammar.Czech.Services
{
    public class CzechWordFormComposer : IWordFormComposer<CzechWordRequest>
    {
        private readonly INegationService<CzechWordRequest> negationService;
        private readonly CzechVerbPhraseBuilderService verbPhraseBuilderService;
        private readonly MorphologyEngine morphologyEngine;

        public CzechWordFormComposer(CzechVerbPhraseBuilderService verbPhraseBuilderService, INegationService<CzechWordRequest> negationService, MorphologyEngine morphologyEngine)
        {
            this.negationService = negationService;
            this.verbPhraseBuilderService = verbPhraseBuilderService;
            this.morphologyEngine = morphologyEngine;
        }

        public WordForm GetFullForm(CzechWordRequest request)
        {
            // TODO: Make full form of phrase (especially verb for now). If word is single, return single form.
            WordForm form;
            var verbNegationApplied = false;
            if (request.WordCategory == WordCategory.Verb)
            {
                var verbForm = morphologyEngine.GetBasicForm(request).Form;
                if (request.Modus == Modus.Imperative) return new WordForm(verbForm);

                if (request.Aspect == VerbAspect.Imperfective && request.Tense == Tense.Future)
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
                    verbForm = verbPhraseBuilderService.BuildConditionalPhrase(verbForm, request.Number, request.Person, request.HasExplicitSubject.GetValueOrDefault(), request.IsNegative);
                    verbNegationApplied = request.IsNegative;
                }

                if (request.HasReflexive.GetValueOrDefault())
                {
                    verbForm = verbPhraseBuilderService.BuildReflexivePhrase(verbForm, (request.Case == Case.Dative));
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
