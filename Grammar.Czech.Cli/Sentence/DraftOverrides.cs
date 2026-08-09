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
        private readonly Dictionary<int, int> _attachments = [];

        /// <summary>
        /// Gets the clauses whose attachment the user moved, keyed by the clause and naming the one it
        /// is to hang off.
        /// </summary>
        /// <remarks>
        /// Numbered by clause rather than by word, because a clause is what attaches. Unlisted, a clause
        /// hangs off the one before it — which is how a reader takes it — so this is only for saying
        /// that something reaches further back.
        /// </remarks>
        public IReadOnlyDictionary<int, int> Attachments => _attachments;

        /// <summary>
        /// States that a clause hangs off another.
        /// </summary>
        /// <param name="clause">The one-based number of the clause being attached.</param>
        /// <param name="parent">The one-based number of the clause it hangs off.</param>
        /// <exception cref="CliException">Thrown when the attachment cannot hold.</exception>
        public void Attach(int clause, int parent)
        {
            if (clause <= 1)
            {
                throw new CliException(
                    "První klauze se nepřipojuje k ničemu — je to ta, ke které se připojuje zbytek.");
            }

            // Dopředu ani na sebe: klauze visí na něčem, co už bylo řečeno, jinak by ve stromu vznikl
            // cyklus a věta by neměla kořen.
            if (parent >= clause)
            {
                throw new CliException(
                    $"Klauze {clause} se nemůže připojit ke klauzi {parent} — připojuje se vždycky "
                    + "k něčemu, co stojí před ní.");
            }

            _attachments[clause] = parent;
        }

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
        /// Gets or sets a value indicating whether a subject pronoun that adds nothing may be dropped.
        /// </summary>
        /// <remarks>
        /// Off unless asked for, unlike the library's default. A tool that was handed a word and did not
        /// print it looks like it lost it, whereas a library consumer is building a sentence and wants
        /// the neutral Czech one.
        /// </remarks>
        public bool? DropSubject { get; set; }

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
