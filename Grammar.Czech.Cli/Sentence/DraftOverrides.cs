using Grammar.Core.Enums;

namespace Grammar.Czech.Cli.Sentence
{
    /// <summary>
    /// Collects everything the user stated about the clause, from the command line and from the review.
    /// </summary>
    /// <remarks>
    /// The review writes into the same object the command line filled, which is what makes the two modes
    /// one mode: a session can be replayed as a single non-interactive command, and a scripted call goes
    /// through exactly the code path the dialog would have produced.
    /// </remarks>
    public sealed class DraftOverrides
    {
        private readonly Dictionary<string, WordOverride> _words = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets or sets the lemma to treat as the predicate, when the tool should not decide.
        /// </summary>
        public string? PredicateLemma { get; set; }

        /// <summary>
        /// Gets or sets the valency frame to read the arguments from.
        /// </summary>
        public string? FrameLabel { get; set; }

        /// <summary>
        /// Gets or sets the tense of the predicate.
        /// </summary>
        public Tense? Tense { get; set; }

        /// <summary>
        /// Gets or sets the mood of the predicate.
        /// </summary>
        public Modus? Mood { get; set; }

        /// <summary>
        /// Gets or sets the voice of the predicate.
        /// </summary>
        public Voice? Voice { get; set; }

        /// <summary>
        /// Gets or sets the aspect of the predicate.
        /// </summary>
        public VerbAspect? Aspect { get; set; }

        /// <summary>
        /// Gets or sets the person of the predicate, for a clause with no subject to agree with.
        /// </summary>
        public Person? Person { get; set; }

        /// <summary>
        /// Gets or sets the number of the predicate, for a clause with no subject to agree with.
        /// </summary>
        public Number? Number { get; set; }

        /// <summary>
        /// Gets or sets the gender of the predicate, for a clause with no subject to agree with.
        /// </summary>
        public Gender? Gender { get; set; }

        /// <summary>
        /// Gets or sets the reflexive type of the predicate.
        /// </summary>
        public ReflexiveType? ReflexiveType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the predicate is negated.
        /// </summary>
        public bool? IsNegative { get; set; }

        /// <summary>
        /// Gets or sets the communicative force of the clause.
        /// </summary>
        public SentenceType? SentenceType { get; set; }

        /// <summary>
        /// Gets or sets the punctuation mark closing the sentence.
        /// </summary>
        public string? Terminator { get; set; }

        /// <summary>
        /// Gets what was stated about one word, creating an empty record on first use.
        /// </summary>
        /// <param name="key">The lemma or the one-based position of the word.</param>
        /// <returns>The record for that word.</returns>
        public WordOverride For(string key)
        {
            if (!_words.TryGetValue(key, out var word))
            {
                word = new WordOverride();
                _words[key] = word;
            }

            return word;
        }

        /// <summary>
        /// Finds what was stated about a word, by either of the two ways it can be addressed.
        /// </summary>
        /// <param name="lemma">The lemma of the word.</param>
        /// <param name="position">The one-based position the lemma was entered in.</param>
        /// <returns>The record, or <see langword="null"/> when the word was never mentioned.</returns>
        /// <remarks>
        /// Two keys because a lemma is what a person types and a position is what disambiguates it — the
        /// same word can stand in a clause twice, and then only the number identifies one of them.
        /// </remarks>
        public WordOverride? Find(string lemma, int position)
        {
            if (_words.TryGetValue(position.ToString(), out var byPosition))
            {
                return byPosition;
            }

            return _words.GetValueOrDefault(lemma);
        }

        /// <summary>
        /// Lists the keys that match no word in the clause, which are almost always typos.
        /// </summary>
        /// <param name="lemmas">The lemmas of the clause, in the order they were entered.</param>
        /// <returns>The unmatched keys.</returns>
        public IReadOnlyList<string> UnmatchedKeys(IReadOnlyList<string> lemmas)
        {
            var positions = Enumerable.Range(1, lemmas.Count).Select(position => position.ToString());

            return
            [
                .. _words.Keys
                    .Where(key => !positions.Contains(key, StringComparer.Ordinal)
                        && !lemmas.Contains(key, StringComparer.OrdinalIgnoreCase)),
            ];
        }
    }
}
