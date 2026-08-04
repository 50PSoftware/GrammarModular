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
        TWHEN,

        // Appended rather than sorted into their groups: the values are what the lexicon stores, so
        // renumbering would silently repoint every row already written.

        /// <summary>
        /// Difference (o kolik) — the extent by which something changes: zdražilo o pět korun.
        /// </summary>
        DIFF,

        /// <summary>
        /// Obstacle (o co) — what the event stumbles against: zakopl o kámen.
        /// </summary>
        OBST,

        /// <summary>
        /// Intent (za jakým záměrem) — the intended follow-up of a movement: šel nakoupit.
        /// </summary>
        INTT,

        /// <summary>
        /// Material (z čeho, čeho) — the substance or the set something is taken from: skupina studentů.
        /// </summary>
        MAT,

        /// <summary>
        /// Temporal-how-long (jak dlouho) — the duration of the event.
        /// </summary>
        THL,

        /// <summary>
        /// Extent (do jaké míry) — the degree to which the event holds.
        /// </summary>
        EXT,

        /// <summary>
        /// Criterion (podle čeho) — the standard the claim is measured against.
        /// </summary>
        CRIT,

        /// <summary>
        /// Accompaniment (s kým) — the participant taking part alongside the actor.
        /// </summary>
        ACMP,

        /// <summary>
        /// Complement (jako co) — the state a participant holds during the event: pracuje jako učitel.
        /// </summary>
        COMPL
    }
}