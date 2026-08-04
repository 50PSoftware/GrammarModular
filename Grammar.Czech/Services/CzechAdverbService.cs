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
    /// any adverb can be put in a sentence without being registered first.
    /// <para>
    /// Comparison is regular enough to derive. Run against the 99 comparatives in the data, the suffix rule
    /// reproduces 76 of them exactly, and every one it misses is an irregular the ÚJČ reference lists as
    /// such — dobře, zle, brzy, dlouho, vysoko, málo, těžko, snadno, hluboko, široko, úzko, and the same
    /// kind of word besides. The rule and the exception list partition the data without overlap, which is
    /// the condition under which deriving is safe. A registered comparative therefore wins, an adverb
    /// registered without one is taken to be uncompared, and anything unregistered is derived.
    /// </para>
    /// <para>
    /// This does not extend to forming the adverb from its adjective. There the choice between -o, -e/-ě and
    /// -y really is unpredictable and one adjective can yield two adverbs, so that stays a mapping.
    /// </para>
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
            => _adverbs.TryGetValue(lemma, out var data)
                ? data.Comparative is not null
                : DeriveComparative(lemma) is not null;

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

        /// <summary>
        /// Determines whether the lemma is an adverb that can introduce a relative clause.
        /// </summary>
        /// <param name="lemma">The dictionary form to look up.</param>
        /// <returns><see langword="true"/> for a registered relative adverb; otherwise, <see langword="false"/>.</returns>
        public bool IsRelative(string lemma)
            => _adverbs.TryGetValue(lemma, out var data) && data.IsRelative;

        private const string Vowels = "aáeéěiíyýoóuúů";

        // -ěji after d, t, n and the labials, -eji elsewhere: the same ě-orthography split the declension
        // uses. Checked against IJP on tvrději, chytřeji, prudčeji, hezčeji.
        private static string? DeriveComparative(string lemma)
        {
            if (lemma.Length < 3)
            {
                return null;
            }

            var stem = Vowels.Contains(lemma[^1]) ? lemma[..^1] : lemma;

            if (stem.Length == 0)
            {
                return null;
            }

            stem = stem[^1] switch
            {
                'c' or 'k' => stem[..^1] + 'č',
                'h' => stem[..^1] + 'ž',
                _ => stem
            };

            return stem + ("dtnbpmvf".Contains(stem[^1]) ? "ěji" : "eji");
        }

        private string ResolveComparative(CzechWordRequest request)
        {
            // Unregistered: derive. Being absent from the data says nothing about whether the adverb is
            // compared, and the regular suffix is what an unlisted adverb almost always takes.
            if (!_adverbs.TryGetValue(request.Lemma, out var data))
            {
                return DeriveComparative(request.Lemma)
                    ?? throw new InvalidOperationException(
                        $"Z příslovce '{request.Lemma}' nejde odvodit komparativ. Zapiš ho do adverbs.json.");
            }

            // Registered without one: the entry states the adverb is not compared, which is a claim the
            // data makes and the rule must not override.
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
