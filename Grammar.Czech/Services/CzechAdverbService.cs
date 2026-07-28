using Grammar.Core.Models.Word;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;

namespace Grammar.Czech.Services
{
    /// <summary>
    /// Produces Czech adverb forms: the lemma itself in the positive degree, and the registered
    /// comparative and superlative where the adverb is compared.
    /// </summary>
    /// <remarks>
    /// Adverbs are uninflected, so an unlisted lemma passes through unchanged in the positive degree —
    /// any adverb can be put in a sentence without being registered first. Comparison is another matter:
    /// it is irregular often enough that guessing would produce a plausible wrong word rather than a
    /// failure, so an unregistered comparative is reported instead of derived.
    /// </remarks>
    public class CzechAdverbService : ICzechAdverbService
    {
        private const string SuperlativePrefix = "nej";

        private readonly Dictionary<string, AdverbData> _adverbs;
        private readonly Lazy<ILookup<string, string>> _byAdjective;

        /// <summary>
        /// Initializes a new instance of the <see cref="CzechAdverbService"/> type.
        /// </summary>
        public CzechAdverbService(IAdverbDataProvider dataProvider)
        {
            _adverbs = dataProvider.GetAdverbs();

            // A lookup rather than a dictionary: one adjective can yield two adverbs.
            _byAdjective = new Lazy<ILookup<string, string>>(
                () => _adverbs
                    .Where(entry => entry.Value.DerivedFrom is not null)
                    .ToLookup(entry => entry.Value.DerivedFrom!, entry => entry.Key),
                LazyThreadSafetyMode.ExecutionAndPublication);
        }

        /// <summary>
        /// Builds the requested adverb form.
        /// </summary>
        /// <param name="request">The Czech word request to process.</param>
        /// <returns>The generated adverb form.</returns>
        public WordForm GetForm(CzechWordRequest request)
        {
            var degree = request.Degree ?? Models.Degree.Positive;

            if (degree == Models.Degree.Positive)
            {
                return new WordForm(request.Lemma);
            }

            var comparative = ResolveComparative(request);

            return new WordForm(degree == Models.Degree.Superlative
                ? SuperlativePrefix + comparative
                : comparative);
        }

        /// <summary>
        /// Determines whether the adverb is compared.
        /// </summary>
        /// <param name="lemma">The dictionary form to look up.</param>
        /// <returns><see langword="true"/> when a comparative is registered for the lemma; otherwise, <see langword="false"/>.</returns>
        public bool IsComparable(string lemma)
            => _adverbs.TryGetValue(lemma, out var data) && data.Comparative is not null;

        /// <summary>
        /// Gets every comparative usage accepts for the adverb, the generated one first.
        /// </summary>
        /// <param name="lemma">The dictionary form to look up.</param>
        /// <returns>The comparatives, or an empty sequence when the adverb is unregistered or not compared.</returns>
        public IReadOnlyList<string> GetComparativeVariants(string lemma)
        {
            if (!_adverbs.TryGetValue(lemma, out var data) || data.Comparative is null)
            {
                return [];
            }

            var variants = new List<string> { data.Comparative };

            if (data.ComparativeShort is not null)
            {
                variants.Add(data.ComparativeShort);
            }

            variants.AddRange(data.ComparativeAlternatives);

            return variants;
        }

        /// <summary>
        /// Gets the adverbs derived from the supplied adjective.
        /// </summary>
        /// <param name="adjectiveLemma">The adjective lemma.</param>
        /// <returns>The adverbs derived from it, or an empty sequence when none is registered.</returns>
        public IReadOnlyList<string> GetAdverbsFor(string adjectiveLemma)
            => _byAdjective.Value[adjectiveLemma].ToList();

        private string ResolveComparative(CzechWordRequest request)
        {
            if (!_adverbs.TryGetValue(request.Lemma, out var data))
            {
                throw new InvalidOperationException(
                    $"Příslovce '{request.Lemma}' není v adverbs.json, takže jeho komparativ není znám. "
                    + "V prvním stupni projde jakékoli příslovce; stupňovat lze jen zapsaná.");
            }

            if (data.Comparative is null)
            {
                throw new InvalidOperationException(
                    $"Příslovce '{request.Lemma}' se nestupňuje.");
            }

            // The short variant is the colloquial one of a doublet — hůř against hůře — so it is used only
            // when asked for, and only where usage has one.
            return request.PrefersShortForm == true && data.ComparativeShort is not null
                ? data.ComparativeShort
                : data.Comparative;
        }
    }
}
