using Grammar.Core.Helpers;
using Grammar.Core.Interfaces;
using Grammar.Core.Models.Valency;
using Grammar.Czech.Helpers;
using Grammar.Czech.Models;
using System.Reflection;

namespace Grammar.Czech.Providers.JsonProviders
{
    /// <summary>
    /// Loads Czech lexical entries and valency frames from embedded JSON resources.
    /// </summary>
    public sealed class JsonValencyProvider : IValencyProvider<CzechLexicalEntry>
    {
        // The files live under Data/Lexicon, not Data/Valency — the old paths named a folder that
        // never existed, so every load threw before it could return anything.
        private const string LexiconPath = "Data.Lexicon.lexicon";
        private const string ValencyPath = "Data.Lexicon.valency";

        private readonly Lazy<Dictionary<string, CzechLexicalEntry>> _lexicon;
        private readonly Lazy<Dictionary<string, List<ValencyFrame>>> _frames;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonValencyProvider"/> type.
        /// </summary>
        public JsonValencyProvider()
        {
            var assembly = Assembly.GetExecutingAssembly();

            _lexicon = new Lazy<Dictionary<string, CzechLexicalEntry>>(
                () => LoadLexicon(assembly),
                LazyThreadSafetyMode.ExecutionAndPublication);

            _frames = new Lazy<Dictionary<string, List<ValencyFrame>>>(
                () => LoadFrames(assembly),
                LazyThreadSafetyMode.ExecutionAndPublication);
        }

        /// <summary>
        /// Gets the lexical entry registered for the supplied lemma.
        /// </summary>
        /// <param name="lemma">The dictionary form to resolve or analyze.</param>
        /// <returns>The lexical entry for the lemma, or null when the lemma is not present.</returns>
        public CzechLexicalEntry? GetEntry(string lemma)
            => _lexicon.Value.TryGetValue(lemma.ToLowerInvariant(), out var entry)
                ? entry
                : null;

        /// <summary>
        /// Gets valency frames registered for the supplied verb lemma.
        /// </summary>
        /// <param name="verbLemma">The verb lemma whose valency frames are requested.</param>
        /// <returns>The valency frames for the lemma, or an empty sequence when no frames are registered.</returns>
        public IEnumerable<ValencyFrame> GetFrames(string verbLemma)
            => _frames.Value.TryGetValue(verbLemma.ToLowerInvariant(), out var frames)
                ? frames
                : [];

        /// <summary>
        /// Determines whether the lexicon contains an entry for the supplied lemma.
        /// </summary>
        /// <param name="lemma">The dictionary form to resolve or analyze.</param>
        /// <returns><see langword="true"/> when the lemma is present in the lexicon; otherwise, <see langword="false"/>.</returns>
        public bool HasEntry(string lemma)
            => _lexicon.Value.ContainsKey(lemma.ToLowerInvariant());

        private static Dictionary<string, CzechLexicalEntry> LoadLexicon(Assembly assembly)
            => JsonLoader.LoadDictionaryFromFile<CzechLexicalEntry>(
                assembly, LexiconPath, JsonHelpers.SerializerOptions)
               ?? [];

        private static Dictionary<string, List<ValencyFrame>> LoadFrames(Assembly assembly)
            => JsonLoader.LoadDictionaryFromFile<List<ValencyFrame>>(
                assembly, ValencyPath, JsonHelpers.SerializerOptions)
               ?? [];
    }
}
