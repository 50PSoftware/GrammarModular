namespace Grammar.Core.Enums
{
    /// <summary>
    /// Specifies the diathesis of a valency frame — how the participants of the predicate are mapped onto
    /// surface positions.
    /// </summary>
    /// <remarks>
    /// A diathesis rewrites the whole mapping at once, not one slot: the passive of dát moves PAT into the
    /// nominative, pushes ACT into the instrumental or drops it, and leaves ADDR where it was. Storing one
    /// frame per diathesis keeps each mapping stated outright, which is why the model never mixes two of
    /// them into a single frame.
    /// </remarks>
    public enum Diathesis
    {
        /// <summary>
        /// The unmarked mapping — ACT in the nominative, PAT in the accusative.
        /// </summary>
        Active = 1,

        /// <summary>
        /// Periphrastic passive — být plus the passive participle; ACT becomes instrumental or is dropped.
        /// </summary>
        PassivePeriphrastic,

        /// <summary>
        /// Reflexive passive — se plus an active form; general use is restricted to imperfectives.
        /// </summary>
        ReflexivePassive,

        /// <summary>
        /// Recipient deobjective — dostat plus the passive participle; ADDR becomes the nominative subject.
        /// </summary>
        RecipientDeobjective,

        /// <summary>
        /// Dispositional modality — se plus the verb plus a manner adverbial: knihy se mi čtou dobře.
        /// </summary>
        Dispositional,

        /// <summary>
        /// Resultative — mít plus the passive participle: mám napsáno.
        /// </summary>
        Resultative
    }
}
