namespace Grammar.Core.Enums
{
    /// <summary>
    /// Specifies what kind of predicate a valency frame describes.
    /// </summary>
    /// <remarks>
    /// The kind decides what the generator has to build around the frame, not merely how the frame reads.
    /// A copular frame needs a predicative complement that agrees with the subject, a modal frame needs an
    /// infinitive whose subject is controlled from above, and a light-verb frame carries its meaning in the
    /// noun rather than the verb. Those are different jobs, so they are different values.
    /// </remarks>
    public enum ValencyKind
    {
        /// <summary>
        /// An ordinary full verb carrying its own meaning.
        /// </summary>
        Verbal = 1,

        /// <summary>
        /// A copula with a nominal predicative complement — je učitel.
        /// </summary>
        Copular_NominalPred,

        /// <summary>
        /// A copula with an adjectival predicative complement — je mladý.
        /// </summary>
        Copular_AdjectivalPred,

        /// <summary>
        /// An existential predication — je tam problém.
        /// </summary>
        Existential,

        /// <summary>
        /// A modal verb taking an infinitive — musí přijít.
        /// </summary>
        Modal,

        /// <summary>
        /// A phasal verb taking an infinitive or a deverbal noun — začal psát, začal s prací.
        /// </summary>
        PhasalLightVerb,

        /// <summary>
        /// A light verb whose meaning sits in the predicative noun — mít zájem, dělat starosti.
        /// </summary>
        LightVerb
    }
}
