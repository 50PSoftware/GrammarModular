using Grammar.Core.Enums;
using Grammar.Core.Models.Valency;

namespace Grammar.Czech.Interfaces
{
    /// <summary>
    /// Defines operations for reading a verb's valency frame.
    /// </summary>
    public interface ICzechValencyService
    {
        /// <summary>
        /// Gets the valency frame for the supplied verb.
        /// </summary>
        /// <param name="verbLemma">The verb lemma.</param>
        /// <param name="frameLabel">The frame to pick when the verb has several, or null for the only one.</param>
        /// <returns>The frame, or <see langword="null"/> when the verb has none registered.</returns>
        /// <exception cref="System.InvalidOperationException">
        /// Thrown when the verb has several frames and the label does not identify one of them.
        /// </exception>
        ValencyFrame? GetFrame(string verbLemma, string? frameLabel);

        /// <summary>
        /// Gets the slot filling the supplied functor in the frame.
        /// </summary>
        /// <param name="frame">The frame to read.</param>
        /// <param name="functor">The functor to look for.</param>
        /// <returns>The slot, or <see langword="null"/> when the frame has none for that functor.</returns>
        ValencySlot? GetSlot(ValencyFrame frame, FgdFunctor functor);

        /// <summary>
        /// Determines whether the functor is an inner participant, which only a frame can license.
        /// </summary>
        /// <param name="functor">The functor to classify.</param>
        /// <returns><see langword="true"/> for an inner participant; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// Inner participants (ACT, PAT, ADDR, ORIG, EFF) belong to the verb and appear at most once per
        /// clause, so a verb that has no slot for one cannot take it. Free modifications — time, place,
        /// manner and the rest — attach to any verb and are never licensed by the frame.
        /// </remarks>
        bool IsInnerParticipant(FgdFunctor functor);
    }
}
