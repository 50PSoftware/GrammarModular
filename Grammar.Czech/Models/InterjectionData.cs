using Grammar.Core.Enums;

namespace Grammar.Czech.Models
{
    /// <summary>
    /// Represents Czech interjection metadata loaded from JSON data.
    /// </summary>
    /// <remarks>
    /// No paradigm and no derivation. An interjection is uninflected, and NESČ describes it as forming the
    /// most primitive sentence type there is — a whole utterance with neither a verbal nor a nominal head.
    /// What the entry records is how it behaves when it does enter a clause.
    /// <para>
    /// The class is open in a way the others are not: onomatopoeia is coined on the spot, so an unregistered
    /// interjection is a gap in the data rather than a mistake by the caller, and the service passes it
    /// through.
    /// </para>
    /// </remarks>
    public sealed record InterjectionData
    {
        /// <summary>
        /// Gets what the interjection does in the discourse.
        /// </summary>
        public InterjectionType Type { get; init; }

        /// <summary>
        /// Gets a value indicating whether the interjection can stand as the predicate of its clause.
        /// </summary>
        /// <remarks>
        /// "Palicí buch ho po hlavě", "z koně hop", "Petr dup mi na nohu" — the interjection takes objects
        /// and adjuncts exactly as a verb would. This decides the punctuation, which is why it is worth
        /// recording: the ÚJČ rule is that an interjection is set off by a comma <em>unless</em> it stands in
        /// for a clause member, so "Kamarádi, hurá, vyhráli jsme" takes commas and "Palicí buch ho po hlavě"
        /// does not.
        /// <para>
        /// Mostly the onomatopoeic ones, but not only — hop is conative and is predicative all the same — so
        /// it is recorded per word rather than derived from <see cref="Type"/>.
        /// </para>
        /// </remarks>
        public bool CanBePredicate { get; init; }

        /// <summary>
        /// Gets whether the interjection is repeated when used.
        /// </summary>
        /// <remarks>
        /// Conservative on purpose. Czech interjections repeat freely and NESČ names only a few cases
        /// outright — oj oj oj, ťuk(y) ťuk, no no as optional, bubu as obligatory — so anything unmarked is
        /// simply unrecorded rather than known not to repeat.
        /// </remarks>
        public Reduplication Reduplication { get; init; }

        /// <summary>
        /// Gets the verb formed from the interjection, or null when none is registered.
        /// </summary>
        /// <remarks>
        /// žbluňk gives žbluňknout, ťuk gives ťuknout. NESČ points this out as a property of the
        /// onomatopoeic group specifically: they enter word formation directly, without passing through
        /// another word class first.
        /// </remarks>
        public string? DerivedVerb { get; init; }
    }
}
