using Grammar.Core.Enums;
using Grammar.Core.Interfaces;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;
using Grammar.Czech.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Grammar.Czech.Analyzer
{
    /// <summary>
    /// Everything the matcher should treat as already known, so it never proposes a word the
    /// dictionary or the closed-class rules already hold.
    /// </summary>
    /// <remarks>
    /// Three different kinds of "known" feed this. <c>lemma_entry</c> (the open classes — nouns,
    /// adjectives, verbs) via <see cref="IValencyProvider{T}"/>, and the closed classes (pronouns,
    /// numerals, prepositions, conjunctions, particles, interjections, adverbs) via their own data
    /// providers, which are already keyed by lemma — no JSON parsing of our own needed, and no risk
    /// of missing a category the way a hand-picked file list would.
    /// <para>
    /// Clitics (bych/bys/.../jsem/jsi/.../si/se) have no provider that exposes them as a flat lemma
    /// set — the paradigm is five words and never grows, so it is listed here directly rather than
    /// built a provider just to enumerate it once.
    /// </para>
    /// <para>
    /// The third kind is the one a lemma-only set misses: every generated form of an already-known
    /// noun, adjective, verb, pronoun or numeral. Without it, a text repeating <c>město</c> across
    /// several cases proposes <c>měst</c>, <c>města</c>, <c>městu</c>... as new lemmas in their own
    /// right — the matcher has no way to know they are forms of a word already on file, only that the
    /// exact string "město" is. Expanding every known word's own paradigm once at startup, the same
    /// way <see cref="Candidates.NounMatcher"/>/<see cref="Candidates.VerbMatcher"/> expand a
    /// hypothesis, closes that hole with the same mechanism rather than a new one.
    /// </para>
    /// <para>
    /// Pronouns and numerals were missed by the first pass of this fix, and a real article found it
    /// within a day: "který" is a registered pronoun (<c>Pronouns/patterns.json</c>, declining as the
    /// adjective pattern mladý), so the bare lemma was excluded — but "která"/"které"/"kterému" were
    /// not, since only the lemma itself was ever added, never its declension. <c>AdjectiveMatcher</c>
    /// then folded those gender endings straight back to "který" and proposed it as if it were a gap.
    /// <see cref="ICzechPronounService"/> and <see cref="ICzechNumeralService"/> already generate a
    /// lemma's forms on request (returning <see langword="null"/> for a combination that does not
    /// apply, never throwing) precisely because pronoun and numeral paradigms are irregular enough
    /// that nothing else could derive them — which is exactly what expanding them here needs.
    /// </para>
    /// <para>
    /// Checked the rest of the closed classes for the same hole and found two more, smaller ones.
    /// Adverbs compare (rychle → rychleji → nejrychleji), and <see cref="ICzechAdverbService"/> already
    /// exposes every registered comparative for a lemma — the superlative is "nej" plus each of those,
    /// which nothing generates on its own, so it is built here. Prepositions have a vocalized variant
    /// before an awkward cluster (s/se, v/ve, k/ke...), stored right on <see cref="PrepositionData"/>
    /// as a plain string — no service needed, just reading a field the provider already returns.
    /// Conjunctions and particles were checked too and are genuinely one form per lemma. Interjections
    /// mostly are as well, except that some name a verb nothing else registers at all — "hop" names
    /// "hopnout" in its <c>DerivedVerb</c> field, and that verb is in no lexicon entry, so it is not a
    /// missing-forms gap like the others, it is a missing-word one. These are all onomatopoeic -nout
    /// coinages, so <see cref="CzechVerbConjugationService.GuessVerbClass"/> reads the class off the
    /// ending reliably and the derived verb gets the same paradigm expansion a real lexicon verb would.
    /// </para>
    /// </remarks>
    public sealed class KnownWords
    {
        private static readonly string[] Clitics =
        [
            "bych", "bys", "by", "bychom", "byste",
            "jsem", "jsi", "jsme", "jste",
            "si", "se",
        ];

        private static readonly Number[] Numbers = [Number.Singular, Number.Plural];

        private static readonly Case[] Cases =
        [
            Case.Nominative, Case.Genitive, Case.Dative, Case.Accusative,
            Case.Vocative, Case.Locative, Case.Instrumental,
        ];

        private static readonly (Gender Gender, bool? IsAnimate)[] AdjectiveGenderSlots =
        [
            (Gender.Masculine, true),
            (Gender.Masculine, false),
            (Gender.Feminine, null),
            (Gender.Neuter, null),
        ];

        private readonly HashSet<string> _words;

        /// <summary>
        /// Initializes a new instance of the <see cref="KnownWords"/> type, loading every lemma and
        /// every generated noun/adjective form the resolved services know about.
        /// </summary>
        /// <param name="services">The service provider grammar services were registered on.</param>
        public KnownWords(IServiceProvider services)
        {
            _words = [];

            foreach (var lemma in Clitics)
            {
                _words.Add(lemma);
            }

            var pronounProvider = services.GetRequiredService<IPronounDataProvider>();
            var pronounService = services.GetRequiredService<ICzechPronounService>();

            Add(pronounProvider.GetPronouns().Keys);
            Add(pronounProvider.GetParadigms().Keys);

            foreach (var lemma in pronounProvider.GetPronouns().Keys)
            {
                AddPronounForms(pronounService, lemma);
            }

            var numeralProvider = services.GetRequiredService<INumeralDataProvider>();
            var numeralService = services.GetRequiredService<ICzechNumeralService>();

            Add(numeralProvider.GetNumerals().Keys);
            Add(numeralProvider.GetParadigms().Keys);

            foreach (var lemma in numeralProvider.GetNumerals().Keys)
            {
                AddNumeralForms(numeralService, lemma);
            }

            var adverbProvider = services.GetRequiredService<IAdverbDataProvider>();
            var adverbService = services.GetRequiredService<ICzechAdverbService>();

            Add(adverbProvider.GetAdverbs().Keys);

            foreach (var lemma in adverbProvider.GetAdverbs().Keys)
            {
                AddAdverbForms(adverbService, lemma);
            }

            Add(services.GetRequiredService<IConjunctionDataProvider>().GetConjunctions().Keys);
            Add(services.GetRequiredService<IParticleDataProvider>().GetParticles().Keys);

            var interjectionProvider = services.GetRequiredService<IInterjectionDataProvider>();

            Add(interjectionProvider.GetInterjections().Keys);

            var verbServiceForInterjections = services.GetRequiredService<CzechVerbConjugationService>();

            foreach (var interjection in interjectionProvider.GetInterjections().Values)
            {
                if (interjection.DerivedVerb is { } derivedVerb)
                {
                    AddInterjectionDerivedVerb(verbServiceForInterjections, derivedVerb);
                }
            }

            var prepositionProvider = services.GetRequiredService<IPrepositionDataProvider>();

            Add(prepositionProvider.GetPrepositions().Keys);

            foreach (var preposition in prepositionProvider.GetPrepositions().Values)
            {
                if (preposition.Vocalized is { } vocalized)
                {
                    _words.Add(Fold(vocalized));
                }
            }

            var lexicon = services.GetRequiredService<IValencyProvider<CzechLexicalEntry>>();
            var nounService = services.GetRequiredService<CzechNounDeclensionService>();
            var adjectiveService = services.GetRequiredService<CzechAdjectiveDeclensionService>();
            var verbService = services.GetRequiredService<CzechVerbConjugationService>();

            foreach (var entry in lexicon.GetEntries())
            {
                _words.Add(Fold(entry.Lemma));

                if (entry.Pattern is not { } pattern)
                {
                    continue;
                }

                if (entry.Category == WordCategory.Noun)
                {
                    AddNounForms(nounService, entry.Lemma, pattern, entry.Gender, entry.IsAnimate);
                }
                else if (entry.Category == WordCategory.Adjective)
                {
                    AddAdjectiveForms(adjectiveService, entry.Lemma, pattern);
                }
                else if (entry.Category == WordCategory.Verb)
                {
                    AddVerbForms(verbService, entry.Lemma, pattern);
                }
            }
        }

        private void AddNounForms(
            CzechNounDeclensionService service, string lemma, string pattern, Gender? gender, bool? isAnimate)
        {
            if (gender is null)
            {
                return;
            }

            foreach (var number in Numbers)
            {
                foreach (var @case in Cases)
                {
                    TryAddForm(() => service.GetForm(new CzechWordRequest
                    {
                        Lemma = lemma,
                        Pattern = pattern,
                        Gender = gender,
                        IsAnimate = isAnimate,
                        Case = @case,
                        Number = number,
                        WordCategory = WordCategory.Noun,
                    }).Form);
                }
            }
        }

        private void AddAdjectiveForms(CzechAdjectiveDeclensionService service, string lemma, string pattern)
        {
            foreach (var number in Numbers)
            {
                foreach (var @case in Cases)
                {
                    foreach (var (gender, isAnimate) in AdjectiveGenderSlots)
                    {
                        TryAddForm(() => service.GetForm(new CzechWordRequest
                        {
                            Lemma = lemma,
                            Pattern = pattern,
                            Gender = gender,
                            IsAnimate = isAnimate,
                            Case = @case,
                            Number = number,
                            Degree = Degree.Positive,
                            WordCategory = WordCategory.Adjective,
                        }).Form);
                    }
                }
            }
        }

        private void AddPronounForms(ICzechPronounService service, string lemma)
        {
            foreach (var @case in Cases)
            {
                TryAddForm(() => service.TryGetForm(lemma, @case, null, null, null, null));

                foreach (var (gender, isAnimate) in AdjectiveGenderSlots)
                {
                    foreach (var number in Numbers)
                    {
                        TryAddForm(() => service.TryGetForm(lemma, @case, gender, number, isAnimate, null));
                    }
                }
            }
        }

        private void AddNumeralForms(ICzechNumeralService service, string lemma)
        {
            foreach (var @case in Cases)
            {
                TryAddForm(() => service.TryGetForm(lemma, @case, null, null, null, null));

                foreach (var (gender, isAnimate) in AdjectiveGenderSlots)
                {
                    foreach (var number in Numbers)
                    {
                        TryAddForm(() => service.TryGetForm(lemma, @case, gender, number, isAnimate, null));
                    }
                }
            }
        }

        // "nej" + comparative je odvození, ne data — GetForm(Degree.Superlative) to dělá interně jen pro
        // jeden (kanonický) komparativ. GetComparativeVariants dá všechny (snadno → snáze i snadněji),
        // takže se to samo zopakuje tady, aby known-set znal superlativ z každé varianty, ne jen z první.
        private void AddAdverbForms(ICzechAdverbService service, string lemma)
        {
            foreach (var comparative in service.GetComparativeVariants(lemma))
            {
                _words.Add(Fold(comparative));
                _words.Add(Fold("nej" + comparative));
            }
        }

        private void AddVerbForms(CzechVerbConjugationService service, string lemma, string pattern)
        {
            foreach (var request in Candidates.VerbForms.Requests(lemma, pattern))
            {
                TryAddForm(() => service.GetBasicForm(request).Form);
            }
        }

        // interjections.json names a verb (hop -> hopnout) that lemma_entry has never heard of — not
        // missing forms, missing entirely. It has no Pattern to read the way a real lexicon verb does,
        // but these are all onomatopoeic -nout coinages (hopnout, bácnout, ťuknout...), so GuessVerbClass
        // reads trida2 off the ending reliably; the fallback just keeps the bare infinitive known rather
        // than proposing nothing, on the rare entry the guess cannot place.
        private void AddInterjectionDerivedVerb(CzechVerbConjugationService service, string lemma)
        {
            _words.Add(Fold(lemma));

            if (service.GuessVerbClass(lemma) is { } verbClass
                && CzechVerbConjugationService.PatternByVerbClass.TryGetValue(verbClass, out var pattern))
            {
                AddVerbForms(service, lemma, pattern);
            }
        }

        private void TryAddForm(Func<string?> generate)
        {
            try
            {
                if (generate() is { } form)
                {
                    _words.Add(Fold(form));
                }
            }
            catch (InvalidOperationException)
            {
            }
            catch (NotSupportedException)
            {
            }
            catch (ArgumentException)
            {
            }
        }

        /// <summary>
        /// Returns whether the given word is already known, under any category.
        /// </summary>
        /// <param name="word">The word to check, in any casing.</param>
        public bool IsKnown(string word) => _words.Contains(Fold(word));

        private void Add(IEnumerable<string> lemmas)
        {
            foreach (var lemma in lemmas)
            {
                _words.Add(Fold(lemma));
            }
        }

        private static string Fold(string word) => word.ToLowerInvariant();
    }
}
