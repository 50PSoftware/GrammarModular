using Grammar.Core.Helpers;
using Grammar.Czech.Helpers;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;
using System.Reflection;
using System.Text.Json;

namespace Grammar.Czech.Providers.JsonProviders
{
    /// <summary>
    /// Loads particles data provider from embedded JSON resources.
    /// </summary>
    public class JsonParticlesDataProvider : IParticleDataProvider
    {
        private readonly string _particlePath = "Data.Rules.particles";
        private readonly Lazy<ParticlesData> _data;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonParticlesDataProvider"/> type.
        /// </summary>
        public JsonParticlesDataProvider()
        {
            var assembly = Assembly.GetExecutingAssembly();
            _data = new Lazy<ParticlesData>(() => JsonLoader.Load<ParticlesData>(assembly, _particlePath, JsonHelpers.SerializerOptions));
        }

        /// <summary>
        /// Gets Czech particle data loaded from embedded JSON data.
        /// </summary>
        /// <returns>The loaded Czech particle definitions.</returns>
        public ParticlesData GetParticles() => _data.Value;
    }
}
