using Grammar.Czech.Models;

namespace Grammar.Czech.Models.Syntax
{
    /// <summary>
    /// Represents a clause once every grammatical decision has been made and before any word has been
    /// put in front of another.
    /// </summary>
    /// <remarks>
    /// This is the seam between the two halves of building a sentence. Everything above it decides what
    /// the words are — which case the frame governs, what the predicate agrees with, which particle the
    /// reflexive contributes. Everything below it decides what order they go in, and is not allowed to
    /// change any of it.
    /// <para>
    /// The predicate is carried separately from <see cref="Clause"/> rather than written back into it
    /// because <see cref="CzechWordRequest"/> is a struct: subject agreement fills in person, number and
    /// gender on a copy, and a caller reading <c>Clause.Predicate</c> would see the request as it was
    /// before agreement ran.
    /// </para>
    /// </remarks>
    /// <param name="Clause">The clause with government and agreement applied.</param>
    /// <param name="Predicate">The predicate as agreement left it, which is what gets conjugated.</param>
    public sealed record PlannedClause(CzechClause Clause, CzechWordRequest Predicate);
}
