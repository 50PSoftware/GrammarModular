using Grammar.Czech.Models.JsonConverters;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Grammar.Czech.Helpers
{
    public static class JsonHelpers
    {
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
                        new PronounParadigmConverter()
                    },
                };
            }
        }
    }
}
