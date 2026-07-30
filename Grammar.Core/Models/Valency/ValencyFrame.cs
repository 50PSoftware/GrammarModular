using Grammar.Core.Enums;

namespace Grammar.Core.Models.Valency
{
    /// <summary>
    /// Represents valency frame.
    /// </summary>
    /// <remarks>
    /// One frame holds one lexical unit under one diathesis. The two are kept apart because a diathesis
    /// remaps every slot at once — see <see cref="Grammar.Core.Enums.Diathesis"/> — so a frame that tried
    /// to carry the active and the passive together would have no single answer to what case ACT takes.
    /// </remarks>
    public sealed record ValencyFrame
    {
        /// <summary>
        /// Gets or sets verb Lemma.
        /// </summary>
        public string VerbLemma { get; init; } = string.Empty;

        /// <summary>
        /// Gets the identifier of the lexical unit — the sense — the frame belongs to.
        /// </summary>
        public long LuId { get; init; }

        /// <summary>
        /// Gets or sets frame Label.
        /// </summary>
        public string? FrameLabel { get; init; }

        /// <summary>
        /// Gets the kind of predicate the frame describes.
        /// </summary>
        public ValencyKind Kind { get; init; } = ValencyKind.Verbal;

        /// <summary>
        /// Gets the diathesis the frame states the mapping for.
        /// </summary>
        public Diathesis Diathesis { get; init; } = Diathesis.Active;

        /// <summary>
        /// Gets a value indicating whether the frame is the one to reach for when the caller names no label.
        /// </summary>
        public bool IsDefault { get; init; }

        /// <summary>
        /// Gets or sets the valency slots required by the frame.
        /// </summary>
        public IReadOnlyList<ValencySlot> Slots { get; init; } = [];
    }
}
