namespace Grammar.Core.Enums
{
    /// <summary>
    /// Specifies the way a verb's action unfolds — způsob slovesného děje, Aktionsart.
    /// </summary>
    /// <remarks>
    /// Not aspect, and not a finer grade of it. Aspect is a grammatical category with two members that
    /// every Czech verb has; this is a lexical classification of what kind of event the verb names, and
    /// most verbs are in none of these groups at all. The two meet only in that a group implies an
    /// aspect — see <see cref="AktionsartFacts.RequiredAspect"/>.
    /// <para>
    /// The inventory is the one in Nový encyklopedický slovník češtiny, heslo ZPŮSOB SLOVESNÉHO DĚJE,
    /// taken whole rather than subsetted: it is a closed classification and a partial copy of it would
    /// invite the next person to invent the missing half.
    /// </para>
    /// <para>
    /// The members are in the order the source lists them, which is also the order its rule about
    /// aspect is stated in. Do not renumber: the value is what the lexicon stores.
    /// </para>
    /// </remarks>
    public enum Aktionsart
    {
        /// <summary>Ingresivní — the action begins: zakašlat, zakřičet, zamyslet se.</summary>
        Ingressive = 1,

        /// <summary>Evolutivní — the action gets under way: rozšumět se, rozplakat se.</summary>
        Evolutive,

        /// <summary>Delimitativní — the action lasts a while and stops: pospat, posedět, pozpívat.</summary>
        Delimitative,

        /// <summary>Rezultativní — the action reaches its result: napsat, poprosit, zaplatit.</summary>
        Resultative,

        /// <summary>Terminativní, konfektivní — the action is carried through: propracovat, přečíst.</summary>
        Terminative,

        /// <summary>Perdurativní, spaciální — the action fills a stretch of time: propracovat.</summary>
        Perdurative,

        /// <summary>Finitivní, kompletivní — the rest of the action is finished off: dočíst, dodělat.</summary>
        Finitive,

        /// <summary>Egresivní — the action ceases: odzvučet, odučit.</summary>
        Egressive,

        /// <summary>Exhaustivní — the actor is spent by it: ubrečet se, uhnat, utancovat.</summary>
        Exhaustive,

        /// <summary>Totální — the action covers the whole of its object: pročistit, promrazit.</summary>
        Total,

        /// <summary>Saturativní — the actor has had enough: najíst se, vyspat se.</summary>
        Saturative,

        /// <summary>Extenzivní — the action goes on to excess for the actor: napracovat se, nasedět se.</summary>
        Extensive,

        /// <summary>Kumulativní — the action accumulates: nabalit, nahrabat, nakapat.</summary>
        Cumulative,

        /// <summary>Intenzivní — the action is done thoroughly: najet, načadit.</summary>
        Intensive,

        /// <summary>Excesivní — the action overshoots: překrmit, přepít se, přesolit.</summary>
        Excessive,

        /// <summary>Distributivní — the action goes through its objects one by one: pozamykat.</summary>
        Distributive,

        /// <summary>Atenuativní — the action happens slightly: nachýlit se, přimodřit.</summary>
        Attenuative,

        /// <summary>Semelfaktivní — one instance of an act that can repeat: bodnout, kopnout, mávnout.</summary>
        Semelfactive,

        /// <summary>Momentální — the action has no duration to speak of: dát, hodit, koupit, najít.</summary>
        Momentary,

        /// <summary>
        /// Iterativní, frekventativní — the secondary iteratives in -ívat/-ávat: dělávat, mívat, vídat.
        /// </summary>
        /// <remarks>
        /// Not to be confused with <see cref="Frequentative"/>, whose double name is the same two words
        /// the other way round. This one is the habitual formed from a verb; that one is the repeated
        /// counterpart of a single act.
        /// </remarks>
        Iterative,

        /// <summary>Deminutivní — the action is done a little and repeatedly: pozpěvovat, pospávat.</summary>
        Diminutive,

        /// <summary>Komitativní — the action accompanies another: přizvukovat, přikusovat.</summary>
        Comitative,

        /// <summary>
        /// Frekventativní, iterativní — repeated instances of a single act: bodat, kopat, klepat.
        /// </summary>
        /// <remarks>
        /// The imperfective partner of <see cref="Semelfactive"/>: bodat against bodnout, blýskat
        /// against blýsknout. See <see cref="Iterative"/> for the group named the other way round.
        /// </remarks>
        Frequentative,

        /// <summary>Stativní — a state rather than an event: sedět, milovat, toužit.</summary>
        Stative,

        /// <summary>Dekurzivní — an activity that simply runs on: psát, myslet, zpívat.</summary>
        Decursive,

        /// <summary>Mutativní — a gradual change of state: bohatnout, černat.</summary>
        Mutative
    }
}
