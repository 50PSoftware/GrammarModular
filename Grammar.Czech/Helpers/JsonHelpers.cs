using Grammar.Czech.Models.JsonConverters;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Grammar.Czech.Helpers
{
    /// <summary>
    /// Provides JSON serializer settings shared by Czech data providers.
    /// </summary>
    public static class JsonHelpers
    {
        /// <summary>
        /// Gets the serializer options used for embedded Czech grammar data.
        /// </summary>
        internal static JsonSerializerOptions SerializerOptions
        {
            get
            {
                return new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Converters = {
                        new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
                        new VerbTenseFormsConverter(),
                        new PronounParadigmConverter(),
                        new NumeralParadigmConverter()
                    },
                };
            }
        }
    }
}
