namespace Grammar.Core.Enums
{
    /// <summary>
    /// Morfologická třída flexe — nezávislá na sémantickém typu slova.
    /// Určuje, jakým mechanismem se slovo ohýbá.
    /// </summary>
    public enum InflectionClass
    {
        /// <summary>
        /// Substantivní flexe — suppletivní fixedForms lookup.
        /// Příklady: já, ty, my, vy, se
        /// </summary>
        Substantive,

        /// <summary>
        /// Zájmenný tvrdý vzor — paradigms.json, klíč "ten".
        /// Příklady: ten, kdo, co, nikdo, někdo, tento, tenhle
        /// </summary>
        PronounHard,

        /// <summary>
        /// Zájmenný měkký vzor — paradigms.json, klíč "nas".
        /// Příklady: náš, váš
        /// </summary>
        PronounSoft,

        /// <summary>
        /// Adjektivní tvrdý vzor — deleguje na CzechAdjectiveDeclensionService.
        /// Příklady: můj, tvůj, svůj, který, jaký, takový, sám
        /// </summary>
        AdjectiveHard,

        /// <summary>
        /// Adjektivní měkký vzor — deleguje na CzechAdjectiveDeclensionService.
        /// Příklady: její, čí, něčí, ničí
        /// </summary>
        AdjectiveSoft,

        /// <summary>
        /// Nesklonné — vrátí vždy lemma beze změny.
        /// Příklady: jeho, jejich
        /// </summary>
        Indeclinable
    }
}
