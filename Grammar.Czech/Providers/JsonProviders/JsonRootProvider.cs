using Grammar.Core.Helpers;
using Grammar.Core.Interfaces;
using Grammar.Core.Models.Derivation;
using Grammar.Czech.Helpers;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models.Derivation;
using System.Reflection;

namespace Grammar.Czech.Providers.JsonProviders
{
    /// <summary>
    /// Loads Czech root entries from the embedded <c>Data/Roots/roots.json</c> resource
    /// and implements <see cref="ICzechRootProvider"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The JSON format is a two-level flat map: root key → (lemma → metadata).
    /// At load time a reverse index (lemma → root entry) is built for O(1)
    /// <see cref="GetCzechByLemma"/> lookups.
    /// </para>
    /// <para>
    /// Both the forward and reverse dictionaries are loaded exactly once via a
    /// thread-safe <see cref="Lazy{T}"/>.
    /// </para>
    /// </remarks>
    public sealed class JsonRootProvider : ICzechRootProvider
    {
        private readonly Lazy<Dictionary<string, CzechRootEntry>> _byRoot;
        private readonly Lazy<Dictionary<string, CzechRootEntry>> _byLemma;

        /// <summary>
        /// Initializes a new instance of <see cref="JsonRootProvider"/> and sets up
        /// lazy loading from the embedded JSON resource.
        /// </summary>
        public JsonRootProvider()
        {
            var assembly = Assembly.GetExecutingAssembly();

            _byRoot = new Lazy<Dictionary<string, CzechRootEntry>>(
                () => LoadRoots(assembly),
                LazyThreadSafetyMode.ExecutionAndPublication);

            _byLemma = new Lazy<Dictionary<string, CzechRootEntry>>(
                () => BuildReverseIndex(_byRoot.Value),
                LazyThreadSafetyMode.ExecutionAndPublication);
        }

        #region ICzechRootProvider

        /// <inheritdoc/>
        public CzechRootEntry? GetCzechByRoot(string root)
            => _byRoot.Value.TryGetValue(root.ToLowerInvariant(), out var entry) ? entry : null;

        /// <inheritdoc/>
        public CzechRootEntry? GetCzechByLemma(string lemma)
            => _byLemma.Value.TryGetValue(lemma.ToLowerInvariant(), out var entry) ? entry : null;

        #endregion ICzechRootProvider

        #region IRootProvider

        /// <inheritdoc/>
        public RootEntry? GetByRoot(string root)
            => MapToCore(GetCzechByRoot(root));

        /// <inheritdoc/>
        public RootEntry? GetByLemma(string lemma)
            => MapToCore(GetCzechByLemma(lemma));

        #endregion IRootProvider

        #region Private helpers

        private static Dictionary<string, CzechRootEntry> LoadRoots(Assembly assembly)
        {
            // roots.json lives at: Grammar.Czech/Data/Roots/roots.json
            // Embedded resource path: Grammar.Czech.Data.Roots.roots
            var raw = JsonLoader.LoadDictionaryFromFile<Dictionary<string, CzechDerivationLink>>(
                assembly, "Data.Lexicon.roots", JsonHelpers.SerializerOptions)
                ?? [];

            return raw.ToDictionary(
                kvp => kvp.Key.ToLowerInvariant(),
                kvp => new CzechRootEntry
                {
                    Root        = kvp.Key,
                    Derivations = kvp.Value
                });
        }

        private static Dictionary<string, CzechRootEntry> BuildReverseIndex(
            Dictionary<string, CzechRootEntry> byRoot)
        {
            var index = new Dictionary<string, CzechRootEntry>(StringComparer.OrdinalIgnoreCase);

            foreach (var rootEntry in byRoot.Values)
            {
                foreach (var lemma in rootEntry.Derivations.Keys)
                {
                    index[lemma] = rootEntry;
                }
            }

            return index;
        }

        /// <summary>
        /// Maps a <see cref="CzechRootEntry"/> to the language-agnostic <see cref="RootEntry"/>.
        /// Only fields present on <see cref="DerivationLink"/> are populated; Czech-specific
        /// metadata stays in the typed layer.
        /// </summary>
        private static RootEntry? MapToCore(CzechRootEntry? czech)
        {
            if (czech is null)
            {
                return null;
            }

            // CzechDerivationLink does NOT inherit from DerivationLink — manual projection required.
            // Lemma is the dictionary key in roots.json, not a field on CzechDerivationLink.
            var derivations = czech.Derivations
                .Select(kvp => new DerivationLink
                {
                    Lemma        = kvp.Key,
                    PartOfSpeech = kvp.Value.PartOfSpeech
                })
                .ToList();

            return new RootEntry
            {
                Root        = czech.Root,
                Derivations = derivations
            };
        }

        #endregion Private helpers
    }
}
