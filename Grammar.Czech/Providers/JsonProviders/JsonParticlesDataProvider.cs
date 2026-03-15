using Grammar.Czech.Helpers;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;
using System.Text.Json;

namespace Grammar.Czech.Providers.JsonProviders
{
    public class JsonParticlesDataProvider : IParticleDataProvider
    {
        private readonly string _particlePath = "Data.particles";
        private readonly Lazy<ParticlesData> _data;

        public JsonParticlesDataProvider()
        {
            _data = new Lazy<ParticlesData>(() => JsonSerializer.Deserialize<ParticlesData>(File.ReadAllText(_particlePath), JsonHelpers.SerializerOptions)!);
        }

        public ParticlesData GetParticles() => _data.Value;
    }
}
