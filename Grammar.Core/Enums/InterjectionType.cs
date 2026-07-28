namespace Grammar.Core.Enums
{
    /// <summary>
    /// Represents what a Czech interjection does in the discourse.
    /// </summary>
    /// <remarks>
    /// The four types of NESČ, which splits them by discourse function and then draws one line across the
    /// four: the first three are subjective — they say something about the speaker — while the
    /// onomatopoeic ones are objective and report a sound in the world.
    /// </remarks>
    public enum InterjectionType
    {
        /// <summary>
        /// Emocionální: the speaker's state — ach, au, fuj, brr, ouvej, hurá.
        /// </summary>
        Emotional,

        /// <summary>
        /// Kontaktová: opening or holding contact with the addressee — hej, haló, ahoj, pa, čao.
        /// </summary>
        Phatic,

        /// <summary>
        /// Apelová: acting on the addressee — psst, prr, kuk, hop, aha.
        /// </summary>
        Conative,

        /// <summary>
        /// Zvukomalebná: imitating a sound — ťuk, bum, žbluňk, tik tak, bim bam.
        /// </summary>
        /// <remarks>
        /// The one objective type, and the one that reaches furthest into the grammar. NESČ notes these
        /// carry an interpretation of the event as well as of its sound — "Petr dup mi na nohu" is read as
        /// past and semelfactive without any verb saying so — and that they feed word formation directly:
        /// žbluňk gives žbluňknout.
        /// </remarks>
        Onomatopoeic
    }
}
