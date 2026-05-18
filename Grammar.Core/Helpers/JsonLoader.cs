using System.Reflection;
using System.Text.Json;

namespace Grammar.Core.Helpers
{
    /// <summary>
    /// Loads embedded JSON resources into strongly typed models.
    /// </summary>
    public static class JsonLoader
    {
        /// <summary>
        /// Loads a JSON list from an embedded resource.
        /// </summary>
        /// <param name="assembly">The assembly that contains the embedded JSON resource.</param>
        /// <param name="path">The embedded resource name or path segment to load.</param>
        /// <param name="options">The JSON serializer options used to deserialize the resource.</param>
        /// <returns>The deserialized list, or an empty list when the resource cannot be loaded.</returns>
        public static List<T> LoadListFromFile<T>(Assembly assembly, string path, JsonSerializerOptions options)
        {
            var fullName = $"{assembly.GetName().Name}.{path}.json";
            using var json = assembly.GetManifestResourceStream(fullName) ?? throw new FileNotFoundException($"Embedded resource '{fullName}' not found!");
            return JsonSerializer.Deserialize<List<T>>(json, options)!;
        }

        /// <summary>
        /// Loads a JSON dictionary from an embedded resource.
        /// </summary>
        /// <param name="assembly">The assembly that contains the embedded JSON resource.</param>
        /// <param name="path">The embedded resource name or path segment to load.</param>
        /// <param name="options">The JSON serializer options used to deserialize the resource.</param>
        /// <returns>The deserialized dictionary, or an empty dictionary when the resource cannot be loaded.</returns>
        public static Dictionary<string, T> LoadDictionaryFromFile<T>(Assembly assembly, string path, JsonSerializerOptions options)
        {
            var fullName = $"{assembly.GetName().Name}.{path}.json";
            using var json = assembly.GetManifestResourceStream(fullName) ?? throw new FileNotFoundException($"Embedded resource '{fullName}' not found!");
            return JsonSerializer.Deserialize<Dictionary<string, T>>(json, options)!;
        }

        /// <summary>
        /// Loads a JSON object from an ambedded resource.
        /// </summary>
        /// <typeparam name="T">DataObject.</typeparam>
        /// <param name="assembly">The assembly that contains the embedded JSON resource.</param>
        /// <param name="path">The embedded resource name or path segment to load.</param>
        /// <param name="options">The JSON serializer options used to deserialize the resource.</param>
        /// <returns>The deserialized data object.</returns>
        public static T Load<T>(Assembly assembly, string path, JsonSerializerOptions options)
        {
            var fullName = $"{assembly.GetName().Name}.{path}.json";
            using var json = assembly.GetManifestResourceStream(fullName) ?? throw new FileNotFoundException($"Embedded resource '{fullName}' not found!");
            return JsonSerializer.Deserialize<T>(json, options)!;
        }
    }
}
