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
    }
}
