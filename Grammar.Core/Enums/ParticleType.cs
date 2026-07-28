namespace Grammar.Core.Enums
{
    /// <summary>
    /// Represents the function a Czech particle performs in the utterance.
    /// </summary>
    /// <remarks>
    /// NESČ records several competing classifications rather than one settled inventory — the functional
    /// scheme of MČ 2, Čermák's attitudinal one, and the functional-semantic one of Nekula in PMČ. This
    /// follows the last, which is the one Czech reference works cite most and the only one whose categories
    /// are defined by what the particle does rather than by how the speaker feels.
    /// <para>
    /// What all of them agree on is the defining property: a particle is uninflected and has no clause-member
    /// status — <em>bez větněčlenské platnosti</em>. That is what separates it from an adverb, which is a
    /// clause member and answers a supplementary question, and the two are frequently the same word.
    /// </para>
    /// </remarks>
    public enum ParticleType
    {
        /// <summary>
        /// Modální: how sure the speaker is that it holds — asi, snad, prý, možná, nejspíš, sotva.
        /// </summary>
        Modal,

        /// <summary>
        /// Intenzifikační: how far a property departs from the expected degree — velmi, příliš, zcela,
        /// poněkud, celkem.
        /// </summary>
        Intensifying,

        /// <summary>
        /// Vytýkací: singles one element out against the alternatives — jen, pouze, také, dokonce, teprve,
        /// zejména, právě.
        /// </summary>
        Focusing,

        /// <summary>
        /// Modifikační: colours the utterance without altering what it claims — přece, prostě, klidně, no.
        /// </summary>
        /// <remarks>
        /// The one type with a positional restriction NESČ states outright: a modifying particle cannot stand
        /// in the rheme. It carries no stress of its own and cannot be what the utterance is about.
        /// </remarks>
        Modifying,

        /// <summary>
        /// Odpověďové: a reply standing on its own — ano, ne, jistě, ovšem, samozřejmě.
        /// </summary>
        Response,

        /// <summary>
        /// Negační: denies it — ne, nikoli, nikoliv.
        /// </summary>
        Negative,

        /// <summary>
        /// Přací: wishes it were so — ať, kéž, nechť, bodejž.
        /// </summary>
        /// <remarks>
        /// The group whose membership is least settled. NESČ notes these can be read etymologically and
        /// syntactically as conjunctions rather than particles narrowly defined, which is why ať appears both
        /// here and in the conjunction inventory.
        /// </remarks>
        Optative,

        /// <summary>
        /// Strukturující: organizes the text rather than its content — za prvé, především, dále, nuže, inu.
        /// </summary>
        Structuring,

        /// <summary>
        /// Emocionální a hodnotící: the speaker's stance towards it — bohužel, naštěstí, chválabohu, stejně.
        /// </summary>
        Emotional
    }
}
