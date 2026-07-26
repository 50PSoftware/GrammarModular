using Grammar.Core.Enums;

namespace Grammar.Czech.Models.Syntax
{
    /// <summary>
    /// Represents a single non-predicate constituent of a clause together with the information it carries.
    /// </summary>
    public sealed record ClauseElement
    {
        /// <summary>
        /// Gets the word request that produces the surface form of the constituent.
        /// </summary>
        public CzechWordRequest Word { get; init; }

        /// <summary>
        /// Gets the FGD functor the constituent fills.
        /// </summary>
        public FgdFunctor Functor { get; init; }

        /// <summary>
        /// Gets the communicative status that decides where the constituent lands relative to the verb.
        /// </summary>
        public InformationStatus Status { get; init; } = InformationStatus.New;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClauseElement"/> type.
        /// </summary>
        /// <param name="word">The word request that produces the surface form.</param>
        /// <param name="functor">The FGD functor the constituent fills.</param>
        /// <param name="status">The communicative status of the constituent.</param>
        /// <returns>The clause element.</returns>
        public static ClauseElement Of(CzechWordRequest word, FgdFunctor functor, InformationStatus status = InformationStatus.New)
            => new() { Word = word, Functor = functor, Status = status };
    }
}
