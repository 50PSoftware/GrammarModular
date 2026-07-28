using Grammar.Core.Enums;

namespace Grammar.Czech.Interfaces
{
    /// <summary>
    /// Defines lookup operations for Czech interjections.
    /// </summary>
    public interface ICzechInterjectionService
    {
        /// <summary>
        /// Determines whether the supplied lemma is a registered interjection.
        /// </summary>
        /// <param name="lemma">The dictionary form to look up.</param>
        /// <returns><see langword="true"/> when the lemma is in the inventory; otherwise, <see langword="false"/>.</returns>
        bool IsInterjection(string lemma);

        /// <summary>
        /// Gets what the supplied interjection does in the discourse.
        /// </summary>
        /// <param name="lemma">The dictionary form to look up.</param>
        /// <returns>The interjection type.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown when the lemma is not in the inventory.</exception>
        InterjectionType GetInterjectionType(string lemma);

        /// <summary>
        /// Determines whether the interjection can stand as the predicate of its clause.
        /// </summary>
        /// <param name="lemma">The dictionary form to look up.</param>
        /// <returns><see langword="true"/> when it takes objects and adjuncts as a verb would; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// False for an unregistered lemma. Predicative use is a property of the particular word rather than
        /// something a new coinage inherits, so a word the data does not know is not assumed to have it.
        /// </remarks>
        bool CanBePredicate(string lemma);

        /// <summary>
        /// Determines whether a comma separates the interjection from the rest of the sentence.
        /// </summary>
        /// <param name="lemma">The dictionary form to look up.</param>
        /// <param name="asPredicate">Whether the interjection is being used as the predicate.</param>
        /// <returns><see langword="true"/> unless the interjection stands in for a clause member.</returns>
        /// <remarks>
        /// The ÚJČ rule, and it turns on the use rather than on the word: an interjection is set off by a
        /// comma except where it replaces a clause member, usually the predicate. "Kamarádi, hurá, vyhráli
        /// jsme" against "Palicí buch ho po hlavě". So the same word is punctuated both ways and no entry in
        /// the data could have settled it.
        /// </remarks>
        bool RequiresComma(string lemma, bool asPredicate);

        /// <summary>
        /// Gets how the interjection repeats.
        /// </summary>
        /// <param name="lemma">The dictionary form to look up.</param>
        /// <returns>The reduplication recorded for it.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown when the lemma is not in the inventory.</exception>
        Reduplication GetReduplication(string lemma);

        /// <summary>
        /// Gets the verb formed from the interjection.
        /// </summary>
        /// <param name="lemma">The dictionary form to look up.</param>
        /// <returns>The derived verb, or <see langword="null"/> when none is registered.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown when the lemma is not in the inventory.</exception>
        string? GetDerivedVerb(string lemma);

        /// <summary>
        /// Gets every registered interjection of the supplied type.
        /// </summary>
        /// <param name="type">The interjection type to select.</param>
        /// <returns>The lemmas registered under that type.</returns>
        IReadOnlyList<string> GetInterjectionsOfType(InterjectionType type);
    }
}
