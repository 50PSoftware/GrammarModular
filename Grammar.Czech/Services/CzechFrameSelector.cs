using Grammar.Core.Enums;
using Grammar.Core.Interfaces;
using Grammar.Core.Models.Valency;
using Grammar.Czech.Models;

namespace Grammar.Czech.Services
{
    /// <summary>
    /// Picks the sense of a verb to build from, or reports that the dictionary does not settle it.
    /// </summary>
    /// <remarks>
    /// Kept apart from both the planner and the role resolver because both need the answer and neither
    /// owns it. The rule it applies is the project's rule about who decides: a verb with one frame has
    /// nothing to choose, a verb whose dictionary marks one sense as the default has had the choice
    /// made for it, and a verb with neither is an open question. Picking for the caller there would
    /// produce a well-formed sentence with the wrong meaning.
    /// </remarks>
    public class CzechFrameSelector
    {
        private readonly IValencyProvider<CzechLexicalEntry> valencyProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="CzechFrameSelector"/> type.
        /// </summary>
        /// <param name="valencyProvider">The dictionary to read the frames from.</param>
        public CzechFrameSelector(IValencyProvider<CzechLexicalEntry> valencyProvider)
        {
            this.valencyProvider = valencyProvider;
        }

        /// <summary>
        /// Selects the frame for the verb under the requested diathesis.
        /// </summary>
        /// <param name="verbLemma">The verb lemma.</param>
        /// <param name="frameLabel">The sense to take, or null to let the dictionary decide.</param>
        /// <param name="diathesis">The diathesis the frame has to state.</param>
        /// <returns>The selection, which reports the choices when it settles nothing.</returns>
        /// <exception cref="InvalidOperationException">Thrown when a named sense does not exist.</exception>
        public FrameSelection Select(
            string verbLemma, string? frameLabel, Diathesis diathesis = Diathesis.Active)
        {
            var frames = valencyProvider.GetFrames(verbLemma)
                .Where(frame => frame.Diathesis == diathesis)
                .ToList();

            if (frames.Count == 0)
            {
                // Most of Czech is not in the dictionary. Silence is not a refusal — the caller states
                // the cases itself, exactly as it did before the lexicon existed.
                return new FrameSelection(null, []);
            }

            if (frameLabel is not null)
            {
                var named = frames.FirstOrDefault(frame =>
                    string.Equals(frame.FrameLabel, frameLabel, StringComparison.OrdinalIgnoreCase));

                return named is not null
                    ? new FrameSelection(named, frames)
                    : throw new InvalidOperationException(
                        $"Sloveso '{verbLemma}' nemá význam '{frameLabel}'. Na výběr je: "
                        + $"{string.Join(", ", frames.Select(Describe))}.");
            }

            return new FrameSelection(
                frames.Count == 1 ? frames[0] : frames.FirstOrDefault(frame => frame.IsDefault),
                frames);
        }

        private static string Describe(ValencyFrame frame) => frame.FrameLabel ?? "bez popisku";
    }

    /// <summary>
    /// Represents the outcome of looking for the sense of a verb.
    /// </summary>
    /// <param name="Frame">The frame to build from, or null when the dictionary settles nothing.</param>
    /// <param name="Choices">Every frame the verb has under that diathesis, for a message that lists them.</param>
    public sealed record FrameSelection(ValencyFrame? Frame, IReadOnlyList<ValencyFrame> Choices)
    {
        /// <summary>
        /// Gets a value indicating whether the verb has senses but none of them was settled on.
        /// </summary>
        public bool IsAmbiguous => Frame is null && Choices.Count > 1;

        /// <summary>
        /// Gets a value indicating whether the dictionary holds no frame for the verb at all.
        /// </summary>
        public bool IsUnknown => Choices.Count == 0;

        /// <summary>
        /// Names the senses on offer, for a message that has to ask which one was meant.
        /// </summary>
        /// <returns>The sense labels, comma separated.</returns>
        public string DescribeChoices() =>
            string.Join(", ", Choices.Select(frame => frame.FrameLabel ?? "bez popisku"));
    }
}
