using Grammar.Core.Enums;
using Grammar.Core.Interfaces;
using Grammar.Core.Models.Valency;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;

namespace Grammar.Czech.Services
{
    /// <summary>
    /// Provides access to a verb's valency frame: which arguments it takes and how each is realized.
    /// </summary>
    public class CzechValencyService : ICzechValencyService
    {
        private static readonly HashSet<FgdFunctor> InnerParticipants =
            [FgdFunctor.ACT, FgdFunctor.PAT, FgdFunctor.ADDR, FgdFunctor.ORIG, FgdFunctor.EFF];

        private readonly IValencyProvider<CzechLexicalEntry> _valencyProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="CzechValencyService"/> type.
        /// </summary>
        public CzechValencyService(IValencyProvider<CzechLexicalEntry> valencyProvider)
        {
            _valencyProvider = valencyProvider;
        }

        /// <summary>
        /// Gets the valency frame for the supplied verb.
        /// </summary>
        /// <param name="verbLemma">The verb lemma.</param>
        /// <param name="frameLabel">The frame to pick when the verb has several, or null for the only one.</param>
        /// <returns>The frame, or <see langword="null"/> when the verb has none registered.</returns>
        /// <remarks>
        /// A verb with several frames is genuinely ambiguous — jít has one frame for motion and one for a
        /// process — and the two take different arguments, so guessing would silently pick a reading. The
        /// label has to say which.
        /// </remarks>
        public ValencyFrame? GetFrame(string verbLemma, string? frameLabel, Diathesis diathesis = Diathesis.Active)
        {
            // Filtered before anything else is counted, so a sense that has gained a passive frame does
            // not start reading as ambiguous to every caller who only ever wanted the active one.
            var frames = _valencyProvider.GetFrames(verbLemma)
                .Where(frame => frame.Diathesis == diathesis)
                .ToList();

            if (frames.Count == 0)
            {
                return null;
            }

            if (frameLabel is not null)
            {
                return frames.FirstOrDefault(frame => frame.FrameLabel == frameLabel)
                    ?? throw new InvalidOperationException(
                        $"Sloveso '{verbLemma}' nemá rámec '{frameLabel}'. Dostupné rámce: "
                        + string.Join(", ", frames.Select(frame => frame.FrameLabel ?? "(bez názvu)")) + ".");
            }

            if (frames.Count > 1)
            {
                throw new InvalidOperationException(
                    $"Sloveso '{verbLemma}' má víc rámců, vyber jeden přes FrameLabel. Dostupné rámce: "
                    + string.Join(", ", frames.Select(frame => frame.FrameLabel ?? "(bez názvu)")) + ".");
            }

            return frames[0];
        }

        /// <summary>
        /// Gets the slot filling the supplied functor in the frame.
        /// </summary>
        /// <param name="frame">The frame to read.</param>
        /// <param name="functor">The functor to look for.</param>
        /// <returns>The slot, or <see langword="null"/> when the frame has none for that functor.</returns>
        public ValencySlot? GetSlot(ValencyFrame frame, FgdFunctor functor)
            => frame.Slots.FirstOrDefault(slot => slot.Functor == functor);

        /// <summary>
        /// Determines whether the functor is an inner participant, which only a frame can license.
        /// </summary>
        /// <param name="functor">The functor to classify.</param>
        /// <returns><see langword="true"/> for an inner participant; otherwise, <see langword="false"/>.</returns>
        public bool IsInnerParticipant(FgdFunctor functor) => InnerParticipants.Contains(functor);

        /// <summary>
        /// Determines whether the frame licenses the periphrastic passive.
        /// </summary>
        /// <param name="frame">The frame to judge.</param>
        /// <returns><see langword="true"/> when the verb can be passivized in this sense; otherwise, <see langword="false"/>.</returns>
        public bool LicensesPeriphrasticPassive(ValencyFrame frame)
        {
            var functors = frame.Slots.Select(slot => slot.Functor).ToList();

            // The agent and one more aktant. A direction or a place does not count — those attach to any
            // verb at all, so counting them would license every verb there is.
            return functors.Contains(FgdFunctor.ACT)
                && functors.Any(functor => functor != FgdFunctor.ACT && IsInnerParticipant(functor));
        }
    }
}
