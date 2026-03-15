using Grammar.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grammar.Czech.Models
{
    namespace Grammar.Czech.Models
    {
        /// <summary>
        /// Slot-based paradigma pro zájmenné vzory (ten, náš, váš, kdo, co).
        /// Klíče: Number → GenderSlot → Case → tvar.
        /// </summary>
        public sealed record PronounParadigm
        {
            public Dictionary<Number, Dictionary<GenderSlot, Dictionary<Case, string>>> Slots { get; init; } = new();
        }

        public enum GenderSlot
        {
            MasculineAnimate,
            MasculineInanimate,
            Feminine,
            Neuter,
            Other   // pro plurál kde fem/neutr/inan sdílejí tvary
        }
    }
}
