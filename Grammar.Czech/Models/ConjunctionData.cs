using Grammar.Core.Enums;

namespace Grammar.Czech.Models
{
    /// <summary>
    /// Represents Czech conjunction metadata loaded from JSON data.
    /// </summary>
    /// <remarks>
    /// Conjunctions are a closed class, so the data file is the whole inventory the builder accepts and an
    /// unknown conjunction is reported rather than guessed at.
    /// </remarks>
    public sealed record ConjunctionData
    {
        /// <summary>
        /// Gets how the conjunction joins clauses, which also decides whether it takes the first position
        /// of the clause it introduces.
        /// </summary>
        public ConjunctionType Type { get; init; }

        /// <summary>
        /// Gets the relation the conjunction establishes between what it joins.
        /// </summary>
        public ConjunctionSemanticGroup SemanticGroup { get; init; } = ConjunctionSemanticGroup.Other;

        /// <summary>
        /// Gets a value indicating whether a comma is written before the conjunction.
        /// </summary>
        /// <remarks>
        /// A default rather than a fact about the word, because for a, i, ani, nebo and či the comma follows
        /// from the relation and not from the conjunction: no comma when the conjuncts are merely joined —
        /// "koupím jablka a hrušky" — and a comma when they stand in any other relation. The ÚJČ reference is
        /// explicit about this. Callers that know the relation override it through
        /// <see cref="Syntax.Coordination.RequiresComma"/>; this is what to write when they do not.
        /// </remarks>
        public bool RequiresComma { get; init; }

        /// <summary>
        /// Gets the second member of a paired conjunction, or null when the conjunction is not paired.
        /// </summary>
        /// <remarks>
        /// buď – nebo, nejen – ale i, ani – ani. NESČ notes that doubles exist only among the coordinating
        /// conjunctions and that the first member decides which second member may follow, which is why the
        /// pairing is recorded on the opening word rather than on both.
        /// </remarks>
        public string? Correlate { get; init; }

        /// <summary>
        /// Gets a value indicating whether the conjunction stands after the first constituent of its clause
        /// rather than in front of it.
        /// </summary>
        /// <remarks>
        /// True for však alone. Modern Czech does not put it first — "Petr však přišel", not "Však Petr
        /// přišel" — while avšak, its non-enclitic twin, is always clause-initial.
        /// <para>
        /// Where exactly it lands is less settled than that it is not first. NESČ counts však among the
        /// nestálá klitika rather than the klitika tantum, so it takes no rank in the obligatory cluster
        /// (-li, auxiliary, free dative, reflexive, dative, accusative), and Nekula ties its word-order
        /// indeterminacy to the split between the enclitic adversative však and the non-enclitic explanatory
        /// one. The builder therefore places it after the cluster — "Petr se však umyl" — as a working
        /// decision the sources permit rather than one they prescribe.
        /// </para>
        /// </remarks>
        public bool SecondPosition { get; init; }

        /// <summary>
        /// Gets a value indicating whether the conjunction absorbs the conditional auxiliary and inflects
        /// with it for person and number.
        /// </summary>
        /// <remarks>
        /// True for aby and kdyby. NESČ analyses them as containing the conditional auxiliary and showing
        /// subject agreement through it — "Řekl, abych přišel" against "Řekl, aby Petr přišel" — so the
        /// paradigm is not stored. <see cref="Stem"/> plus the particle already in the clitic data gives
        /// abych, abys, aby, abychom, abyste, aby and the kdyby row alongside it.
        /// </remarks>
        public bool FusesWithConditional { get; init; }

        /// <summary>
        /// Gets the part of the conjunction that precedes the absorbed conditional auxiliary.
        /// </summary>
        /// <remarks>
        /// "a" for aby, "kdy" for kdyby. Only meaningful together with <see cref="FusesWithConditional"/>.
        /// </remarks>
        public string? Stem { get; init; }
    }
}
