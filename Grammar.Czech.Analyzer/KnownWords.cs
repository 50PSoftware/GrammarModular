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
    /// noun or adjective. Without it, a text repeating <c>město</c> across several cases proposes
    /// <c>měst</c>, <c>města</c>, <c>městu</c>, <c>měst</c>u... as new lemmas in their own right — the
    /// matcher has no way to know they are forms of a word already on file, only that the exact string
    /// "město" is. Expanding every known noun/adjective's own paradigm once at startup, the same way
    /// <see cref="Candidates.NounMatcher"/> expands a hypothesis, closes that hole with the same
    /// mechanism rather than a new one.
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

            Add(services.GetRequiredService<IPronounDataProvider>().GetPronouns().Keys);
            Add(services.GetRequiredService<IPronounDataProvider>().GetParadigms().Keys);
            Add(services.GetRequiredService<INumeralDataProvider>().GetNumerals().Keys);
            Add(services.GetRequiredService<INumeralDataProvider>().GetParadigms().Keys);
            Add(services.GetRequiredService<IAdverbDataProvider>().GetAdverbs().Keys);
            Add(services.GetRequiredService<IConjunctionDataProvider>().GetConjunctions().Keys);
            Add(services.GetRequiredService<IPrepositionDataProvider>().GetPrepositions().Keys);
            Add(services.GetRequiredService<IParticleDataProvider>().GetParticles().Keys);
            Add(services.GetRequiredService<IInterjectionDataProvider>().GetInterjections().Keys);

            var lexicon = services.GetRequiredService<IValencyProvider<CzechLexicalEntry>>();
            var nounService = services.GetRequiredService<CzechNounDeclensionService>();
            var adjectiveService = services.GetRequiredService<CzechAdjectiveDeclensionService>();

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

        private void TryAddForm(Func<string> generate)
        {
            try
            {
                _words.Add(Fold(generate()));
            }
            catch (InvalidOperationException)
            {
            }
            catch (NotSupportedException)
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
