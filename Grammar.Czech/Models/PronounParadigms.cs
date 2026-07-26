using Grammar.Core.Enums;

namespace Grammar.Czech.Models
{
    namespace Grammar.Czech.Models
    {
        /// <summary>
        /// Represents pronoun paradigm.
        /// </summary>
        public sealed record PronounParadigm
        {
            /// <summary>
            /// Gets the forms grouped by number, gender slot, and case.
            /// </summary>
            public Dictionary<Number, Dictionary<GenderSlot, Dictionary<Case, string>>> Slots { get; init; } = new();
        }
    }
}
