using Grammar.Core.Enums;

namespace Grammar.Core.Models.Semantics
{
    /// <summary>
    /// Represents a synonymy or antonymy relation between two senses.
    /// </summary>
    /// <remarks>
    /// A supplement to <see cref="SemanticFeature"/>, not the primary record of closeness — where two
    /// senses share enough features, that already implies synonymy without a row here. This exists for
    /// what features do not capture cheaply, in particular <see cref="AntonymSubtype"/>.
    /// <para>
    /// The relation is symmetric: <see cref="LuIdA"/> and <see cref="LuIdB"/> are an unordered pair, and a
    /// caller asking about one sense must check both sides of the relation.
    /// </para>
    /// </remarks>
    public sealed record SemanticRelation
    {
        /// <summary>
        /// Gets the identifier of one lexical unit in the pair.
        /// </summary>
        public long LuIdA { get; init; }

        /// <summary>
        /// Gets the identifier of the other lexical unit in the pair.
        /// </summary>
        public long LuIdB { get; init; }

        /// <summary>
        /// Gets the kind of relation between the two senses.
        /// </summary>
        public SemanticRelationType RelationType { get; init; }

        /// <summary>
        /// Gets how the two senses oppose each other, when <see cref="RelationType"/> is
        /// <see cref="SemanticRelationType.Antonym"/>; otherwise <see langword="null"/>.
        /// </summary>
        public AntonymSubtype? AntonymSubtype { get; init; }

        /// <summary>
        /// Gets how close the relation is, on a scale from 0.0 to 1.0, or <see langword="null"/> when
        /// unscored.
        /// </summary>
        public double? Strength { get; init; }
    }
}
