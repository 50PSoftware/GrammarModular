using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;
using System.Text.Json;

namespace Grammar.Czech.Providers.JsonProviders
{
    public class JsonParticlesDataProvider : IParticleDataProvider
    {
        private readonly string _particlePath;
        private ParticlesData _data;

        public JsonParticlesDataProvider()
        {
            this._particlePath = "Data.particles";
        }

        public ParticlesData GetParticles()
        {
            if (_data == null)
            {
                var json = File.ReadAllText(_particlePath);
                _data = JsonSerializer.Deserialize<ParticlesData>(json, Helpers.JsonHelpers.SerializerOptions)!;
            }

            return _data;
        }
    }
}
