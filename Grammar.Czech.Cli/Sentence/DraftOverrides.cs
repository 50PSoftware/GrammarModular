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
        // Skládaný klíč, ne jen bez velikosti písmen: kdo napsal 'ucitel' a v tabulce vidí 'učitel',
        // musí ho umět opravit kteroukoli z těch dvou podob.
        private readonly Dictionary<string, WordOverride> _words = new(Terms.LemmaComparer);
        private readonly Dictionary<int, int> _attachments = [];
        private readonly Dictionary<int, PredicateOverride> _predicates = [];
        private readonly Dictionary<int, int> _relatives = [];
        private readonly Dictionary<int, string> _relativizers = [];

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
        /// Gets the constituent each relative clause was moved onto, keyed by the constituent's position
        /// and naming the relative clause that is to hang there.
        /// </summary>
        /// <remarks>
        /// Unlisted, a relative clause hangs off the last constituent of the clause before it, which is
        /// how a reader takes it — the pronoun reaches for the nearest preceding noun. This is only for
        /// saying that it reaches further back.
        /// </remarks>
        public IReadOnlyDictionary<int, int> Relatives => _relatives;

        /// <summary>
        /// Gets the relativizer each relative clause was given, keyed by the constituent it hangs off.
        /// </summary>
        public IReadOnlyDictionary<int, string> Relativizers => _relativizers;

        /// <summary>
        /// States that a relative clause hangs off a constituent.
        /// </summary>
        /// <param name="member">The one-based position of the constituent it is to hang off.</param>
        /// <param name="relative">The one-based number of the relative clause.</param>
        /// <exception cref="CliException">Thrown when the constituent already carries another one.</exception>
        public void Hang(int member, int relative)
        {
            // Jeden člen unese jednu vztažnou větu: druhá by se neměla kam připojit a která z nich by
            // vyhrála, by rozhodlo pořadí přepínačů na řádce, což není odpověď.
            if (_relatives.TryGetValue(member, out var taken) && taken != relative)
            {
                throw new CliException(
                    $"Na člen {member} už visí vztažná věta {taken}, takže {relative} se tam nevejde.");
            }

            _relatives[member] = relative;
        }

        /// <summary>
        /// States which word introduces the relative clause hanging off a constituent.
        /// </summary>
        /// <param name="member">The one-based position of the constituent it hangs off.</param>
        /// <param name="relativizer">The lemma to introduce it with.</param>
        public void Introduce(int member, string relativizer) => _relativizers[member] = relativizer;

        /// <summary>
        /// Gets or sets the lemma to treat as the predicate, when the tool should not decide.
        /// </summary>
        public string? PredicateLemma { get; set; }

        /// <summary>
        /// Gets what was stated about every predicate in the sentence.
        /// </summary>
        /// <remarks>
        /// A statement about the sentence rather than about one clause: <c>--cas minuly</c> puts the
        /// whole thing in the past. A clause that says otherwise wins over it.
        /// </remarks>
        public PredicateOverride Predicate { get; } = new();

        /// <summary>
        /// Gets what was stated about the predicate of one clause, creating an empty record on first use.
        /// </summary>
        /// <param name="ordinal">The one-based number of the clause.</param>
        /// <returns>The record for that clause.</returns>
        public PredicateOverride PredicateOf(int ordinal)
        {
            if (!_predicates.TryGetValue(ordinal, out var predicate))
            {
                predicate = new PredicateOverride();
                _predicates[ordinal] = predicate;
            }

            return predicate;
        }

        /// <summary>
        /// Gets everything that applies to the predicate of one clause, its own word first.
        /// </summary>
        /// <param name="ordinal">The one-based number of the clause.</param>
        /// <returns>The combined record.</returns>
        public PredicateOverride PredicateFor(int ordinal) =>
            _predicates.TryGetValue(ordinal, out var predicate) ? predicate.Over(Predicate) : Predicate;

        /// <summary>
        /// Lists the clause numbers singled out, so that a number naming no clause can be reported.
        /// </summary>
        public IReadOnlyCollection<int> SingledOutClauses => _predicates.Keys;


        /// <summary>
        /// Forgets everything said about individual words and about how the clauses hang together.
        /// </summary>
        /// <remarks>
        /// What the session keeps between sentences and what it cannot. A statement about the predicate
        /// — past tense, conditional — is about the sentence being built and holds for the next one just
        /// as well. A statement about a word is addressed by lemma or by position, and the next sentence
        /// has different words in those positions, so carrying it over would silently apply it to
        /// something else. The same for an attachment, which numbers clauses that will not be there.
        /// </remarks>
        public void ForgetWords()
        {
            _words.Clear();
            _attachments.Clear();
            _predicates.Clear();
            _relatives.Clear();
            _relativizers.Clear();
            PredicateLemma = null;
        }

        /// <summary>
        /// Forgets everything, leaving the record as it was built.
        /// </summary>
        public void ForgetAll()
        {
            ForgetWords();

            Predicate.Forget();
            SentenceType = null;
            Terminator = null;
        }

        /// <summary>
        /// Describes what is still in force, for a session to show on request.
        /// </summary>
        /// <returns>The statements in force, one per line, empty when nothing is set.</returns>
        public IReadOnlyList<string> Describe()
        {
            List<string> lines = [.. Predicate.Describe()];

            if (SentenceType is { } type)
            {
                lines.Add($"typ = {Terms.Name(type)}");
            }

            if (Terminator is { } terminator)
            {
                lines.Add($"konec = {terminator}");
            }

            return lines;
        }

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
                        && !lemmas.Contains(key, Terms.LemmaComparer)),
            ];
        }
    }
}
