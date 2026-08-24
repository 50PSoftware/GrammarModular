using Grammar.Core.Interfaces;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Grammar.Czech.Analyzer
{
    /// <summary>
    /// Everything the matcher should treat as already known, so it never proposes a word the
    /// dictionary or the closed-class rules already hold.
    /// </summary>
    /// <remarks>
    /// Two different kinds of "known" feed this, and both matter: <c>lemma_entry</c> (the open
    /// classes — nouns, adjectives, verbs) via <see cref="IValencyProvider{T}"/>, and the closed
    /// classes (pronouns, numerals, prepositions, conjunctions, particles, interjections, adverbs)
    /// via their own data providers, which are already keyed by lemma — no JSON parsing of our own
    /// needed, and no risk of missing a category the way a hand-picked file list would.
    /// <para>
    /// Clitics (bych/bys/.../jsem/jsi/.../si/se) have no provider that exposes them as a flat lemma
    /// set — the paradigm is five words and never grows, so it is listed here directly rather than
    /// built a provider just to enumerate it once.
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

        private readonly HashSet<string> _words;

        /// <summary>
        /// Initializes a new instance of the <see cref="KnownWords"/> type, loading every lemma the
        /// resolved services know about.
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

            foreach (var entry in lexicon.GetEntries())
            {
                _words.Add(Fold(entry.Lemma));
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
