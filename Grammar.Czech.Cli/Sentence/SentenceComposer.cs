using Grammar.Czech.Services;

namespace Grammar.Czech.Cli.Sentence
{
    /// <summary>
    /// Builds the sentence from a draft and turns a refusal by the library into something to read.
    /// </summary>
    /// <remarks>
    /// The builder throws for things it is right to refuse — a functor the verb has no slot for, a
    /// passive the frame does not license — and in the review those are answers, not crashes. They come
    /// back as text so the dialog can print them and carry on taking corrections.
    /// </remarks>
    public sealed class SentenceComposer
    {
        private readonly CzechSentenceBuilder _builder;

        /// <summary>
        /// Initializes a new instance of the <see cref="SentenceComposer"/> type.
        /// </summary>
        /// <param name="builder">The sentence builder.</param>
        public SentenceComposer(CzechSentenceBuilder builder)
        {
            _builder = builder;
        }

        /// <summary>
        /// Builds the sentence, reporting a refusal instead of throwing it.
        /// </summary>
        /// <param name="draft">The draft to build.</param>
        /// <param name="failure">The reason the sentence could not be built, or <see langword="null"/>.</param>
        /// <returns>The sentence, or an empty string when it could not be built.</returns>
        public string Compose(ClauseDraft draft, out string? failure)
        {
            try
            {
                failure = null;

                return _builder.Build(draft.ToClause());
            }
            catch (Exception exception) when (exception is InvalidOperationException
                or NotSupportedException
                or KeyNotFoundException
                or ArgumentException
                or CliException)
            {
                failure = exception.Message;

                return string.Empty;
            }
        }

        /// <summary>
        /// Builds the sentence, or throws with the reason it could not be built.
        /// </summary>
        /// <param name="draft">The draft to build.</param>
        /// <returns>The sentence.</returns>
        /// <exception cref="CliException">Thrown when the sentence cannot be built.</exception>
        public string Compose(ClauseDraft draft)
        {
            var sentence = Compose(draft, out var failure);

            return failure is null ? sentence : throw new CliException(failure);
        }
    }
}
