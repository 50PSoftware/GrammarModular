namespace Grammar.Core.Enums
{
    /// <summary>
    /// Specifies Functional Generative Description (FGD) functors — language-independent
    /// semantic roles assigned to valency slots and to predicate arguments.
    /// Surface realization of a functor (case, preposition, clause type) is
    /// language-specific and is resolved by the respective language module.
    /// </summary>
    public enum FgdFunctor
    {
        /// <summary>
        /// Actor (konatel) — the agent or bearer of the action.
        /// </summary>
        ACT = 1,

        /// <summary>
        /// Patient (patiens) — the entity affected by the action.
        /// </summary>
        PAT,

        /// <summary>
        /// Addressee (adresát) — the recipient of a communicated or transferred content.
        /// </summary>
        ADDR,

        /// <summary>
        /// Origin (původ) — the source entity or the original state.
        /// </summary>
        ORIG,

        /// <summary>
        /// Effect (efekt) — the resulting state or product of the action.
        /// </summary>
        EFF,

        /// <summary>
        /// Directional-from (odkud) — the starting point of a movement.
        /// </summary>
        DIR1,

        /// <summary>
        /// Directional-through (kudy) — the path of a movement.
        /// </summary>
        DIR2,

        /// <summary>
        /// Directional-to (kam) — the target of a movement.
        /// </summary>
        DIR3,

        /// <summary>
        /// Locative (kde) — the place where the event holds.
        /// </summary>
        LOC,

        /// <summary>
        /// Manner (jak) — the way the event proceeds.
        /// </summary>
        MANN,

        /// <summary>
        /// Means (čím) — the instrument used.
        /// </summary>
        MEANS,

        /// <summary>
        /// Beneficiary (pro koho) — the party the event is meant to benefit.
        /// </summary>
        BEN,

        /// <summary>
        /// Cause (proč) — the reason the event occurs.
        /// </summary>
        CAUS,

        /// <summary>
        /// Aim (za jakým účelem) — the purpose the event serves.
        /// </summary>
        AIM,

        /// <summary>
        /// Temporal-when (kdy) — the time the event holds.
        /// </summary>
        TWHEN
    }
}