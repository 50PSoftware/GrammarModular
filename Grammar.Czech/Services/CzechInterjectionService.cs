using Grammar.Core.Enums;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;

namespace Grammar.Czech.Services
{
    /// <summary>
    /// Provides Czech interjection lookup: what an interjection does, whether it can carry a clause, and how
    /// it is punctuated.
    /// </summary>
    /// <remarks>
    /// No morphology. An interjection is uninflected and, in the NESČ description, forms the most primitive
    /// sentence type there is — an utterance with neither a verbal nor a nominal head — so the lemma is the
    /// form and there is nothing to build.
    /// <para>
    /// The inventory is open, and more openly than any other class here: onomatopoeia is coined freely, so an
    /// unregistered lemma passes through rather than being reported. That is the opposite of the conjunction
    /// service, where a closed class makes an unknown word a mistake worth naming.
    /// </para>
    /// </remarks>
    public class CzechInterjectionService : ICzechInterjectionService
    {
        private readonly Dictionary<string, InterjectionData> _interjections;
        private readonly Lazy<ILookup<InterjectionType, string>> _byType;

        /// <summary>
        /// Initializes a new instance of the <see cref="CzechInterjectionService"/> type.
        /// </summary>
        public CzechInterjectionService(IInterjectionDataProvider dataProvider)
        {
            _interjections = dataProvider.GetInterjections();

            _byType = new Lazy<ILookup<InterjectionType, string>>(
                () => _interjections.ToLookup(entry => entry.Value.Type, entry => entry.Key),
                LazyThreadSafetyMode.ExecutionAndPublication);
        }

        /// <summary>
        /// Determines whether the supplied lemma is a registered interjection.
        /// </summary>
        /// <param name="lemma">The dictionary form to look up.</param>
        /// <returns><see langword="true"/> when the lemma is in the inventory; otherwise, <see langword="false"/>.</returns>
        public bool IsInterjection(string lemma) => _interjections.ContainsKey(lemma);

        /// <summary>
        /// Gets what the supplied interjection does in the discourse.
        /// </summary>
        /// <param name="lemma">The dictionary form to look up.</param>
        /// <returns>The interjection type.</returns>
        public InterjectionType GetInterjectionType(string lemma) => Lookup(lemma).Type;

        /// <summary>
        /// Determines whether the interjection can stand as the predicate of its clause.
        /// </summary>
        /// <param name="lemma">The dictionary form to look up.</param>
        /// <returns><see langword="true"/> when it takes objects and adjuncts as a verb would; otherwise, <see langword="false"/>.</returns>
        public bool CanBePredicate(string lemma)
            => _interjections.TryGetValue(lemma, out var data) && data.CanBePredicate;

        /// <summary>
        /// Determines whether a comma separates the interjection from the rest of the sentence.
        /// </summary>
        /// <param name="lemma">The dictionary form to look up.</param>
        /// <param name="asPredicate">Whether the interjection is being used as the predicate.</param>
        /// <returns><see langword="true"/> unless the interjection stands in for a clause member.</returns>
        /// <remarks>
        /// Asking for predicative punctuation on a word not recorded as predicative is a contradiction the
        /// caller should hear about rather than have quietly resolved one way or the other.
        /// </remarks>
        public bool RequiresComma(string lemma, bool asPredicate)
        {
            if (!asPredicate)
            {
                return true;
            }

            if (!CanBePredicate(lemma))
            {
                throw new InvalidOperationException(
                    $"Citoslovce '{lemma}' není zapsané jako přísudkové, takže nemůže zastupovat větný člen. "
                    + "Buď ho tak doplň do interjections.json, nebo ho nepoužívej jako přísudek.");
            }

            return false;
        }

        /// <summary>
        /// Gets how the interjection repeats.
        /// </summary>
        /// <param name="lemma">The dictionary form to look up.</param>
        /// <returns>The reduplication recorded for it.</returns>
        public Reduplication GetReduplication(string lemma) => Lookup(lemma).Reduplication;

        /// <summary>
        /// Gets the verb formed from the interjection.
        /// </summary>
        /// <param name="lemma">The dictionary form to look up.</param>
        /// <returns>The derived verb, or <see langword="null"/> when none is registered.</returns>
        public string? GetDerivedVerb(string lemma) => Lookup(lemma).DerivedVerb;

        /// <summary>
        /// Gets every registered interjection of the supplied type.
        /// </summary>
        /// <param name="type">The interjection type to select.</param>
        /// <returns>The lemmas registered under that type.</returns>
        public IReadOnlyList<string> GetInterjectionsOfType(InterjectionType type) => _byType.Value[type].ToList();

        private InterjectionData Lookup(string lemma)
        {
            if (_interjections.TryGetValue(lemma, out var data))
            {
                return data;
            }

            throw new InvalidOperationException(
                $"Citoslovce '{lemma}' není v datech. Ověř to přes IsInterjection, "
                + "nebo ho doplň do interjections.json.");
        }
    }
}
