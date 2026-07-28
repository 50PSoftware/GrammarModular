using Grammar.Core.Helpers;
using Grammar.Czech.Helpers;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;
using System.Reflection;

namespace Grammar.Czech.Providers.JsonProviders
{
    /// <summary>
    /// Loads particle data from embedded JSON resources.
    /// </summary>
    public sealed class JsonParticleDataProvider : IParticleDataProvider
    {
        private const string ParticlesPath = "Data.Rules.particles";

        private readonly Lazy<Dictionary<string, ParticleData>> _data;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonParticleDataProvider"/> type.
        /// </summary>
        public JsonParticleDataProvider()
        {
            var assembly = Assembly.GetExecutingAssembly();

            _data = new Lazy<Dictionary<string, ParticleData>>(
                () => JsonLoader.LoadDictionaryFromFile<ParticleData>(assembly, ParticlesPath, JsonHelpers.SerializerOptions),
                LazyThreadSafetyMode.ExecutionAndPublication);
        }

        /// <summary>
        /// Gets the Czech particles loaded from embedded JSON data, keyed by lemma.
        /// </summary>
        /// <returns>The loaded particle definitions.</returns>
        public Dictionary<string, ParticleData> GetParticles() => _data.Value;
    }
}
