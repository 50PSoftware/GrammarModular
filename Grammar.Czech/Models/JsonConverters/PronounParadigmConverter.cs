using Grammar.Core.Enums;
using Grammar.Czech.Models.Grammar.Czech.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Grammar.Czech.Models.JsonConverters
{
    public class PronounParadigmConverter : JsonConverter<PronounParadigm>
    {
        public override PronounParadigm Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            var slots = new Dictionary<Number, Dictionary<GenderSlot, Dictionary<Case, string>>>();

            foreach (var numberProp in root.EnumerateObject())
            {
                var number = Enum.Parse<Number>(numberProp.Name);
                var genderSlots = new Dictionary<GenderSlot, Dictionary<Case, string>>();

                foreach (var genderProp in numberProp.Value.EnumerateObject())
                {
                    var genderSlot = Enum.Parse<GenderSlot>(genderProp.Name);
                    var caseForms = new Dictionary<Case, string>();

                    foreach (var caseProp in genderProp.Value.EnumerateObject())
                    {
                        var grammaticalCase = Enum.Parse<Case>(caseProp.Name);
                        caseForms[grammaticalCase] = caseProp.Value.GetString()!;
                    }

                    genderSlots[genderSlot] = caseForms;
                }

                slots[number] = genderSlots;
            }

            return new PronounParadigm { Slots = slots };
        }

        public override void Write(Utf8JsonWriter writer, PronounParadigm value, JsonSerializerOptions options)
            => throw new NotImplementedException();
    }
}
