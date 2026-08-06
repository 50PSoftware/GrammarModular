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
        /// A verb with several frames is genuinely ambiguous, and the two take different arguments, so
        /// the reading cannot be picked by guessing. It can be picked by the dictionary, which is not the
        /// same thing: a frame marked default is the lexicographer saying outright which reading wins
        /// when nobody asks, and dát is transfer unless the caller says konzumace. Where the dictionary
        /// has not said — jít is motion and jít is a process, with nothing to choose between them — the
        /// caller still has to.
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
                // Two defaults are a contradiction rather than a tie to be broken, so they fall through
                // to the same refusal as none. The lexicon tool reports them at validate time.
                var preferred = frames.Where(frame => frame.IsDefault).ToList();

                if (preferred.Count == 1)
                {
                    return preferred[0];
                }

                throw new InvalidOperationException(
                    $"Sloveso '{verbLemma}' má víc rámců a žádný z nich není jednoznačně výchozí, "
                    + "vyber jeden přes FrameLabel. Dostupné rámce: "
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
            // The agent and one more aktant. A direction or a place does not count — those attach to any
            // verb at all, so counting them would license every verb there is.
            //
            // A copula has an aktant and still does not passivize: its patient is the nominal predicate
            // itself. Asked of the case it comes in, the answer would be wrong either way — Petr je
            // učitel is nominative and already in the subject position, while lev je králem zvířat is
            // instrumental and would read as a promotable object. The kind of predicate is what settles
            // it, which is what the column is for.
            if (frame.Kind is ValencyKind.Copular_NominalPred or ValencyKind.Copular_AdjectivalPred)
            {
                return false;
            }

            // The other aktant also has to be something the passive can lift into the subject, which an
            // infinitive is not: the patient of moci is the infinitive it governs, and *je mohnut jít is
            // not a sentence. So the slot has to offer at least one realization carrying a case.
            return frame.Slots.Any(slot => slot.Functor == FgdFunctor.ACT)
                && frame.Slots.Any(slot => slot.Functor != FgdFunctor.ACT
                    && IsInnerParticipant(slot.Functor)
                    && slot.Realizations.Any(realization => realization.Case is not null));
        }
    }
}
