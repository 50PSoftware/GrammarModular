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
        EFF
    }
}