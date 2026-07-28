using Grammar.Core.Helpers;
using Grammar.Czech.Helpers;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;
using System.Reflection;
using System.Text.Json;

namespace Grammar.Czech.Providers.JsonProviders
{
    /// <summary>
    /// Loads clitic data from embedded JSON resources.
    /// </summary>
    public class JsonCliticsDataProvider : ICliticDataProvider
    {
        private readonly string _cliticPath = "Data.Rules.clitics";
        private readonly Lazy<CliticsData> _data;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonCliticsDataProvider"/> type.
        /// </summary>
        public JsonCliticsDataProvider()
        {
            var assembly = Assembly.GetExecutingAssembly();
            _data = new Lazy<CliticsData>(() => JsonLoader.Load<CliticsData>(assembly, _cliticPath, JsonHelpers.SerializerOptions));
        }

        /// <summary>
        /// Gets Czech clitic data loaded from embedded JSON data.
        /// </summary>
        /// <returns>The loaded Czech clitic definitions.</returns>
        public CliticsData GetClitics() => _data.Value;
    }
}
