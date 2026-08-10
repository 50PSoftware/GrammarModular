using Grammar.Core.Enums;
using Grammar.Core.Models.Valency;

namespace Grammar.Core.Interfaces
{
    /// <summary>
    /// Defines the contract for Valency Provider.
    /// </summary>
    public interface IValencyProvider<T> where T : class
    {
        /// <summary>
        /// Returns the lexical entry for the given lemma, or <c>null</c> if not registered.
        /// </summary>
        /// <param name="lemma">The dictionary form of the word (case-insensitive).</param>
        T? GetEntry(string lemma);

        /// <summary>
        /// Returns the lexical entry for the given lemma in the given word class, or <c>null</c>.
        /// </summary>
        /// <remarks>
        /// A lemma can be held under two word classes — stát the country and stát the verb — and the two
        /// rows share nothing. Without the class, a lookup returns whichever it finds.
        /// </remarks>
        /// <param name="lemma">The dictionary form of the word (case-insensitive).</param>
        /// <param name="category">The word class to look the lemma up in.</param>
        T? GetEntry(string lemma, WordCategory category);

        /// <summary>
        /// Returns all valency frames registered for the given verb lemma.
        /// Returns an empty sequence when the lemma has no registered frames.
        /// </summary>
        /// <param name="verbLemma">The infinitive form of the verb (case-insensitive).</param>
        IEnumerable<ValencyFrame> GetFrames(string verbLemma);

        /// <summary>
        /// Returns <c>true</c> when a lexical entry exists for the given lemma.
        /// </summary>
        /// <param name="lemma">The dictionary form of the word (case-insensitive).</param>
        bool HasEntry(string lemma);

        /// <summary>
        /// Returns every entry the lexicon holds, in lemma order.
        /// </summary>
        /// <remarks>
        /// For callers that have to answer a question about the dictionary as a whole rather than about
        /// one lemma — matching a word written without diacritics against the lemmas that could have
        /// produced it, or building an index of forms. Neither can be asked one lookup at a time,
        /// because the question is which lemma to look up.
        /// <para>
        /// The sequence is meant to be enumerated once and turned into whatever the caller actually
        /// needs. An implementation may stream it straight off storage, so enumerating it repeatedly is
        /// the caller paying for the same scan again.
        /// </para>
        /// </remarks>
        IEnumerable<T> GetEntries();
    }
}
