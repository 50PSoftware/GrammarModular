using Grammar.Core.Interfaces;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;

namespace Grammar.Czech.Cli.Sentence
{
    /// <summary>
    /// Finds the lemma behind a word written without diacritics or in another case.
    /// </summary>
    /// <remarks>
    /// Czech is unpleasant to type on a keyboard that is not set up for it, and <c>ucitel</c> is what a
    /// person writes when they mean <c>učitel</c>. Folding it away lives here rather than in the lexicon
    /// because it is a convenience of the tool and not a property of the dictionary: the lexicon is
    /// keyed exactly on purpose, since a culture collation treats <em>ch</em> as one unit and an
    /// accent-insensitive one makes <c>dát</c> and <c>dat</c> the same word.
    /// <para>
    /// The closed classes are folded alongside the dictionary, because they are the words a person is
    /// least able to look up and least able to type: <c>ktery</c>, <c>protoze</c>, <c>jenz</c>. They live
    /// in the rule files rather than in the lexicon, so folding only the lexicon left exactly the wrong
    /// half uncovered — <c>ktery</c> came back as a guessed noun and the sentence failed complaining
    /// about verbs.
    /// </para>
    /// <para>
    /// Which is also why folding is a fallback and never a first choice. <c>být</c> and <c>byt</c> fold
    /// together and are unrelated words, so an exact hit always wins, and a fold matching several lemmas
    /// is a question rather than a decision.
    /// </para>
    /// </remarks>
    public sealed class LemmaLookup
    {
        private readonly IValencyProvider<CzechLexicalEntry> _lexicon;
        private readonly Func<IEnumerable<string>> _ruleLemmas;

        // Postavené až při prvním minutí, ne v konstruktoru: běžný běh trefí každé slovo přesně a projít
        // kvůli tomu celý slovník by byla práce navíc pro každé spuštění nástroje.
        private readonly Lazy<IReadOnlyDictionary<string, IReadOnlyList<string>>> _folded;
        private readonly Lazy<HashSet<string>> _exact;

        /// <summary>
        /// Initializes a new instance of the <see cref="LemmaLookup"/> type.
        /// </summary>
        /// <param name="lexicon">The lexicon to look words up in.</param>
        /// <param name="pronouns">The pronoun data, one of the closed classes folded alongside it.</param>
        /// <param name="prepositions">The preposition data.</param>
        /// <param name="conjunctions">The conjunction data.</param>
        /// <param name="numerals">The numeral data.</param>
        /// <param name="adverbs">The adverb data.</param>
        /// <param name="particles">The particle data.</param>
        /// <param name="interjections">The interjection data.</param>
        public LemmaLookup(
            IValencyProvider<CzechLexicalEntry> lexicon,
            IPronounDataProvider pronouns,
            IPrepositionDataProvider prepositions,
            IConjunctionDataProvider conjunctions,
            INumeralDataProvider numerals,
            IAdverbDataProvider adverbs,
            IParticleDataProvider particles,
            IInterjectionDataProvider interjections)
        {
            _lexicon = lexicon;
            _ruleLemmas = () =>
            [
                .. pronouns.GetPronouns().Keys,
                .. prepositions.GetPrepositions().Keys,
                .. conjunctions.GetConjunctions().Keys,
                .. numerals.GetNumerals().Keys,
                .. adverbs.GetAdverbs().Keys,
                .. particles.GetParticles().Keys,
                .. interjections.GetInterjections().Keys,
            ];

            _folded = new Lazy<IReadOnlyDictionary<string, IReadOnlyList<string>>>(BuildIndex);
            _exact = new Lazy<HashSet<string>>(
                () => new HashSet<string>(_ruleLemmas(), StringComparer.Ordinal));
        }

        /// <summary>
        /// Resolves a written word to the lemma it is held under.
        /// </summary>
        /// <param name="written">The word as the user typed it.</param>
        /// <returns>What the dictionary and the rule data have to say about that spelling.</returns>
        public LemmaMatch Resolve(string written)
        {
            // Přesný zásah vyhrává, a hledá se i v pravidlech: 'který' napsané správně je hotové lemma,
            // ne něco, co se má doplňovat — jinak by se hlásilo 'doplnil jsem který → který'.
            if (_lexicon.HasEntry(written) || _exact.Value.Contains(written))
            {
                return new LemmaMatch(written, false, []);
            }

            if (!_folded.Value.TryGetValue(Terms.Plain(written), out var candidates))
            {
                // Neznámé slovo není chyba — většina češtiny ve slovníku není a odhad ze zakončení je
                // přesně to, co se s ním má stát.
                return new LemmaMatch(written, false, []);
            }

            return candidates.Count == 1
                ? new LemmaMatch(candidates[0], true, [])
                : new LemmaMatch(written, false, candidates);
        }

        private IReadOnlyDictionary<string, IReadOnlyList<string>> BuildIndex()
        {
            var index = new Dictionary<string, List<string>>(StringComparer.Ordinal);

            // Podle lemmatu, ne podle hesla: 'stát' je podstatné jméno i sloveso a to jsou dvě hesla
            // téhož zápisu. Pro otázku „jak se to píše“ je to jedna odpověď, ne dvě. Kolize mezi
            // slovníkem a pravidly se tím řeší sama: dvě lemmata pod jedním složením jsou otázka.
            foreach (var lemma in _lexicon.GetEntries().Select(entry => entry.Lemma).Concat(_ruleLemmas()))
            {
                var folded = Terms.Plain(lemma);

                if (!index.TryGetValue(folded, out var lemmas))
                {
                    index[folded] = lemmas = [];
                }

                if (!lemmas.Contains(lemma, StringComparer.Ordinal))
                {
                    lemmas.Add(lemma);
                }
            }

            return index.ToDictionary(
                pair => pair.Key,
                pair => (IReadOnlyList<string>)pair.Value,
                StringComparer.Ordinal);
        }
    }

    /// <summary>
    /// What the dictionary makes of one written word.
    /// </summary>
    /// <param name="Lemma">
    /// The lemma to work with: the word as written when it was already a lemma or is unknown, and the
    /// dictionary spelling when the tool filled the diacritics in.
    /// </param>
    /// <param name="Completed">
    /// <see langword="true"/> when the spelling was completed from the dictionary, which is worth saying
    /// out loud — the sentence will contain a word the user did not write.
    /// </param>
    /// <param name="Candidates">
    /// The lemmas the spelling could stand for, when it could stand for more than one. Empty otherwise.
    /// </param>
    public readonly record struct LemmaMatch(
        string Lemma,
        bool Completed,
        IReadOnlyList<string> Candidates);
}
