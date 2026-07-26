using Grammar.Core.Enums;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Grammar.Czech.Models.JsonConverters
{
    /// <summary>
    /// Converts JSON values for numeral paradigms, accepting three nesting depths.
    /// </summary>
    /// <remarks>
    /// Numerals vary wildly in how much they distinguish. Jeden needs the full number × gender × case table;
    /// dva distinguishes gender but not number; pět and několik have a single flat set of case forms.
    /// Writing all three as the deepest shape would bury the data in filler keys, so the converter reads
    /// whichever depth the entry uses and normalizes it — a missing level becomes
    /// <see cref="NumberSlot.Any"/> or <see cref="GenderSlot.Other"/>, which is exactly what the lookup
    /// falls back to.
    /// <para>The depth is told apart by the first property name: a number, a gender slot, or a case.</para>
    /// </remarks>
    public class NumeralParadigmConverter : JsonConverter<NumeralParadigm>
    {
        /// <summary>
        /// Reads a numeral paradigm from its nested JSON object representation.
        /// </summary>
        /// <param name="reader">The JSON reader positioned at the paradigm object.</param>
        /// <param name="typeToConvert">The target type requested by the serializer.</param>
        /// <param name="options">The JSON serializer options used to deserialize the resource.</param>
        /// <returns>The deserialized numeral paradigm.</returns>
        public override NumeralParadigm Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var document = JsonDocument.ParseValue(ref reader);
            var root = document.RootElement;

            var slots = new Dictionary<NumberSlot, Dictionary<GenderSlot, Dictionary<Case, string>>>();
            var first = root.EnumerateObject().FirstOrDefault();

            if (first.Value.ValueKind == JsonValueKind.Undefined)
            {
                return new NumeralParadigm { Slots = slots };
            }

            // Deepest shape: number → gender slot → case.
            if (Enum.TryParse<NumberSlot>(first.Name, out _))
            {
                foreach (var number in root.EnumerateObject())
                {
                    slots[NestedCaseTableReader.ParseKey<NumberSlot>(number.Name, "číslo")] =
                        NestedCaseTableReader.ReadGenderSlots(number.Value);
                }

                return new NumeralParadigm { Slots = slots };
            }

            // Gender slot → case. The numeral does not distinguish number, so every number shares the table.
            if (Enum.TryParse<GenderSlot>(first.Name, out _))
            {
                slots[NumberSlot.Any] = NestedCaseTableReader.ReadGenderSlots(root);

                return new NumeralParadigm { Slots = slots };
            }

            // Flat case → form. The numeral distinguishes neither number nor gender.
            slots[NumberSlot.Any] = new Dictionary<GenderSlot, Dictionary<Case, string>>
            {
                [GenderSlot.Other] = NestedCaseTableReader.ReadCases(root)
            };

            return new NumeralParadigm { Slots = slots };
        }

        /// <summary>
        /// Writes a numeral paradigm to JSON.
        /// </summary>
        /// <param name="writer">The JSON writer used by the serializer.</param>
        /// <param name="value">The numeral paradigm to write.</param>
        /// <param name="options">The JSON serializer options used to serialize the resource.</param>
        public override void Write(Utf8JsonWriter writer, NumeralParadigm value, JsonSerializerOptions options)
            => throw new NotImplementedException();
    }
}
