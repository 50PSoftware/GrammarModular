using Grammar.Core.Enums;
using Grammar.Core.Models.Valency;
using System.Text.Json;

namespace Grammar.Czech.Providers.SqliteProviders
{
    /// <summary>
    /// Reads the slots of a light verb construction out of the JSON the dictionary stores them in.
    /// </summary>
    /// <remarks>
    /// The shape mirrors <c>valency_slot</c> and <c>slot_realization</c> on purpose, so that a
    /// construction reads as the frame it becomes and nobody has to learn a second vocabulary for the
    /// same thing:
    /// <code>
    /// {"slots":[
    ///   {"functor":"ACT","order":1,"obligatoriness":"Obligatory","forms":[{"case":"Nominative"}]},
    ///   {"functor":"CPHR","order":2,"obligatoriness":"Obligatory","forms":[{"case":"Accusative"}]},
    ///   {"functor":"PAT","order":3,"obligatoriness":"Optional","forms":[{"case":"Accusative","preposition":"o"}]}
    /// ]}
    /// </code>
    /// A malformed template is an error rather than an empty frame: a construction that licenses
    /// nothing would silently behave as though the verb were used on its own, which is the reading the
    /// row exists to rule out.
    /// </remarks>
    public static class ConstructionSlots
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
        };

        /// <summary>
        /// Parses the slots of a construction.
        /// </summary>
        /// <param name="json">The stored template.</param>
        /// <param name="patternName">The name of the pattern, for the message when it will not read.</param>
        /// <returns>The slots.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the template cannot be read.</exception>
        public static IReadOnlyList<ValencySlot> Parse(string json, string patternName)
        {
            Template? template;

            try
            {
                template = JsonSerializer.Deserialize<Template>(json, Options);
            }
            catch (JsonException exception)
            {
                throw new InvalidOperationException(
                    $"Konstrukce '{patternName}' má nečitelný template_json: {exception.Message}", exception);
            }

            if (template?.Slots is not { Count: > 0 } slots)
            {
                throw new InvalidOperationException(
                    $"Konstrukce '{patternName}' nemá v template_json žádný slot, takže neříká, co váže.");
            }

            return [.. slots.Select((slot, index) => Read(slot, index, patternName))];
        }

        private static ValencySlot Read(SlotTemplate slot, int index, string patternName) => new()
        {
            Functor = Enum.TryParse<FgdFunctor>(slot.Functor, ignoreCase: true, out var functor)
                && Enum.IsDefined(functor)
                    ? functor
                    : throw new InvalidOperationException(
                        $"Konstrukce '{patternName}' uvádí funktor '{slot.Functor}', který neexistuje."),

            // Pořadí je nepovinné: pořadí v poli je rozumné výchozí a psát ho dvakrát zve k rozejití.
            CanonicalOrder = slot.Order ?? index + 1,
            Obligatoriness = Enum.TryParse<Obligatoriness>(slot.Obligatoriness, ignoreCase: true, out var obligatoriness)
                ? obligatoriness
                : Obligatoriness.Optional,
            Realizations = [.. (slot.Forms ?? []).Select((form, position) => Read(form, position, patternName))],
        };

        private static SlotRealization Read(FormTemplate form, int index, string patternName) => new()
        {
            Case = form.Case is null
                ? null
                : Enum.TryParse<Case>(form.Case, ignoreCase: true, out var kase) && Enum.IsDefined(kase)
                    ? kase
                    : throw new InvalidOperationException(
                        $"Konstrukce '{patternName}' uvádí pád '{form.Case}', který neexistuje."),
            Preposition = form.Preposition,
            ClauseType = form.Clause,
            TakesInfinitive = form.Infinitive ?? false,
            Preference = form.Preference ?? index + 1,
        };

        private sealed record Template(List<SlotTemplate>? Slots);

        private sealed record SlotTemplate(
            string Functor,
            int? Order,
            string? Obligatoriness,
            List<FormTemplate>? Forms);

        private sealed record FormTemplate(
            string? Case,
            string? Preposition,
            string? Clause,
            bool? Infinitive,
            int? Preference);
    }
}
