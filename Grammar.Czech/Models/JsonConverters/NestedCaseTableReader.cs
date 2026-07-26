using Grammar.Core.Enums;
using System.Text.Json;

namespace Grammar.Czech.Models.JsonConverters
{
    /// <summary>
    /// Reads the nested case tables shared by pronoun and numeral paradigm data.
    /// </summary>
    /// <remarks>
    /// Paradigm keys are parsed as enum names rather than mapped through the camel-case naming policy, so
    /// they stay PascalCase in the JSON while ordinary properties elsewhere are camelCase.
    /// </remarks>
    internal static class NestedCaseTableReader
    {
        /// <summary>
        /// Reads a case-to-form table.
        /// </summary>
        /// <param name="element">The JSON object whose properties are case names.</param>
        /// <returns>The forms keyed by case.</returns>
        internal static Dictionary<Case, string> ReadCases(JsonElement element)
        {
            var forms = new Dictionary<Case, string>();

            foreach (var property in element.EnumerateObject())
            {
                forms[ParseKey<Case>(property.Name, "pád")] = property.Value.GetString()!;
            }

            return forms;
        }

        /// <summary>
        /// Reads a gender-slot-to-case table.
        /// </summary>
        /// <param name="element">The JSON object whose properties are gender slot names.</param>
        /// <returns>The case tables keyed by gender slot.</returns>
        internal static Dictionary<GenderSlot, Dictionary<Case, string>> ReadGenderSlots(JsonElement element)
        {
            var slots = new Dictionary<GenderSlot, Dictionary<Case, string>>();

            foreach (var property in element.EnumerateObject())
            {
                slots[ParseKey<GenderSlot>(property.Name, "rodový slot")] = ReadCases(property.Value);
            }

            return slots;
        }

        /// <summary>
        /// Parses a paradigm key, naming the offending key when it does not match the enum.
        /// </summary>
        /// <typeparam name="T">The enum the key is expected to name.</typeparam>
        /// <param name="key">The raw JSON property name.</param>
        /// <param name="description">The Czech description of what the key was expected to be.</param>
        /// <returns>The parsed enum value.</returns>
        internal static T ParseKey<T>(string key, string description) where T : struct, Enum =>
            Enum.TryParse<T>(key, out var parsed)
                ? parsed
                : throw new JsonException($"'{key}' není platný {description} ({typeof(T).Name}).");
    }
}
