using System.Reflection;
using System.Text.Json;

namespace Grammar.Core.Helpers
{
    public static class JsonLoader
    {
        public static List<T> LoadListFromFile<T>(Assembly assembly, string path, JsonSerializerOptions options)
        {
            var fullName = $"{assembly.GetName().Name}.{path}.json";
            using var json = assembly.GetManifestResourceStream(fullName) ?? throw new FileNotFoundException($"Embedded resource '{fullName}' not found!");
            return JsonSerializer.Deserialize<List<T>>(json, options)!;
        }

        public static Dictionary<string, T> LoadDictionaryFromFile<T>(Assembly assembly, string path, JsonSerializerOptions options)
        {
            var fullName = $"{assembly.GetName().Name}.{path}.json";
            using var json = assembly.GetManifestResourceStream(fullName) ?? throw new FileNotFoundException($"Embedded resource '{fullName}' not found!");
            return JsonSerializer.Deserialize<Dictionary<string, T>>(json, options)!;
        }
    }
}
