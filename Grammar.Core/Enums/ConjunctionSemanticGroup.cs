namespace Grammar.Core.Enums
{
    /// <summary>
    /// Represents the relation a Czech conjunction establishes between the clauses it joins.
    /// </summary>
    /// <remarks>
    /// The groups follow the NESČ classification, which splits the coordinating conjunctions by the relation
    /// of the conjuncts and the subordinating ones by the role of the dependent clause. A few names serve
    /// both sides — a causal relation is causal whether neboť coordinates it or protože subordinates it —
    /// so the two systems share one enum rather than duplicating the overlap.
    /// </remarks>
    public enum ConjunctionSemanticGroup
    {
        /// <summary>
        /// Slučovací: the conjuncts simply hold together — a, i, ani, nebo.
        /// </summary>
        Copulative,

        /// <summary>
        /// Odporovací: the second conjunct runs against the first — ale, avšak, však, leč, nýbrž, sice.
        /// </summary>
        Adversative,

        /// <summary>
        /// Stupňovací: the second conjunct outweighs the first — ba, dokonce.
        /// </summary>
        Gradational,

        /// <summary>
        /// Vylučovací: the conjuncts exclude one another — (a)nebo, či.
        /// </summary>
        Disjunctive,

        /// <summary>
        /// Příčinné: the reason. Coordinating only in neboť; subordinating in protože, jelikož, poněvadž.
        /// </summary>
        Causal,

        /// <summary>
        /// Důsledkové: what follows from it — tedy, tudíž, tak, takže.
        /// </summary>
        Consecutive,

        /// <summary>
        /// Časové: when — když, než, až, dokud, jakmile, zatímco, sotva.
        /// </summary>
        Temporal,

        /// <summary>
        /// Přípustkové: it holds despite — ač, ačkoli, přestože, třebaže, byť.
        /// </summary>
        Concessive,

        /// <summary>
        /// Podmínkové: it holds if — jestliže, pokud, -li, kdyby.
        /// </summary>
        Conditional,

        /// <summary>
        /// Účelové: what for — aby.
        /// </summary>
        Purpose,

        /// <summary>
        /// Obsahové: the dependent clause is an argument of the matrix predicate — že, ať, zda, jestli.
        /// </summary>
        /// <remarks>
        /// NESČ groups these as the plain complementizers: the clause fills a subject, object or attribute
        /// slot rather than adding a circumstance, so the conjunction carries no meaning of its own.
        /// </remarks>
        Content,

        /// <summary>
        /// Srovnávací: measured against — jako, než, jak.
        /// </summary>
        Comparison,

        /// <summary>
        /// Anything the groups above do not cover.
        /// </summary>
        Other
    }
}
