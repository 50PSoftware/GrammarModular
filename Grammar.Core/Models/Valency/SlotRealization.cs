using Grammar.Core.Enums;

namespace Grammar.Core.Models.Valency
{
    /// <summary>
    /// Represents one surface form a valency slot may take.
    /// </summary>
    /// <remarks>
    /// A slot generally has several of these, which is why the type is named for the realization rather
    /// than for the slot: the patient of bát se is a bare genitive in bát se tmy and a prepositional
    /// accusative in bát se o dceru, and the patient of říct is an accusative in řekl to and a clause in
    /// řekl, že přijde. It was SyntacticRealization, held one per slot, and could state only the first
    /// member of each of those pairs.
    /// </remarks>
    public sealed record SlotRealization
    {
        /// <summary>
        /// Gets the grammatical case the slot is realized in, or <see langword="null"/> when the slot is
        /// realized as a clause or an infinitive and therefore carries no case.
        /// </summary>
        public Case? Case { get; init; }

        /// <summary>
        /// Gets the preposition governing the case, or <see langword="null"/> for a bare case.
        /// </summary>
        public string? Preposition { get; init; }

        /// <summary>
        /// Gets the subordinator introducing the dependent clause that fills the slot — že, aby, zda,
        /// jestli — or <see langword="null"/> when the slot is not realized as a clause.
        /// </summary>
        public string? ClauseType { get; init; }

        /// <summary>
        /// Gets a value indicating whether the slot is realized as an infinitive.
        /// </summary>
        public bool TakesInfinitive { get; init; }

        /// <summary>
        /// Gets the generation preference, where 1 is the form to produce and higher values are accepted
        /// but not produced.
        /// </summary>
        /// <remarks>
        /// Generation and analysis pull in opposite directions here. Analysis should accept every form the
        /// slot allows, whereas generation has to settle on one, and there is no way to derive which one
        /// from case and preposition alone. The rank says it outright.
        /// </remarks>
        public int Preference { get; init; } = 1;
    }
}
